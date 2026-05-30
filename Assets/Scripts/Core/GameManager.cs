using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { WaitingToStart, Playing, Caught, Won }

    public GameState State { get; private set; } = GameState.Playing;

    [Header("Game Settings")]
    [SerializeField] private float timeLimit = 120f;

    private float _timeRemaining;

    public float TimeRemaining => _timeRemaining;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        State = GameState.WaitingToStart;
        _timeRemaining = timeLimit;
    }

    public void StartGame()
    {
        if (State != GameState.WaitingToStart) return;
        State = GameState.Playing;
        EventManager.FireGameStarted();
    }

    void Update()
    {
        if (State != GameState.Playing) return;

        _timeRemaining -= Time.deltaTime;
        EventManager.FireTimerUpdated(_timeRemaining);
        if (_timeRemaining <= 0f)
            TriggerWin();
    }

    public void TriggerCaught()
    {
        if (State != GameState.Playing) return;
        State = GameState.Caught;
        EventManager.FirePlayerCaught();
    }

    public void TriggerWin()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        EventManager.FirePlayerWon();
    }

    public bool IsPlaying() => State == GameState.Playing;
    public bool IsWaiting() => State == GameState.WaitingToStart;
}
