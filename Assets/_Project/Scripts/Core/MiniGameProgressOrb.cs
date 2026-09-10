using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Single progress orb: dormant (unlit material, no glow) until its room is solved, then
    /// switches to a lit emissive material plus a small glow light. Hovers gently at all times
    /// and pulses its glow once lit, for a subtle "living crystal" feel with placeholder primitives.
    /// </summary>
    public sealed class MiniGameProgressOrb : MonoBehaviour
    {
        [SerializeField] private MeshRenderer orbRenderer;
        [SerializeField] private Material unlitMaterial;
        [SerializeField] private Material litMaterial;
        [SerializeField] private Light glowLight;

        [Header("Idle Hover")]
        [SerializeField] private float bobAmplitude = 0.05f;
        [SerializeField] private float bobSpeed = 1.2f;

        [Header("Lit Pulse")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField, Range(0f, 1f)] private float pulseIntensityRange = 0.35f;

        private Vector3 basePosition;
        private float baseLightIntensity;
        private float timeOffset;
        private bool isLit;

        private void Awake()
        {
            basePosition = transform.localPosition;
            timeOffset = Random.Range(0f, 10f);

            if (glowLight != null)
            {
                baseLightIntensity = glowLight.intensity;
                glowLight.enabled = false;
            }
        }

        /// <summary>
        /// Swaps between the dormant and lit look. Safe to call repeatedly with the same value.
        /// </summary>
        public void SetLit(bool lit)
        {
            isLit = lit;

            if (orbRenderer != null)
            {
                orbRenderer.sharedMaterial = lit ? litMaterial : unlitMaterial;
            }

            if (glowLight != null)
            {
                glowLight.enabled = lit;
            }
        }

        private void Update()
        {
            var t = Time.time + timeOffset;
            transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(t * bobSpeed) * bobAmplitude);

            if (isLit && glowLight != null)
            {
                glowLight.intensity = baseLightIntensity * (1f + Mathf.Sin(t * pulseSpeed) * pulseIntensityRange);
            }
        }
    }
}
