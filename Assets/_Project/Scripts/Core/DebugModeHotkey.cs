#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorGame.Core
{
    /// <summary>
    /// Polls a fixed key to flip DebugMode. Uses the Input System's low-level Keyboard device
    /// directly rather than the project's InputSystem_Actions asset -- this is a dev-only utility
    /// key, not a real gameplay action, so it doesn't need an action map entry. The whole component
    /// (including this Update call) compiles out of non-development builds, on top of DebugMode's
    /// own Toggle() already being a no-op there -- belt and suspenders.
    /// </summary>
    public sealed class DebugModeHotkey : MonoBehaviour
    {
        [SerializeField] private Key toggleKey = Key.K;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                DebugMode.Toggle();
            }
        }
    }
}
#endif
