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

    public enum BehaviorMode { Patrol, Hold }
    [Header("Behavior")]
    [Tooltip("Patrol = walks a waypoint loop. Hold = stays put watching a doorway/angle - set Hold Point below.")]
    public BehaviorMode behaviorMode = BehaviorMode.Patrol;
    [Tooltip("Only used in Hold mode. The direction the Sentinel watches while idle. Leave empty to hold its starting facing.")]
    public Transform holdPoint;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float turnSpeed = 8f;
    public float waypointArriveDistance = 0.3f;

    [Header("Reaction")]
    [Tooltip("Delay before the Sentinel's first shot after spotting the player, so a good peek or a flash still has a window.")]
    public float reactionDelayMin = 0.3f;
    public float reactionDelayMax = 0.5f;

    [Header("Audio")]
    [Tooltip("Looping 3D hover hum, so the player can hear a Sentinel before seeing it. Leave unassigned until Phase 6.")]
    public AudioClip hoverHum;
    public float hoverHumVolume = 0.5f;

    enum State { Patrol, Alert }
    State state = State.Patrol;
    int patrolIndex;
    int patrolDir = 1;

    /// <summary>Where the tracer/muzzle flash originates. Falls back to eye height if unset.</summary>
    public Transform muzzle;

    float fireTimer;
    float currentHealth;
    float blindRemaining;
    float revealRemaining;
    bool isPushing;
    Vector3 pushTarget;
    NavMeshAgent agent;
    WeaponFeedback feedback;
    AudioSource hoverAudioSource;

    [Header("Push (set by RoundController after the Breach Charge plants)")]
    public float pushArriveDistance = 1.5f;

    public bool IsBlinded { get { return blindRemaining > 0f; } }
    /// <summary>True while a Recon Dart (or the ultimate) is showing this Sentinel through walls.</summary>
    public bool IsRevealed { get { return revealRemaining > 0f; } }

    void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // facing is driven manually so Alert can snap to the player
        agent.speed = patrolSpeed;
        feedback = GetComponent<WeaponFeedback>();
        if (feedback == null) feedback = gameObject.AddComponent<WeaponFeedback>();

        hoverAudioSource = gameObject.AddComponent<AudioSource>();
        hoverAudioSource.loop = true;
        hoverAudioSource.playOnAwake = false;
        hoverAudioSource.spatialBlend = 1f; // 3D - hear it before you see it
        hoverAudioSource.volume = hoverHumVolume;
        hoverAudioSource.minDistance = 2f;
        hoverAudioSource.maxDistance = 14f;
        if (hoverHum != null)
        {
            hoverAudioSource.clip = hoverHum;
            hoverAudioSource.Play();
        }
    }

    public void Blind(float seconds)
    {
        if (seconds > blindRemaining) blindRemaining = seconds;
    }

    public void Reveal(float seconds)
    {
        if (seconds > revealRemaining) revealRemaining = seconds;
    }

    /// <summary>Overrides Patrol/Hold and moves straight to point - used once the Breach Charge is planted. Resumes normal behavior from the new position on arrival, or immediately if it spots the player en route.</summary>
    public void PushTowards(Vector3 point)
    {
        isPushing = true;
        pushTarget = point;
        agent.SetDestination(point);
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

        if (revealRemaining > 0f)
        {
            revealRemaining -= Time.deltaTime;
            if (revealRemaining <= 0f) revealRemaining = 0f;
        }

        if (player == null) return;

        bool seesPlayer = CanSeePlayer();
        bool justSpotted = seesPlayer && state != State.Alert;
        state = seesPlayer ? State.Alert : State.Patrol;

        if (state == State.Alert)
        {
            agent.isStopped = true;
            FaceTowards(player.position);

            if (justSpotted)
            {
                // A fair reaction window - a good peek or a well-timed flash beats this before the first shot lands.
                fireTimer = Random.Range(reactionDelayMin, reactionDelayMax);
            }
            else
            {
                fireTimer -= Time.deltaTime;
                if (fireTimer <= 0f)
                {
                    Fire();
                    fireTimer = fireInterval;
                }
            }
        }
        else
        {
            fireTimer = 0f;
            if (isPushing)
            {
                RunPush();
            }
            else if (behaviorMode == BehaviorMode.Hold)
            {
                RunHold();
            }
            else
            {
                RunPatrol();
            }
        }
    }

    void RunPush()
    {
        agent.isStopped = false;

        if (agent.velocity.sqrMagnitude > 0.05f)
        {
            FaceTowards(transform.position + agent.velocity.normalized);
        }

        if (!agent.pathPending && Vector3.Distance(transform.position, pushTarget) <= pushArriveDistance)
        {
            isPushing = false; // arrived - falls back to Patrol/Hold from here, which reads as "now defending the site"
        }
    }

    void RunHold()
    {
        agent.isStopped = true;
        if (holdPoint != null) FaceTowards(holdPoint.position);
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
        if (health != null) health.TakeDamage(damagePerHit, transform.position);
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
