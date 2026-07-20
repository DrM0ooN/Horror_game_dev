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

        private float elapsed;
        private float switchInterval;
        private int currentAnchor;

        public bool IsLitByFlashlight => isLitByFlashlight;

        private void Awake()
        {
            currentAnchor = Mathf.Clamp(startingAnchorIndex, 0, anchors != null ? anchors.Length - 1 : 0);
            ApplyAnchor(currentAnchor);
            switchInterval = baseSwitchInterval;
        }

        private void Update()
        {
            if (anchors == null || anchors.Length < 2)
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
