using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Add this alongside XRGrabInteractable to any prop that can be picked up.
// Handles visual feedback on hover/grab.
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class GrabbableObject : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private Color defaultColor = Color.white;

    private XRGrabInteractable _grabInteractable;
    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _renderer = GetComponentInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();

        _grabInteractable.hoverEntered.AddListener(OnHoverEnter);
        _grabInteractable.hoverExited.AddListener(OnHoverExit);
        _grabInteractable.selectEntered.AddListener(OnGrab);
        _grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        _grabInteractable.hoverEntered.RemoveListener(OnHoverEnter);
        _grabInteractable.hoverExited.RemoveListener(OnHoverExit);
        _grabInteractable.selectEntered.RemoveListener(OnGrab);
        _grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    void OnHoverEnter(HoverEnterEventArgs args) => SetColor(hoverColor);
    void OnHoverExit(HoverExitEventArgs args) => SetColor(defaultColor);
    void OnGrab(SelectEnterEventArgs args) => Debug.Log($"{name}: grabbed");
    void OnRelease(SelectExitEventArgs args) => Debug.Log($"{name}: released");

    void SetColor(Color color)
    {
        if (_renderer == null) return;
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", color);
        _renderer.SetPropertyBlock(_mpb);
    }
}
