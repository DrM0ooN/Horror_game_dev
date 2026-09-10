using System;

namespace HorrorGame.Core
{
    /// <summary>
    /// Centralized dev/testing toggle. Every debug-only visual (monster glow, lighting overrides,
    /// etc.) should read <see cref="IsEnabled"/> or subscribe to <see cref="Changed"/> instead of
    /// keeping its own separate flag -- that doesn't scale past one or two debug aids.
    /// <para>
    /// In non-development builds <see cref="IsEnabled"/> is a compile-time constant false and
    /// <see cref="Toggle"/>/<see cref="Changed"/> are no-ops, so any code gated behind them is dead
    /// code the compiler can strip. There is no runtime way to enable this outside the Editor or a
    /// Development Build, regardless of the hotkey being pressed.
    /// </para>
    /// </summary>
    public static class DebugMode
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool IsEnabled { get; private set; }

        public static event Action<bool> Changed;

        public static void Toggle()
        {
            IsEnabled = !IsEnabled;
            Changed?.Invoke(IsEnabled);
            NotificationHud.Toast(IsEnabled ? "Debug mode: ON" : "Debug mode: OFF");
        }
#else
        public const bool IsEnabled = false;

        public static event Action<bool> Changed { add { } remove { } }

        public static void Toggle() { }
#endif
    }
}
