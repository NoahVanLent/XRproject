using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

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
        Debug.Log("Player: hidden");
    }

    public void Unhide()
    {
        IsHidden = false;
        if (hiddenIndicator != null) hiddenIndicator.SetActive(false);
        Debug.Log("Player: visible");
    }
}
