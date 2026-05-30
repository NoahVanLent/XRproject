using UnityEngine;
using UnityEngine.AI;

// Patrols a fixed route. Stops and raises alarm if player enters alert radius.
// Does not chase — alerts the SeekerAgent instead.
[RequireComponent(typeof(NavMeshAgent))]
public class GuardAgent : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float waitTime = 2f;

    [Header("Alert")]
    [SerializeField] private float alertRadius = 5f;
    [SerializeField] private SeekerAgent seekerToAlert;

    private NavMeshAgent _agent;
    private Transform _player;
    private int _patrolIndex;
    private float _waitTimer;
    private bool _waiting;
    private bool _alerted;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _agent.speed = patrolSpeed;
    }

    void Start()
    {
        if (patrolPoints.Length > 0)
            _agent.SetDestination(patrolPoints[0].position);
    }

    void Update()
    {
        // Game always continues

        CheckAlert();
        if (!_alerted) Patrol();
    }

    void CheckAlert()
    {
        if (_player == null) return;
        if (_alerted) return;

        bool playerHidden = _player.TryGetComponent<PlayerHide>(out var hide) && hide.IsHidden;
        if (!playerHidden && Vector3.Distance(transform.position, _player.position) < alertRadius)
        {
            _alerted = true;
            _agent.isStopped = true;
            Debug.Log("GuardAgent: Alert! Notifying seeker.");
            // Seeker will pick up the player via its own sight, guard just stops
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        if (_waiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waiting = false;
                _agent.isStopped = false;
                _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
                _agent.SetDestination(patrolPoints[_patrolIndex].position);
            }
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance < 0.3f)
        {
            _waiting = true;
            _waitTimer = waitTime;
            _agent.isStopped = true;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }
}
