using HorrorGame.Core;
using UnityEngine;

namespace HorrorGame.MiniGames
{
    /// <summary>
    /// In real gameplay, fragments only glow once the player is close, ramping in gradually as
    /// they approach -- players should have to search for them, not spot them from across the
    /// room. Debug mode keeps the original always-on, full-brightness glow (a testing aid for
    /// checking spawn point placement/visibility, left untouched per explicit request).
    /// <para>
    /// Uses a MaterialPropertyBlock rather than editing the material directly, since all
    /// fragments share one Material asset (palette.Fragment) -- mutating it would make every
    /// fragment glow identically instead of independently by its own distance to the player.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class FlashlightFragmentGlow : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Real Mode Proximity Glow (tune to taste)")]
        [Tooltip("Beyond this distance from the player, the fragment shows no glow at all in real mode.")]
        [SerializeField, Min(0.1f)] private float glowStartDistance = 3.5f;
        [Tooltip("At or within this distance, the fragment reaches its full (real-mode) glow.")]
        [SerializeField, Min(0.1f)] private float glowFullDistance = 1.2f;
        [Tooltip("Even at full proximity, real-mode glow is this fraction of the original/debug-mode brightness.")]
        [SerializeField, Range(0f, 1f)] private float realModeMaxEmissionScale = 0.45f;

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Color baseEmissionColor;
        private Transform player;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            propertyBlock = new MaterialPropertyBlock();
            baseEmissionColor = meshRenderer.sharedMaterial.GetColor(EmissionColorId);

            // Single-player vertical slice only -- revisit once players are networked (multiple
            // remote players won't all share one "the" player to measure distance from).
            var mainCamera = Camera.main;
            player = mainCamera != null ? mainCamera.transform : null;
        }

        private void Update()
        {
            var scale = DebugMode.IsEnabled ? 1f : ComputeRealModeScale();

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(EmissionColorId, baseEmissionColor * scale);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private float ComputeRealModeScale()
        {
            if (player == null)
            {
                return 0f;
            }

            var distance = Vector3.Distance(transform.position, player.position);
            // InverseLerp(far, near, distance) -> 0 at/beyond glowStartDistance, 1 at/within
            // glowFullDistance, smoothly interpolated between (and already clamped to 0-1).
            var proximity = Mathf.InverseLerp(glowStartDistance, glowFullDistance, distance);
            return proximity * realModeMaxEmissionScale;
        }
    }
}
