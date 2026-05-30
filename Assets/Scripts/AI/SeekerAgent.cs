using UnityEngine;
using UnityEngine.AI;

// Three modes selectable in the Inspector:
//   Wander  — roams randomly across the NavMesh
//   Chaser  — always follows the player, no line-of-sight needed
//   Stalker — stands still until player enters sight, then chases
[RequireComponent(typeof(NavMeshAgent))]
public class SeekerAgent : MonoBehaviour
{
    public enum Mode { Wander, Chaser, Stalker }

    [Header("Behaviour")]
    [SerializeField] private Mode mode = Mode.Wander;

    [Header("Detection (Stalker only)")]
    [SerializeField] private float sightRange = 12f;
    [SerializeField] private float sightAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Speed")]
    [SerializeField] private float wanderSpeed  = 1.2f;
    [SerializeField] private float chaseSpeed   = 2.0f;
    [SerializeField] private float stalkerSpeed = 1.6f;

    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float wanderWaitMin = 2f;
    [SerializeField] private float wanderWaitMax = 5f;

    private NavMeshAgent _agent;
    private Transform _player;
    private bool _activated;      // for Stalker: has it spotted player yet?
    private float _wanderTimer;

    void OnEnable()  => EventManager.OnPlayerCaught += OnPlayerCaught;
    void OnDisable() => EventManager.OnPlayerCaught -= OnPlayerCaught;
    void OnPlayerCaught() => _agent.isStopped = true;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        switch (mode)
        {
            case Mode.Wander:  _agent.speed = wanderSpeed;  break;
            case Mode.Chaser:  _agent.speed = chaseSpeed;   break;
            case Mode.Stalker: _agent.speed = 0f;           break;
        }
    }

    void Start()
    {
        if (mode == Mode.Wander)
            SetRandomWanderDestination();
    }

    void Update()
    {
        switch (mode)
        {
            case Mode.Wander:  UpdateWander();  break;
            case Mode.Chaser:  UpdateChaser();  break;
            case Mode.Stalker: UpdateStalker(); break;
        }
    }

    // ── Wander ──────────────────────────────────────────────
    void UpdateWander()
    {
        if (_agent.pathPending) return;

        if (_agent.remainingDistance <= _agent.stoppingDistance)
        {
            _wanderTimer -= Time.deltaTime;
            if (_wanderTimer <= 0f)
                SetRandomWanderDestination();
        }
    }

    void SetRandomWanderDestination()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius + transform.position;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);

        _wanderTimer = Random.Range(wanderWaitMin, wanderWaitMax);
    }

    // ── Chaser ──────────────────────────────────────────────
    void UpdateChaser()
    {
        if (_player == null) return;
        _agent.SetDestination(_player.position);

        if (Vector3.Distance(transform.position, _player.position) < 1.5f)
            GameManager.Instance.TriggerCaught();
    }

    // ── Stalker ─────────────────────────────────────────────
    void UpdateStalker()
    {
        if (!_activated)
        {
            if (CanSeePlayer())
            {
                _activated = true;
                _agent.speed = stalkerSpeed;
                Debug.Log("Stalker: player spotted!");
            }
            return;
        }

        // Once activated, chase like a chaser
        if (_player == null) return;
        _agent.SetDestination(_player.position);

        if (Vector3.Distance(transform.position, _player.position) < 1.5f)
            GameManager.Instance.TriggerCaught();
    }

    bool CanSeePlayer()
    {
        if (_player == null) return false;

        Vector3 toPlayer = _player.position - transform.position;
        if (toPlayer.magnitude > sightRange) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > sightAngle * 0.5f) return false;

        if (_player.TryGetComponent<PlayerHide>(out var hide) && hide.IsHidden) return false;

        return !Physics.Raycast(transform.position + Vector3.up, toPlayer.normalized, toPlayer.magnitude, obstacleMask);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = mode == Mode.Stalker && !_activated ? Color.yellow :
                       mode == Mode.Chaser  ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, mode == Mode.Stalker ? sightRange : wanderRadius);
    }
}
