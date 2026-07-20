using UnityEngine;

namespace HorrorGame.Player
{
    /// <summary>
    /// Reusable flashlight item with battery drain that can be enabled per mini-game.
    /// </summary>
    public sealed class FlashlightItem : MonoBehaviour
    {
        [SerializeField] private Light flashlightLight;
        [SerializeField, Min(1f)] private float batteryDurationSeconds = 90f;
        [SerializeField] private bool startsEnabled;

        private float remainingBattery;
        private bool itemEnabled;

        public float RemainingBattery01 => batteryDurationSeconds <= 0f ? 0f : Mathf.Clamp01(remainingBattery / batteryDurationSeconds);
        public bool IsEnabled => itemEnabled;

        private void Awake()
        {
            remainingBattery = batteryDurationSeconds;
            SetEnabled(startsEnabled);
        }

        private void Update()
        {
            if (!itemEnabled)
            {
                return;
            }

            remainingBattery = Mathf.Max(0f, remainingBattery - Time.deltaTime);
            if (remainingBattery <= 0f)
            {
                SetEnabled(false);
            }
        }

        public void SetEnabled(bool enabled)
        {
            itemEnabled = enabled;
            if (flashlightLight != null)
            {
                flashlightLight.enabled = enabled;
            }
        }

        public void ResetBattery()
        {
            remainingBattery = batteryDurationSeconds;
        }
    }
}
