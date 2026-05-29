using UnityEngine;
using UnityEngine.AI;

// Stationary watcher that rotates to scan an area.
// Triggers game over immediately if player enters its cone and is not hidden.
[RequireComponent(typeof(NavMeshAgent))]
public class WatcherAgent : MonoBehaviour
{
    [Header("Scan")]
    [SerializeField] private float scanAngle = 60f;
    [SerializeField] private float scanRange = 8f;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private float maxRotationAngle = 90f;

    [Header("Obstacles")]
    [SerializeField] private LayerMask obstacleMask;

    private Transform _player;
    private float _baseYRotation;
    private float _currentAngle;
    private float _rotationDir = 1f;

    void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _baseYRotation = transform.eulerAngles.y;

        // Watcher doesn't move — disable NavMeshAgent movement
        var agent = GetComponent<NavMeshAgent>();
        agent.isStopped = true;
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying()) return;

        Scan();
        Rotate();
    }

    void Rotate()
    {
        _currentAngle += rotationSpeed * _rotationDir * Time.deltaTime;
        if (Mathf.Abs(_currentAngle) >= maxRotationAngle)
            _rotationDir *= -1f;

        transform.eulerAngles = new Vector3(0f, _baseYRotation + _currentAngle, 0f);
    }

    void Scan()
    {
        if (_player == null) return;

        Vector3 toPlayer = _player.position - transform.position;
        if (toPlayer.magnitude > scanRange) return;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > scanAngle * 0.5f) return;

        if (_player.TryGetComponent<PlayerHide>(out var hide) && hide.IsHidden) return;

        if (!Physics.Raycast(transform.position + Vector3.up, toPlayer.normalized, toPlayer.magnitude, obstacleMask))
            GameManager.Instance.TriggerCaught();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3 leftBound = Quaternion.Euler(0, -scanAngle * 0.5f, 0) * transform.forward * scanRange;
        Vector3 rightBound = Quaternion.Euler(0, scanAngle * 0.5f, 0) * transform.forward * scanRange;
        Gizmos.DrawRay(transform.position, leftBound);
        Gizmos.DrawRay(transform.position, rightBound);
        Gizmos.DrawWireSphere(transform.position, scanRange);
    }
}
