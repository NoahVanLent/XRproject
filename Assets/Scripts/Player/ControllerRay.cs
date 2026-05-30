using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

// Attach to the Right Controller GameObject.
// Draws a ray from the controller and highlights whatever it points at.
// Pull the right trigger to interact (grab/select).
public class ControllerRay : MonoBehaviour
{
    [Header("Ray Settings")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hitColor = new Color(0.3f, 1f, 0.3f);

    private LineRenderer _line;
    private InputAction _triggerAction;
    private GameObject _lastHit;

    void Awake()
    {
        // Line renderer for visual ray
        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = 0.005f;
        _line.endWidth = 0.002f;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.useWorldSpace = true;

        // Right trigger input
        _triggerAction = new InputAction("RightTrigger", expectedControlType: "float");
        _triggerAction.AddBinding("<XRController>{RightHand}/trigger");
        _triggerAction.AddBinding("<OculusTouchController>{RightHand}/triggerPressed");
        _triggerAction.Enable();
    }

    void OnDestroy()
    {
        _triggerAction?.Disable();
    }

    void Update()
    {
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;

        bool didHit = Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance);

        // Update line
        _line.SetPosition(0, origin);
        _line.SetPosition(1, didHit ? hit.point : origin + direction * maxDistance);
        _line.startColor = _line.endColor = didHit ? hitColor : defaultColor;

        // Hover highlight
        if (didHit)
        {
            if (hit.collider.gameObject != _lastHit)
            {
                ClearLastHit();
                _lastHit = hit.collider.gameObject;
                HighlightObject(_lastHit, true);
            }

            // Trigger pressed — interact
            if (_triggerAction.ReadValue<float>() > 0.5f)
                TryInteract(hit.collider.gameObject);
        }
        else
        {
            ClearLastHit();
        }
    }

    void TryInteract(GameObject target)
    {
        // Hiding spot interaction
        if (target.TryGetComponent<HidingSpot>(out var spot))
        {
            var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerHide>();
            if (player != null && !player.IsHidden)
                player.Hide();
        }

        Debug.Log($"Ray interact: {target.name}");
    }

    void HighlightObject(GameObject obj, bool on)
    {
        if (obj == null) return;
        var rend = obj.GetComponent<Renderer>();
        if (rend == null) return;

        var mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", on ? new Color(1f, 0.9f, 0.3f) : Color.white);
        rend.SetPropertyBlock(mpb);
    }

    void ClearLastHit()
    {
        if (_lastHit == null) return;
        HighlightObject(_lastHit, false);
        _lastHit = null;
    }
}
