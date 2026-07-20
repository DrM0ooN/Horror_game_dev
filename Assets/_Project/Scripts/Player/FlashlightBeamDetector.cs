using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using HorrorGame.MiniGames;
using HorrorGame.Core;

namespace HorrorGame.Player
{
    /// <summary>
    /// Bridges a FlashlightItem to the flashlight mini-game: reports monster exposure when it
    /// falls inside the lit cone with a clear line of sight, and lets the player collect a
    /// fragment with the Interact action while it is illuminated (not on proximity/overlap alone).
    /// </summary>
    [RequireComponent(typeof(FlashlightItem))]
    public sealed class FlashlightBeamDetector : MonoBehaviour
    {
        [SerializeField] private Transform beamOrigin;
        [SerializeField] private FlashlightMonsterSwitcher monsterSwitcher;
                [SerializeField, Min(0.1f)] private float range = 10f;
                [SerializeField, Range(1f, 89f)] private float halfAngleDegrees = 20f;
        [SerializeField, Min(0.1f)] private float collectRange = 2.5f;
        [SerializeField] private LayerMask obstructionMask = ~0;

        [Header("Interact")]
        [Tooltip("The Input Action Asset containing the Interact action.")]
        [SerializeField] private InputActionAsset inputAsset;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string interactActionName = "Interact";

        private FlashlightItem flashlightItem;
        private InputAction interactAction;
        private readonly Collider[] overlapBuffer = new Collider[16];
        private readonly HashSet<FlashlightKeyFragment> fragmentsInRange = new HashSet<FlashlightKeyFragment>();
        private readonly HashSet<FlashlightKeyFragment> seenThisFrame = new HashSet<FlashlightKeyFragment>();

        private void Awake()
        {
            flashlightItem = GetComponent<FlashlightItem>();
            InitializeInput();
        }

        private void OnEnable()
        {
            interactAction?.Enable();
        }

        private void OnDisable()
        {
            interactAction?.Disable();
        }

        private void InitializeInput()
        {
            if (inputAsset == null)
            {
                Debug.LogWarning("[FlashlightBeamDetector] InputActionAsset reference is missing — fragment collection via Interact will not work.", this);
                return;
            }

            InputActionMap map = inputAsset.FindActionMap(actionMapName);
            if (map == null)
            {
                Debug.LogWarning($"[FlashlightBeamDetector] Action Map '{actionMapName}' not found in the Input Action Asset.", this);
                return;
            }

            interactAction = map.FindAction(interactActionName);
            if (interactAction == null)
            {
                Debug.LogWarning($"[FlashlightBeamDetector] Action '{interactActionName}' not found in map '{actionMapName}'.", this);
            }
        }

private void Update()
        {
            if (!flashlightItem.IsEnabled)
            {
                if (monsterSwitcher != null)
                {
                    monsterSwitcher.SetLitByFlashlight(false);
                }

                ClearFragmentsInRange();
                return;
            }

            var origin = beamOrigin != null ? beamOrigin.position : transform.position;
            var forward = beamOrigin != null ? beamOrigin.forward : transform.forward;

            seenThisFrame.Clear();

            var count = Physics.OverlapSphereNonAlloc(origin, range, overlapBuffer, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var fragment = overlapBuffer[i].GetComponentInParent<FlashlightKeyFragment>();
                if (fragment == null || fragment.IsCollected)
                {
                    continue;
                }

                if (!IsIlluminated(origin, forward, fragment.transform.position))
                {
                    continue;
                }

                // Illumination uses the full flashlight range so fragments are visible/findable from
                // a distance, but collecting one requires actually closing in -- a tighter, separate gate.
                if (Vector3.Distance(origin, fragment.transform.position) > collectRange)
                {
                    continue;
                }

                seenThisFrame.Add(fragment);
            }

            var interactPressed = interactAction != null && interactAction.WasPressedThisFrame();
            if (interactPressed)
            {
                foreach (var fragment in seenThisFrame)
                {
                    fragment.Collect();
                }
            }

            fragmentsInRange.Clear();
            foreach (var fragment in seenThisFrame)
            {
                fragmentsInRange.Add(fragment);
            }

            NotificationHud.Interact(fragmentsInRange.Count > 0 ? "Press [E] to collect" : null);

            if (monsterSwitcher != null)
            {
                monsterSwitcher.SetLitByFlashlight(IsIlluminated(origin, forward, monsterSwitcher.transform.position));
            }
        }

private void ClearFragmentsInRange()
        {
            if (fragmentsInRange.Count == 0)
            {
                return;
            }

            fragmentsInRange.Clear();
            NotificationHud.Interact(null);
        }

        private bool IsIlluminated(Vector3 origin, Vector3 forward, Vector3 targetPosition)
        {
            var toTarget = targetPosition - origin;
            var distance = toTarget.magnitude;
            if (distance > range)
            {
                return false;
            }

            if (Vector3.Angle(forward, toTarget) > halfAngleDegrees)
            {
                return false;
            }

            return !Physics.Raycast(origin, toTarget.normalized, distance, obstructionMask, QueryTriggerInteraction.Ignore);
        }
    }
}
