using UnityEngine;
using UnityEngine.AI;

// Actively hunts the player using NavMesh pathfinding.
// Switches between patrolling and chasing based on line-of-sight.
[RequireComponent(typeof(NavMeshAgent))]
public class SeekerAgent : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float sightRange = 12f;
    [SerializeField] private float sightAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private Transform[] patrolPoints;

    private NavMeshAgent _agent;
    private Transform _player;
    private int _patrolIndex;
    private bool _isChasing;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying()) return;

        if (CanSeePlayer())
        {
            Chase();
        }
        else
        {
            Patrol();
        }
    }

    bool CanSeePlayer()
    {
        if (_player == null) return false;

        Vector3 toPlayer = _player.position - transform.position;
        if (toPlayer.magnitude > sightRange) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > sightAngle * 0.5f) return false;

        // Hidden players block line of sight
        if (_player.TryGetComponent<PlayerHide>(out var hide) && hide.IsHidden) return false;

        return !Physics.Raycast(transform.position + Vector3.up, toPlayer.normalized, toPlayer.magnitude, obstacleMask);
    }

    void Chase()
    {
        _isChasing = true;
        _agent.speed = chaseSpeed;
        _agent.SetDestination(_player.position);

        if (Vector3.Distance(transform.position, _player.position) < 1.5f)
            GameManager.Instance.TriggerCaught();
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        _isChasing = false;
        _agent.speed = patrolSpeed;

        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            _agent.SetDestination(patrolPoints[_patrolIndex].position);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = _isChasing ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}
