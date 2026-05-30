using UnityEngine;

/// <summary>
/// Place on a 3D object. ControllerRay detects this component
/// and calls Press() when the trigger is pulled.
/// </summary>
public class StartButton : MonoBehaviour
{
    private bool _pressed;
    private Renderer _renderer;
    private Color _defaultColor;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null) _defaultColor = _renderer.sharedMaterial.color;
    }

    public void Hover(bool on)
    {
        if (_renderer == null) return;
        var mpb = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", on ? new Color(0.2f, 0.9f, 0.2f) : _defaultColor);
        _renderer.SetPropertyBlock(mpb);
    }

    public void Press()
    {
        if (_pressed) return;
        _pressed = true;
        GameManager.Instance.StartGame();
    }
}
