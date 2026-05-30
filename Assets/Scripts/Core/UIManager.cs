using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages all in-game UI:
/// - World-space HUD (timer + hidden indicator) attached to camera
/// - Caught screen
/// - Won screen
/// Subscribes to EventManager — no direct reference to GameManager needed.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI hiddenStatusText;

    [Header("Caught Screen")]
    [SerializeField] private GameObject caughtPanel;
    [SerializeField] private TextMeshProUGUI caughtText;

    [Header("Won Screen")]
    [SerializeField] private GameObject wonPanel;
    [SerializeField] private TextMeshProUGUI wonText;

    void OnEnable()
    {
        EventManager.OnTimerUpdated  += UpdateTimer;
        EventManager.OnPlayerCaught  += ShowCaught;
        EventManager.OnPlayerWon     += ShowWon;
        EventManager.OnPlayerHidden  += () => SetHiddenStatus(true);
        EventManager.OnPlayerVisible += () => SetHiddenStatus(false);
    }

    void OnDisable()
    {
        EventManager.OnTimerUpdated  -= UpdateTimer;
        EventManager.OnPlayerCaught  -= ShowCaught;
        EventManager.OnPlayerWon     -= ShowWon;
        EventManager.OnPlayerHidden  -= () => SetHiddenStatus(true);
        EventManager.OnPlayerVisible -= () => SetHiddenStatus(false);
    }

    void Start()
    {
        if (caughtPanel != null) caughtPanel.SetActive(false);
        if (wonPanel    != null) wonPanel.SetActive(false);
        SetHiddenStatus(false);
    }

    void UpdateTimer(float remaining)
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        timerText.text = $"{minutes:0}:{seconds:00}";

        // Turn red in last 20 seconds
        timerText.color = remaining < 20f ? Color.red : Color.white;
    }

    void SetHiddenStatus(bool hidden)
    {
        if (hiddenStatusText == null) return;
        hiddenStatusText.text = hidden ? "HIDDEN" : "";
        hiddenStatusText.color = hidden ? Color.green : Color.white;
    }

    void ShowCaught()
    {
        if (caughtPanel != null) caughtPanel.SetActive(true);
        if (wonPanel    != null) wonPanel.SetActive(false);
    }

    void ShowWon()
    {
        if (wonPanel    != null) wonPanel.SetActive(true);
        if (caughtPanel != null) caughtPanel.SetActive(false);
    }

    public void OnRestartButton()
    {
        EventManager.FireGameRestart();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
