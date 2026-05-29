using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Caught, Won }

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
        DontDestroyOnLoad(gameObject);
        _timeRemaining = timeLimit;
    }

    void Update()
    {
        if (State != GameState.Playing) return;

        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0f)
            TriggerWin();
    }

    public void TriggerCaught()
    {
        if (State != GameState.Playing) return;
        State = GameState.Caught;
        Debug.Log("GameManager: Player caught!");
    }

    public void TriggerWin()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        Debug.Log("GameManager: Player survived!");
    }

    public bool IsPlaying() => State == GameState.Playing;
}
