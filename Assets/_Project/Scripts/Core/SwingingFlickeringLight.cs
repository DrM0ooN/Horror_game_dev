using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Makes a hanging light fixture sway gently like a pendulum and occasionally dip in
    /// intensity for a brief flicker. Attach to the pivot the Light hangs below (offset on a
    /// "cord"), not the Light itself, so rotating the pivot swings the light sideways. Deliberately
    /// does not require a Light on this GameObject itself (only [RequireComponent] would silently
    /// auto-add a second, unconfigured Light on the pivot) -- it finds the real one on a child.
    /// </summary>
    public sealed class SwingingFlickeringLight : MonoBehaviour
    {
        [Header("Swing")]
        [SerializeField] private float swingAmplitudeDegrees = 4f;
        [SerializeField] private float swingSpeed = 0.6f;

        [Header("Flicker")]
        [SerializeField] private float minSecondsBetweenFlickers = 4f;
        [SerializeField] private float maxSecondsBetweenFlickers = 10f;
        [SerializeField] private float flickerDurationSeconds = 0.15f;
        [SerializeField, Range(0f, 1f)] private float flickerLowIntensityFraction = 0.15f;

        private Light hubLight;
        private float baseIntensity;
        private float nextFlickerTime;
        private float flickerEndTime;
        private bool isFlickering;
        private float timeOffset;

        private void Awake()
        {
            hubLight = GetComponentInChildren<Light>();
            baseIntensity = hubLight.intensity;
            timeOffset = Random.Range(0f, 100f);
            ScheduleNextFlicker();
        }

        private void Update()
        {
            var t = Time.time + timeOffset;
            transform.localRotation = Quaternion.Euler(
                Mathf.Sin(t * swingSpeed) * swingAmplitudeDegrees,
                0f,
                Mathf.Sin((t * swingSpeed * 0.7f) + 1.3f) * swingAmplitudeDegrees * 0.6f);

            UpdateFlicker();
        }

        private void UpdateFlicker()
        {
            if (isFlickering)
            {
                if (Time.time >= flickerEndTime)
                {
                    isFlickering = false;
                    hubLight.intensity = baseIntensity;
                    ScheduleNextFlicker();
                }
                else
                {
                    hubLight.intensity = baseIntensity * flickerLowIntensityFraction;
                }

                return;
            }

            if (Time.time >= nextFlickerTime)
            {
                isFlickering = true;
                flickerEndTime = Time.time + flickerDurationSeconds;
            }
        }

        private void ScheduleNextFlicker()
        {
            nextFlickerTime = Time.time + Random.Range(minSecondsBetweenFlickers, maxSecondsBetweenFlickers);
        }
    }
}
