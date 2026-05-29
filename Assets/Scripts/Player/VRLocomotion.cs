using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

// Attach to XR Origin. Provides smooth movement (left stick) and snap turning (right stick).
// Requires: XR Interaction Toolkit 3.x, CharacterController on XR Origin.
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

    private CharacterController _controller;
    private InputDevice _leftController;
    private InputDevice _rightController;
    private float _verticalVelocity;
    private bool _snapTurnReady = true;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;
    }

    void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceConnected;
        FindControllers();
    }

    void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
    }

    void OnDeviceConnected(InputDevice device) => FindControllers();

    void FindControllers()
    {
        var leftDevices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftDevices);
        if (leftDevices.Count > 0) _leftController = leftDevices[0];

        var rightDevices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightDevices);
        if (rightDevices.Count > 0) _rightController = rightDevices[0];
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying()) return;

        HandleMovement();
        HandleSnapTurn();
        ApplyGravity();
    }

    void HandleMovement()
    {
        if (!_leftController.isValid) return;

        _leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 input);
        if (input.magnitude < 0.1f) return;

        // Move relative to where the player is looking (horizontal only)
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 move = (forward * input.y + right * input.x) * moveSpeed * Time.deltaTime;

        _controller.Move(move);
    }

    void HandleSnapTurn()
    {
        if (!_rightController.isValid) return;

        _rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 input);

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
        {
            _verticalVelocity = -1f;
        }
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }

        _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }
}
