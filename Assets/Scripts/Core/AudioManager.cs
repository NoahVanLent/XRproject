using UnityEngine;

/// <summary>
/// Singleton AudioManager — second singleton pattern in the project.
/// Subscribes to EventManager events and plays the matching audio clip.
/// Add AudioClips in the Inspector on the AudioManager GameObject.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip caughtSound;
    [SerializeField] private AudioClip wonSound;
    [SerializeField] private AudioClip hiddenSound;
    [SerializeField] private AudioClip grabSound;

    private AudioSource _source;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = gameObject.AddComponent<AudioSource>();
    }

    void OnEnable()
    {
        // Subscribe to Observer events
        EventManager.OnPlayerCaught  += PlayCaught;
        EventManager.OnPlayerWon     += PlayWon;
        EventManager.OnPlayerHidden  += PlayHidden;
        EventManager.OnObjectGrabbed += PlayGrab;
    }

    void OnDisable()
    {
        // Always unsubscribe to avoid memory leaks
        EventManager.OnPlayerCaught  -= PlayCaught;
        EventManager.OnPlayerWon     -= PlayWon;
        EventManager.OnPlayerHidden  -= PlayHidden;
        EventManager.OnObjectGrabbed -= PlayGrab;
    }

    void PlayCaught()  => Play(caughtSound);
    void PlayWon()     => Play(wonSound);
    void PlayHidden()  => Play(hiddenSound);
    void PlayGrab()    => Play(grabSound);

    public void Play(AudioClip clip)
    {
        if (clip == null) return;
        _source.PlayOneShot(clip);
    }
}
