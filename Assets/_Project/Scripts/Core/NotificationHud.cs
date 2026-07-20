using UnityEngine;
using UnityEngine.UIElements;

namespace HorrorGame.Core
{
    /// <summary>
    /// Simple on-screen HUD: a persistent "press E"-style interact prompt (shown/hidden by
    /// condition) and a single transient toast (shown for a fixed duration then hidden). Backed by
    /// a UI Toolkit UXML/USS document; static helpers let any script show a message without
    /// needing a direct reference.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class NotificationHud : MonoBehaviour
    {
        public static NotificationHud Instance { get; private set; }

        [SerializeField] private string interactPromptElementName = "interact-prompt";
        [SerializeField] private string toastElementName = "toast";
        [SerializeField, Min(0.1f)] private float toastDurationSeconds = 2.5f;

        private Label interactPromptLabel;
        private Label toastLabel;
        private float toastHideTime;
        private bool toastActive;

        private void Awake()
        {
            Instance = this;

            var root = GetComponent<UIDocument>().rootVisualElement;
            interactPromptLabel = root.Q<Label>(interactPromptElementName);
            toastLabel = root.Q<Label>(toastElementName);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (toastActive && Time.time >= toastHideTime)
            {
                toastActive = false;
                toastLabel?.AddToClassList("hud-hidden");
            }
        }

        /// <summary>
        /// Shows (or hides, if text is null/empty) the persistent interact prompt.
        /// </summary>
        public void SetInteractPrompt(string text)
        {
            if (interactPromptLabel == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                interactPromptLabel.AddToClassList("hud-hidden");
                return;
            }

            interactPromptLabel.text = text;
            interactPromptLabel.RemoveFromClassList("hud-hidden");
        }

        /// <summary>
        /// Shows a transient toast message for toastDurationSeconds.
        /// </summary>
        public void ShowToast(string text)
        {
            if (toastLabel == null)
            {
                return;
            }

            toastLabel.text = text;
            toastLabel.RemoveFromClassList("hud-hidden");
            toastActive = true;
            toastHideTime = Time.time + toastDurationSeconds;
        }

        /// <summary>Static convenience wrapper -- no-ops if no NotificationHud exists yet.</summary>
        public static void Interact(string text)
        {
            Instance?.SetInteractPrompt(text);
        }

        /// <summary>Static convenience wrapper -- no-ops if no NotificationHud exists yet.</summary>
        public static void Toast(string text)
        {
            Instance?.ShowToast(text);
        }
    }
}
