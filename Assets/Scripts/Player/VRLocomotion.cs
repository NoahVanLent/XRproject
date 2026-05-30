using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

// Attach to XR Origin. Smooth movement (left stick) + snap turn (right stick).
[RequireComponent(typeof(CharacterController))]
public class VRLocomotion : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Transform cameraTransform;

    [Header("Turning")]
    [SerializeField] private float snapTurnDegrees = 45f;
    [SerializeField] private float snapTurnDeadzone = 0.5f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    // Input actions - bound in Inspector via Input Action Asset or here via code
    private InputAction _moveAction;
    private InputAction _turnAction;

    private CharacterController _controller;
    private float _verticalVelocity;
    private bool _snapTurnReady = true;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

        // Bind to controller sticks — both generic XR and Oculus-specific for Quest 2
        _moveAction = new InputAction("Move", expectedControlType: "Vector2");
        _moveAction.AddBinding("<XRController>{LeftHand}/thumbstick");
        _moveAction.AddBinding("<OculusTouchController>{LeftHand}/thumbstick");

        _turnAction = new InputAction("Turn", expectedControlType: "Vector2");
        _turnAction.AddBinding("<XRController>{RightHand}/thumbstick");
        _turnAction.AddBinding("<OculusTouchController>{RightHand}/thumbstick");

        _moveAction.Enable();
        _turnAction.Enable();
    }

    void OnDestroy()
    {
        _moveAction?.Disable();
        _turnAction?.Disable();
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying()) return;

        UpdateCharacterControllerHeight();
        HandleMovement();
        HandleSnapTurn();
        ApplyGravity();
    }

    // Keep CharacterController height matching the player's real-world height
    void UpdateCharacterControllerHeight()
    {
        if (cameraTransform == null) return;
        float camY = cameraTransform.localPosition.y;
        float height = Mathf.Clamp(camY, 0.5f, 2.5f);
        _controller.height = height;
        _controller.center = new Vector3(0f, height * 0.5f, 0f); // keep x/z centered
    }

    void HandleMovement()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        if (input.magnitude < 0.1f) return;

        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right   = Vector3.ProjectOnPlane(cameraTransform.right,   Vector3.up).normalized;
        Vector3 move    = (forward * input.y + right * input.x) * moveSpeed * Time.deltaTime;

        _controller.Move(move);
    }

    void HandleSnapTurn()
    {
        Vector2 input = _turnAction.ReadValue<Vector2>();

        if (Mathf.Abs(input.x) < snapTurnDeadzone)
        {
            _snapTurnReady = true;
            return;
        }

        if (!_snapTurnReady) return;

        float dir = input.x > 0 ? 1f : -1f;
        transform.Rotate(Vector3.up, snapTurnDegrees * dir);
        _snapTurnReady = false;
    }

    void ApplyGravity()
    {
        if (_controller.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }
}
