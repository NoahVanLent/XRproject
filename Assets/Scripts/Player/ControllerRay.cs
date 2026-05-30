using UnityEngine;
using UnityEngine.InputSystem;

// Attach to the Right Controller GameObject.
// Only highlights and grabs objects that have a GrabbableObject component.
public class ControllerRay : MonoBehaviour
{
    [Header("Ray Settings")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color hitColor = new Color(0.3f, 1f, 0.3f);

    private LineRenderer _line;
    private InputAction _triggerAction;
    private GameObject _lastHovered;
    private GameObject _heldObject;
    private Rigidbody _heldRb;
    private bool _triggerWasPressed;

    void Awake()
    {
        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = 0.005f;
        _line.endWidth = 0.002f;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.useWorldSpace = true;

        _triggerAction = new InputAction("RightTrigger", expectedControlType: "float");
        _triggerAction.AddBinding("<XRController>{RightHand}/trigger");
        _triggerAction.AddBinding("<OculusTouchController>{RightHand}/triggerPressed");
        _triggerAction.Enable();
    }

    void OnDestroy() => _triggerAction?.Disable();

    void Update()
    {
        // If holding an object, move it with the controller
        if (_heldObject != null)
        {
            UpdateHeld();
            return;
        }

        CastRay();
    }

    void CastRay()
    {
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;

        bool triggerPressed = _triggerAction.ReadValue<float>() > 0.5f;
        bool triggerDown = triggerPressed && !_triggerWasPressed;
        _triggerWasPressed = triggerPressed;

        bool didHit = Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance);

        // Only care about grabbable objects
        GrabbableObject grabbable = didHit ? hit.collider.GetComponent<GrabbableObject>() : null;

        // Update line
        _line.SetPosition(0, origin);
        _line.SetPosition(1, grabbable != null ? hit.point : origin + direction * maxDistance);
        _line.startColor = _line.endColor = grabbable != null ? hitColor : defaultColor;

        // Hover highlight — only on grabbables
        if (grabbable != null)
        {
            if (hit.collider.gameObject != _lastHovered)
            {
                ClearHover();
                _lastHovered = hit.collider.gameObject;
                SetHighlight(_lastHovered, true);
            }

            // Grab on trigger press
            if (triggerDown)
                GrabObject(hit.collider.gameObject, hit.collider.GetComponent<Rigidbody>());
        }
        else
        {
            ClearHover();
        }
    }

    void GrabObject(GameObject obj, Rigidbody rb)
    {
        if (rb == null) return;

        ClearHover();
        _heldObject = obj;
        _heldRb = rb;

        _heldRb.isKinematic = true;
        _heldRb.useGravity = false;

        SetHighlight(_heldObject, false);
        EventManager.FireObjectGrabbed();
    }

    void UpdateHeld()
    {
        // Follow controller position smoothly
        _heldObject.transform.position = Vector3.Lerp(
            _heldObject.transform.position,
            transform.position + transform.forward * 0.4f,
            Time.deltaTime * 20f);
        _heldObject.transform.rotation = transform.rotation;

        // Update line to show held object
        _line.SetPosition(0, transform.position);
        _line.SetPosition(1, _heldObject.transform.position);
        _line.startColor = _line.endColor = hitColor;

        // Release on trigger release
        bool triggerPressed = _triggerAction.ReadValue<float>() > 0.5f;
        if (!triggerPressed && _triggerWasPressed)
            ReleaseObject();

        _triggerWasPressed = triggerPressed;
    }

    void ReleaseObject()
    {
        if (_heldRb != null)
        {
            _heldRb.isKinematic = false;
            _heldRb.useGravity = true;
        }

        EventManager.FireObjectReleased();
        _heldObject = null;
        _heldRb = null;
        _triggerWasPressed = false;
    }

    void SetHighlight(GameObject obj, bool on)
    {
        if (obj == null) return;
        var rend = obj.GetComponent<Renderer>();
        if (rend == null) return;

        var mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", on ? new Color(1f, 0.9f, 0.3f) : new Color(0.8f, 0.5f, 0.1f));
        rend.SetPropertyBlock(mpb);
    }

    void ClearHover()
    {
        if (_lastHovered == null) return;
        SetHighlight(_lastHovered, false);
        _lastHovered = null;
    }
}
