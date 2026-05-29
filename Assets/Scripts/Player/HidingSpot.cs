using UnityEngine;

// Place this on any trigger collider that acts as a hiding spot (e.g. a desk, closet).
// The player is hidden while inside and unhidden when they leave.
[RequireComponent(typeof(Collider))]
public class HidingSpot : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerHide>(out var player))
            player.Hide();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerHide>(out var player))
            player.Unhide();
    }
}
