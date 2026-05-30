using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Procedural walk animation for agents without animation clips.
/// Bobs the body up/down and sways arms based on NavMeshAgent speed.
/// Attach to the same GameObject as SeekerAgent.
/// </summary>
public class AgentAnimator : MonoBehaviour
{
    [Header("Bob Settings")]
    [SerializeField] private float bobFrequency  = 2.0f;  // steps per second
    [SerializeField] private float bobAmplitude  = 0.06f; // up/down height
    [SerializeField] private float swayAmplitude = 3.0f;  // arm rotation degrees

    private NavMeshAgent _agent;
    private Transform    _npcModel;
    private Vector3      _modelBasePos;
    private float        _bobTimer;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        // Find the NPC model child
        var modelTransform = transform.Find("NPCModel");
        if (modelTransform != null)
        {
            _npcModel    = modelTransform;
            _modelBasePos = modelTransform.localPosition;
        }
    }

    void Update()
    {
        if (_npcModel == null) return;

        float speed = _agent != null ? _agent.velocity.magnitude : 0f;
        bool isMoving = speed > 0.1f;

        if (isMoving)
        {
            // Speed up bob with movement speed
            _bobTimer += Time.deltaTime * bobFrequency * (speed / Mathf.Max(_agent.speed, 0.1f));

            float bob = Mathf.Sin(_bobTimer * Mathf.PI * 2f) * bobAmplitude;
            _npcModel.localPosition = _modelBasePos + new Vector3(0, bob, 0);

            // Slight left-right body sway
            float sway = Mathf.Sin(_bobTimer * Mathf.PI) * swayAmplitude * 0.3f;
            _npcModel.localRotation = Quaternion.Euler(0, 0, sway);
        }
        else
        {
            // Idle: gentle breathing effect
            float breathe = Mathf.Sin(Time.time * 1.2f) * 0.01f;
            _npcModel.localPosition = Vector3.Lerp(_npcModel.localPosition,
                _modelBasePos + new Vector3(0, breathe, 0), Time.deltaTime * 3f);
            _npcModel.localRotation = Quaternion.Lerp(_npcModel.localRotation,
                Quaternion.identity, Time.deltaTime * 3f);
        }
    }
}
