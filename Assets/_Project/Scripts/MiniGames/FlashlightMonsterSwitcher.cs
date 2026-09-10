using HorrorGame.Core;
using UnityEngine;

namespace HorrorGame.MiniGames
{
    /// <summary>
    /// Moves monster presence between predefined anchor points to simulate blending with the environment.
    /// </summary>
    public sealed class FlashlightMonsterSwitcher : MonoBehaviour
    {
        [SerializeField] private Transform[] anchors;
        [SerializeField, Min(0.1f)] private float baseSwitchInterval = 10f;
        [SerializeField, Min(0.05f)] private float switchIntervalReductionPerThreat = 0.6f;
        [SerializeField, Min(0.1f)] private float minimumSwitchInterval = 2.5f;
        [SerializeField] private int startingAnchorIndex;

        [Header("Debug/Integration")]
        [SerializeField] private bool isLitByFlashlight;

        [Header("Debug Visibility")]
        [Tooltip("Reads from the centralized DebugMode toggle (default hotkey: K) rather than its own flag, so it stays in sync with every other debug-only visual.")]
        [SerializeField] private MeshRenderer bodyRenderer;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material debugGlowMaterial;

        private float elapsed;
        private float switchInterval;
        private int currentAnchor;
        private bool isActive;

        public bool IsLitByFlashlight => isLitByFlashlight;

        private void Awake()
        {
            currentAnchor = Mathf.Clamp(startingAnchorIndex, 0, anchors != null ? anchors.Length - 1 : 0);
            ApplyAnchor(currentAnchor);
            switchInterval = baseSwitchInterval;
        }

        private void OnEnable()
        {
            DebugMode.Changed += ApplyDebugGlowVisibility;
            ApplyDebugGlowVisibility(DebugMode.IsEnabled);
        }

        private void OnDisable()
        {
            DebugMode.Changed -= ApplyDebugGlowVisibility;
        }

        private void ApplyDebugGlowVisibility(bool debugEnabled)
        {
            if (bodyRenderer == null)
            {
                return;
            }

            var material = debugEnabled ? debugGlowMaterial : normalMaterial;
            if (material != null)
            {
                bodyRenderer.sharedMaterial = material;
            }
        }

        /// <summary>
        /// Snaps back to the configured starting anchor. Called between mini-game attempts so a
        /// retry starts from a consistent monster position rather than wherever it last was.
        /// </summary>
        public void ResetToStartingAnchor()
        {
            elapsed = 0f;
            currentAnchor = Mathf.Clamp(startingAnchorIndex, 0, anchors != null ? anchors.Length - 1 : 0);
            ApplyAnchor(currentAnchor);
        }

        /// <summary>
        /// Gates anchor switching to only while a mini-game session is actually running. Without
        /// this the monster was observed teleporting between anchors continuously from scene load,
        /// including while no one had even entered the room yet.
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            elapsed = 0f;
        }

        private void Update()
        {
            if (!isActive || anchors == null || anchors.Length < 2)
            {
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed < switchInterval)
            {
                return;
            }

            elapsed = 0f;
            SwitchAnchor();
        }

        public void SetLitByFlashlight(bool isLit)
        {
            isLitByFlashlight = isLit;
        }

        public void ApplyThreatLevel(int threatLevel)
        {
            switchInterval = Mathf.Max(minimumSwitchInterval, baseSwitchInterval - (threatLevel * switchIntervalReductionPerThreat));
        }

        private void SwitchAnchor()
        {
            if (anchors == null || anchors.Length == 0)
            {
                return;
            }

            var next = currentAnchor;
            if (anchors.Length > 1)
            {
                while (next == currentAnchor)
                {
                    next = Random.Range(0, anchors.Length);
                }
            }

            currentAnchor = next;
            ApplyAnchor(currentAnchor);
        }

        private void ApplyAnchor(int anchorIndex)
        {
            if (anchors == null || anchors.Length == 0)
            {
                return;
            }

            var anchor = anchors[Mathf.Clamp(anchorIndex, 0, anchors.Length - 1)];
            if (anchor != null)
            {
                transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            }
        }
    }
}
