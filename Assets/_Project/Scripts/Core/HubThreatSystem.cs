using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Shared threat state for the run. Vertical slice uses integer steps with a visible indicator hook.
    /// </summary>
    public sealed class HubThreatSystem : MonoBehaviour
    {
        [SerializeField, Min(0)] private int maxThreatLevel = 10;
        [SerializeField] private ThreatIndicatorView indicatorView;

        public int ThreatLevel { get; private set; }

        /// <summary>
        /// Increases threat by one step and updates view bindings.
        /// </summary>
public void IncreaseThreat()
        {
            ThreatLevel = Mathf.Clamp(ThreatLevel + 1, 0, maxThreatLevel);
            if (indicatorView != null)
            {
                indicatorView.SetThreatLevel(ThreatLevel);
            }

            NotificationHud.Toast($"You died. Threat rises ({ThreatLevel}/{maxThreatLevel}).");
        }
    }
}
