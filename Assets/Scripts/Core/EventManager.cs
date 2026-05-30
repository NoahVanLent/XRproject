using System;
using UnityEngine;

/// <summary>
/// Observer pattern implementation.
/// Systems subscribe to events here instead of calling each other directly.
/// This keeps code decoupled — e.g. UI doesn't need a reference to GameManager.
///
/// Usage:
///   Subscribe:   EventManager.OnPlayerCaught += MyMethod;
///   Unsubscribe: EventManager.OnPlayerCaught -= MyMethod;
///   Fire:        EventManager.FirePlayerCaught();
/// </summary>
public static class EventManager
{
    // Game state events
    public static event Action OnGameStarted;
    public static event Action OnPlayerCaught;
    public static event Action OnPlayerWon;
    public static event Action OnGameRestart;

    // Gameplay events
    public static event Action<float> OnTimerUpdated;   // passes remaining seconds
    public static event Action OnPlayerHidden;
    public static event Action OnPlayerVisible;
    public static event Action OnObjectGrabbed;
    public static event Action OnObjectReleased;

    public static void FireGameStarted()  => OnGameStarted?.Invoke();
    public static void FirePlayerCaught()
    {
        Debug.Log("Event: PlayerCaught");
        OnPlayerCaught?.Invoke();
    }

    public static void FirePlayerWon()
    {
        Debug.Log("Event: PlayerWon");
        OnPlayerWon?.Invoke();
    }

    public static void FireGameRestart()
    {
        Debug.Log("Event: GameRestart");
        OnGameRestart?.Invoke();
    }

    public static void FireTimerUpdated(float remaining) => OnTimerUpdated?.Invoke(remaining);
    public static void FirePlayerHidden()  => OnPlayerHidden?.Invoke();
    public static void FirePlayerVisible() => OnPlayerVisible?.Invoke();
    public static void FireObjectGrabbed() => OnObjectGrabbed?.Invoke();
    public static void FireObjectReleased() => OnObjectReleased?.Invoke();
}
