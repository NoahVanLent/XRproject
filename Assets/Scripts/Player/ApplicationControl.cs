using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

// Attach to XR Origin.
// Menu button (left controller) = pause/unpause
// Primary button (right controller, held 1s) = restart
public class ApplicationControl : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenuUI;

    private InputDevice _leftController;
    private InputDevice _rightController;
    private bool _isPaused;
    private bool _menuWasPressed;
    private bool _primaryWasPressed;
    private float _restartHoldTime;

    void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceConnected;
        FindControllers();
    }

    void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
    }

    void OnDeviceConnected(InputDevice device) => FindControllers();

    void FindControllers()
    {
        var left = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, left);
        if (left.Count > 0) _leftController = left[0];

        var right = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, right);
        if (right.Count > 0) _rightController = right[0];
    }

    void Update()
    {
        HandlePause();
        HandleRestart();
    }

    void HandlePause()
    {
        if (!_leftController.isValid) return;

        _leftController.TryGetFeatureValue(CommonUsages.menuButton, out bool menuPressed);

        if (menuPressed && !_menuWasPressed)
            TogglePause();

        _menuWasPressed = menuPressed;
    }

    void HandleRestart()
    {
        if (!_rightController.isValid) return;

        _rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed);

        if (primaryPressed)
        {
            _restartHoldTime += Time.unscaledDeltaTime;
            if (_restartHoldTime >= 1f)
                RestartScene();
        }
        else
        {
            _restartHoldTime = 0f;
        }

        _primaryWasPressed = primaryPressed;
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(_isPaused);
        Debug.Log(_isPaused ? "Game paused" : "Game resumed");
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
