using UnityEngine;

// Attach to the XR Origin. Tracks whether the player is currently hidden.
// Call Hide() / Unhide() from a HidingSpot when the player enters/exits.
public class PlayerHide : MonoBehaviour
{
    public bool IsHidden { get; private set; }

    [SerializeField] private GameObject hiddenIndicator; // optional UI marker

    public void Hide()
    {
        IsHidden = true;
        if (hiddenIndicator != null) hiddenIndicator.SetActive(true);
        EventManager.FirePlayerHidden();
    }

    public void Unhide()
    {
        IsHidden = false;
        if (hiddenIndicator != null) hiddenIndicator.SetActive(false);
        EventManager.FirePlayerVisible();
    }
}
