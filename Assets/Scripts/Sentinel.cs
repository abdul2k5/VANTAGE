using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Sentinel : MonoBehaviour
{
    public Transform player;
    public LayerMask blockingMask;
    public float range = 6f;
    public float halfAngleDegrees = 28f;
    public float damagePerHit = 20f;
    public float fireInterval = 0.8f;
    public float maxHealth = 50f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float turnSpeed = 8f;
    public float waypointArriveDistance = 0.3f;

    enum State { Patrol, Alert }
    State state = State.Patrol;
    int patrolIndex;
    int patrolDir = 1;

    /// <summary>Where the tracer/muzzle flash originates. Falls back to eye height if unset.</summary>
    public Transform muzzle;

    float fireTimer;
    float currentHealth;
    float blindRemaining;
    NavMeshAgent agent;
    WeaponFeedback feedback;

    public bool IsBlinded { get { return blindRemaining > 0f; } }

    void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // facing is driven manually so Alert can snap to the player
        agent.speed = patrolSpeed;
        feedback = GetComponent<WeaponFeedback>();
        if (feedback == null) feedback = gameObject.AddComponent<WeaponFeedback>();
    }

    public void Blind(float seconds)
    {
        if (seconds > blindRemaining) blindRemaining = seconds;
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
    }

    void Update()
    {
        if (blindRemaining > 0f)
        {
            blindRemaining -= Time.deltaTime;
            if (blindRemaining <= 0f) blindRemaining = 0f;
        }

        if (player == null) return;

        bool seesPlayer = CanSeePlayer();
        state = seesPlayer ? State.Alert : State.Patrol;

        if (state == State.Alert)
        {
            agent.isStopped = true;
            FaceTowards(player.position);

            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                Fire();
                fireTimer = fireInterval;
            }
        }
        else
        {
            fireTimer = 0f;
            RunPatrol();
        }
    }

    void RunPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        agent.isStopped = false;

        if (agent.velocity.sqrMagnitude > 0.05f)
        {
            FaceTowards(transform.position + agent.velocity.normalized);
        }

        if (!agent.pathPending && agent.remainingDistance <= waypointArriveDistance)
        {
            AdvanceWaypoint();
        }
    }

    void AdvanceWaypoint()
    {
        if (patrolPoints.Length == 1) return;

        patrolIndex += patrolDir;
        if (patrolIndex >= patrolPoints.Length)
        {
            patrolIndex = patrolPoints.Length - 2;
            patrolDir = -1;
        }
        else if (patrolIndex < 0)
        {
            patrolIndex = 1;
            patrolDir = 1;
        }

        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    void FaceTowards(Vector3 worldPoint)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    public bool CanSeePlayer()
    {
        if (blindRemaining > 0f) return false;

        Vector3 eye = transform.position + Vector3.up * 1f;
        Vector3 playerPoint = player.position + Vector3.up * 1f;
        Vector3 toPlayer = playerPoint - eye;
        float distance = toPlayer.magnitude;
        if (distance > range) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > halfAngleDegrees) return false;

        RaycastHit hit;
        if (Physics.Raycast(eye, toPlayer.normalized, out hit, distance, blockingMask))
        {
            return false;
        }

        return true;
    }

    void Fire()
    {
        Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1f;
        Vector3 end = player.position + Vector3.up * 0.9f;
        if (feedback != null) feedback.PlayShot(origin, end);

        var health = player.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(damagePerHit);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            gameObject.SetActive(false);
        }
    }
}
