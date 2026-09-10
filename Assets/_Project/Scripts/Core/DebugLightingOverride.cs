using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Brightens the scene's ambient light while DebugMode is on, so testers can see the whole
    /// room instead of only the flashlight's cone -- and restores the actual art-direction-correct
    /// dark ambient (captured on first use, not hardcoded, so it always matches whatever value is
    /// currently set) once debug mode is switched off.
    /// </summary>
    public sealed class DebugLightingOverride : MonoBehaviour
    {
        [SerializeField] private Color debugAmbientColor = new Color(0.6f, 0.6f, 0.6f);

        private Color originalAmbientColor;
        private bool hasCapturedOriginal;

        private void OnEnable()
        {
            DebugMode.Changed += ApplyLighting;
            ApplyLighting(DebugMode.IsEnabled);
        }

        private void OnDisable()
        {
            DebugMode.Changed -= ApplyLighting;
        }

        private void ApplyLighting(bool debugEnabled)
        {
            if (!hasCapturedOriginal)
            {
                originalAmbientColor = RenderSettings.ambientLight;
                hasCapturedOriginal = true;
            }

            RenderSettings.ambientLight = debugEnabled ? debugAmbientColor : originalAmbientColor;
        }
    }
}
