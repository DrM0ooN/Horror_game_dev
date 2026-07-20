using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorGame.Player
{
    /// <summary>
    /// A robust, CharacterController-based first-person player controller.
    /// Uses the Unity Input System and existing input actions for WASD movement and mouse look.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The camera used for first-person vision.")]
        [SerializeField] private Camera playerCamera;
        
        [Tooltip("The Input Action Asset containing player action maps.")]
        [SerializeField] private InputActionAsset inputAsset;

        [Header("Movement Settings")]
        [Tooltip("Base movement speed in meters per second.")]
        [SerializeField] private float walkSpeed = 4f;

        [Tooltip("Sprint movement speed in meters per second.")]
        [SerializeField] private float sprintSpeed = 6.5f;

        [Tooltip("Force of jumping.")]
        [SerializeField] private float jumpHeight = 1.2f;

        [Tooltip("Custom gravity multiplier for better feel.")]
        [SerializeField] private float gravityMultiplier = 2f;

        [Header("Look Settings")]
        [Tooltip("Mouse vertical and horizontal sensitivity.")]
        [SerializeField] private float lookSensitivity = 0.1f;

        [Tooltip("Minimum look angle (pitch) in degrees.")]
        [SerializeField] private float minPitch = -85f;

        [Tooltip("Maximum look angle (pitch) in degrees.")]
        [SerializeField] private float maxPitch = 85f;

        [Header("Cursor Settings")]
        [Tooltip("Automatically lock the cursor on start.")]
        [SerializeField] private bool lockCursorOnStart = true;

        [Header("Action Map Names")]
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string sprintActionName = "Sprint";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string attackActionName = "Attack";
        [SerializeField] private string cancelActionName = "Cancel";

        private CharacterController characterController;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private InputAction attackAction;
        private InputAction cancelAction;

        private float verticalRotation;
        private float verticalVelocity;
        private bool isSprinting;

        /// <summary>
        /// Public getter to check if the controller is currently grounded.
        /// </summary>
        public bool IsGrounded => characterController != null && characterController.isGrounded;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null)
                {
                    Debug.LogWarning("[PlayerFirstPersonController] Player Camera is not assigned and could not be found in children.");
                }
            }

            InitializeInputs();
        }

        private void Start()
        {
            if (lockCursorOnStart)
            {
                SetCursorLocked(true);
            }
        }

        private void InitializeInputs()
        {
            if (inputAsset == null)
            {
                Debug.LogError("[PlayerFirstPersonController] InputActionAsset reference is missing on the controller.", this);
                return;
            }

            InputActionMap playerMap = inputAsset.FindActionMap(actionMapName);
            if (playerMap == null)
            {
                Debug.LogError($"[PlayerFirstPersonController] Action Map '{actionMapName}' not found in the Input Action Asset.", this);
                return;
            }

            moveAction = playerMap.FindAction(moveActionName);
            lookAction = playerMap.FindAction(lookActionName);
            sprintAction = playerMap.FindAction(sprintActionName);
            jumpAction = playerMap.FindAction(jumpActionName);
            attackAction = playerMap.FindAction(attackActionName);
            cancelAction = playerMap.FindAction(cancelActionName);

            if (moveAction == null) Debug.LogWarning($"[PlayerFirstPersonController] Action '{moveActionName}' not found in map '{actionMapName}'.");
            if (lookAction == null) Debug.LogWarning($"[PlayerFirstPersonController] Action '{lookActionName}' not found in map '{actionMapName}'.");
            if (sprintAction == null) Debug.LogWarning($"[PlayerFirstPersonController] Action '{sprintActionName}' not found in map '{actionMapName}'.");
            if (jumpAction == null) Debug.LogWarning($"[PlayerFirstPersonController] Action '{jumpActionName}' not found in map '{actionMapName}'.");
            if (attackAction == null) Debug.LogWarning($"[PlayerFirstPersonController] Action '{attackActionName}' not found in map '{actionMapName}'.");
            if (cancelAction == null) Debug.LogWarning($"[PlayerFirstPersonController] Action '{cancelActionName}' not found in map '{actionMapName}'.");
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            lookAction?.Enable();
            sprintAction?.Enable();
            jumpAction?.Enable();
            attackAction?.Enable();
            cancelAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            lookAction?.Disable();
            sprintAction?.Disable();
            jumpAction?.Disable();
            attackAction?.Disable();
            cancelAction?.Disable();
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();

            // Toggle cursor lock state in editor or runtime for ease of development
            if (cancelAction != null && cancelAction.WasPressedThisFrame())
            {
                SetCursorLocked(false);
            }
            else if (attackAction != null && attackAction.WasPressedThisFrame() && !Cursor.visible)
            {
                SetCursorLocked(true);
            }
        }

        private void HandleLook()
        {
            if (lookAction == null) return;

            Vector2 lookDelta = lookAction.ReadValue<Vector2>();

            // Horizontal rotation (rotate character around Y axis)
            transform.Rotate(Vector3.up * lookDelta.x * lookSensitivity);

            // Vertical rotation (rotate camera around X axis, clamped)
            verticalRotation -= lookDelta.y * lookSensitivity;
            verticalRotation = Mathf.Clamp(verticalRotation, minPitch, maxPitch);

            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            if (characterController == null) return;

            // Determine if sprinting
            isSprinting = sprintAction != null && sprintAction.IsPressed();
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            // Get WASD/analog stick input
            Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector3 targetDirection = transform.right * input.x + transform.forward * input.y;

            // Keep vertical movement and horizontal movement separate
            Vector3 horizontalMove = targetDirection.normalized * currentSpeed;

            // Apply gravity and grounding
            float gravity = Physics.gravity.y * gravityMultiplier;

            if (characterController.isGrounded)
            {
                // Reset vertical velocity when grounded, applying a slight downward force to keep grounded status stable
                verticalVelocity = -2f;

                // Handle Jump
                if (jumpAction != null && jumpAction.WasPressedThisFrame())
                {
                    // standard jump formula: sqrt(height * -2 * gravity)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            else
            {
                // Apply normal gravity when in mid-air
                verticalVelocity += gravity * Time.deltaTime;
            }

            // Combine horizontal and vertical velocity
            Vector3 finalMove = horizontalMove;
            finalMove.y = verticalVelocity;

            // Move the character controller
            characterController.Move(finalMove * Time.deltaTime);
        }

        /// <summary>
        /// Explicitly wires up the Player Camera reference in code.
        /// </summary>
        public void SetPlayerCamera(Camera cam)
        {
            playerCamera = cam;
        }

        /// <summary>
        /// Lock or unlock mouse cursor during play.
        /// </summary>
        public void SetCursorLocked(bool isLocked)
        {
            Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isLocked;
        }
    }
}
