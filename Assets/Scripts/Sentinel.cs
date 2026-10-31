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
    [Tooltip("Scales incoming body-shot damage only - headshots stay full price. 1 = normal, 0.5 = armored body (teaches 'go for the head' through the fight itself, not just the objective text).")]
    public float bodyDamageMultiplier = 1f;
    [Tooltip("If false, this Sentinel still detects/tracks/faces the player and reacts to being shot, but never fires back - for passive training-range drones that are meant to be shot at without shooting back.")]
    public bool canShoot = true;

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

    [Header("Damage reaction / alert sharing")]
    [Tooltip("How long a Sentinel stays forcibly Alert - turning toward and firing at the player's last known position even with no line of sight - after taking damage or being alerted by another Sentinel in the same room.")]
    public float forcedAlertSeconds = 2.5f;

    [Header("Aim")]
    [Tooltip("Within this distance, shots are near-perfect (miss chance stays at missChanceNear). Beyond it, miss chance ramps up to missChanceAtMaxRange as distance approaches 'range'.")]
    public float perfectAimDistance = 8f;
    [Tooltip("Miss chance (0-1) at/under perfectAimDistance.")]
    public float missChanceNear = 0.03f;
    [Tooltip("Miss chance (0-1) once the player is at this Sentinel's max detection range - interpolated from missChanceNear between perfectAimDistance and range.")]
    public float missChanceAtMaxRange = 0.4f;
    [Header("Blind fire (alerted but can't see the player - smoke, cover, behind)")]
    /// <summary>Random offset around the last known position, metres. Small enough that standing still gets you hit, small enough that a sidestep clears it.</summary>
    public float blindSpreadRadius = 0.5f;
    public float blindAimHeight = 1.0f;



    [Header("Hearing (PlayerFootsteps calls HearNoise on running footsteps)")]
    [Tooltip("An un-alerted Sentinel that hears a running footstep stops and turns toward it for this long. It doesn't fire at the sound - if the turn gives it line of sight, the normal spot/reaction delay applies.")]
    public float investigateSeconds = 2f;

    [Header("Audio")]
    [Tooltip("Looping 3D hover hum, so the player can hear a Sentinel before seeing it. Leave unassigned until Phase 6.")]
    public AudioClip hoverHum;
    public float hoverHumVolume = 0.5f;
    public AudioClip deathSound;

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
    float forcedAlertRemaining;
    float investigateRemaining;
    Vector3 noisePosition;
    /// <summary>Where this Sentinel last knew the player to be - updated every frame it can see them, and
    /// on ForceAlert (being shot / a roommate's call). Blind fire aims here, not at the live position.</summary>
    Vector3 lastKnownPlayerPosition;

    bool isPushing;
    Vector3 pushTarget;
    NavMeshAgent agent;
    WeaponFeedback feedback;
    AudioSource hoverAudioSource;
    /// <summary>Index into RoomManager.rooms this Sentinel belongs to - nearest entry point by distance, computed once in Start(). Gates the hover hum to the player's current room.</summary>
    int ownerRoomIndex = -1;
    PlayerController playerController;


    [Header("Push (set by RoundController after the Breach Charge plants)")]
    public float pushArriveDistance = 1.5f;

    public bool IsBlinded { get { return blindRemaining > 0f; } }
    /// <summary>True while a Recon Dart (or the ultimate) is showing this Sentinel through walls.</summary>
    public bool IsRevealed { get { return revealRemaining > 0f; } }
    /// <summary>Index into RoomManager.rooms this Sentinel belongs to - used by CheckpointManager to only reset Rooms 5-8 defenders on a checkpoint retry.</summary>
    public int OwnerRoomIndex { get { return ownerRoomIndex; } }

    Vector3 spawnPosition;
    Quaternion spawnRotation;


    void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

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
        hoverAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        if (hoverHum != null) hoverAudioSource.clip = hoverHum;
        // Not started here - Update() only plays it once the game is actually running and this
        // Sentinel's room is the player's current room (see ownerRoomIndex in Start()).
    }

    public void Blind(float seconds)
    {
        if (seconds > blindRemaining) blindRemaining = seconds;
    }

    public void Reveal(float seconds)
    {
        if (seconds > revealRemaining) revealRemaining = seconds;
    }

/// <summary>Snaps into Alert and starts turning/firing toward towardPosition for forcedAlertSeconds, even with no current line of sight. Used both for reacting to being shot and (Alert sharing) for being called in by a roommate that just spotted the player.</summary>
    public void ForceAlert(Vector3 towardPosition)
    {
        bool wasAlert = state == State.Alert;
        forcedAlertRemaining = Mathf.Max(forcedAlertRemaining, forcedAlertSeconds);
        lastKnownPlayerPosition = towardPosition;
        if (!wasAlert) fireTimer = Random.Range(reactionDelayMin, reactionDelayMax);
    }

    /// <summary>A running footstep within earshot: stop and look toward it. Ignored once already Alert - it's
    /// already tracking the player by sight or forced alert, and a sound shouldn't pull its aim away.</summary>
    public void HearNoise(Vector3 position)
    {
        if (state == State.Alert || isPushing) return;
        noisePosition = position;
        investigateRemaining = investigateSeconds;
    }

    public bool IsInvestigating { get { return investigateRemaining > 0f && state != State.Alert; } }

/// <summary>Calls every other active Sentinel sharing this one's room in to Alert, turning toward the player's last known position even if they have no sight of their own - a spot by one Sentinel means the whole room reacts, not just the one that saw it.</summary>
    void AlertRoommates(Vector3 lastKnownPosition)
    {
        if (ownerRoomIndex < 0) return;

        var all = Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var s = all[i];
            if (s == this || s.ownerRoomIndex != ownerRoomIndex) continue;
            s.ForceAlert(lastKnownPosition);
        }
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
        if (player != null) playerController = player.GetComponent<PlayerController>();

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }

        if (AudioManager.Instance != null) hoverAudioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;

        if (RoomManager.Instance != null && RoomManager.Instance.rooms != null)
        {
            float bestDist = float.MaxValue;
            var rooms = RoomManager.Instance.rooms;
            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i].entryPoint == null) continue;
                float d = Vector3.Distance(transform.position, rooms[i].entryPoint.position);
                if (d < bestDist) { bestDist = d; ownerRoomIndex = i; }
            }
        }
    }

    void UpdateHoverHum()
    {
        if (hoverAudioSource == null || hoverHum == null) return;

        bool shouldPlay = !MainMenuController.IsOpen
            && RoomManager.Instance != null
            && RoomManager.Instance.CurrentRoom == ownerRoomIndex
            && gameObject.activeSelf;

        if (shouldPlay && !hoverAudioSource.isPlaying) hoverAudioSource.Play();
        else if (!shouldPlay && hoverAudioSource.isPlaying) hoverAudioSource.Stop();
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

        if (forcedAlertRemaining > 0f)
        {
            forcedAlertRemaining -= Time.deltaTime;
            if (forcedAlertRemaining <= 0f) forcedAlertRemaining = 0f;
        }

        if (investigateRemaining > 0f)
        {
            investigateRemaining -= Time.deltaTime;
            if (investigateRemaining <= 0f) investigateRemaining = 0f;
        }

        UpdateHoverHum();

        if (player == null) return;

        bool seesPlayer = CanSeePlayer();
        if (seesPlayer) lastKnownPlayerPosition = player.position;
        bool forcedAlert = forcedAlertRemaining > 0f;
        bool shouldBeAlert = seesPlayer || forcedAlert;
        bool justSpotted = shouldBeAlert && state != State.Alert;
        state = shouldBeAlert ? State.Alert : State.Patrol;

        // A disarmed Patrol dummy (canShoot false) has nothing to lock on for - it should just
        // keep walking its route as a moving target, not freeze to stare at the player like an
        // armed Sentinel or a deliberately-stationary Hold dummy would.
        bool locksOnWhenAlert = canShoot || behaviorMode == BehaviorMode.Hold;

        if (state == State.Alert && locksOnWhenAlert)
        {
            agent.isStopped = true;
            // Line of sight wins when available (the real player position); a forced alert with
            // no current sight falls back to wherever the trigger (damage/roommate call) pointed.
            FaceTowards(seesPlayer ? player.position : lastKnownPlayerPosition);

            if (justSpotted)
            {
                // A fair reaction window - a good peek or a well-timed flash still beats this before the first shot lands.
                fireTimer = Random.Range(reactionDelayMin, reactionDelayMax);
                if (seesPlayer) AlertRoommates(player.position);
            }
            else
            {
                fireTimer -= Time.deltaTime;
                if (fireTimer <= 0f)
                {
                    if (canShoot) Fire();
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
            else if (investigateRemaining > 0f)
            {
                agent.isStopped = true;
                FaceTowards(noisePosition);
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
        if (RoomManager.Instance != null && RoomManager.Instance.EntryGraceActive) return false;

        float eyeOffset = playerController != null ? playerController.EyeHeightOffset : 1f;
        Vector3 eye = transform.position + Vector3.up * 1f;
        Vector3 playerPoint = player.position + Vector3.up * eyeOffset;
        Vector3 toPlayer = playerPoint - eye;
        float distance = toPlayer.magnitude;
        if (distance > range) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > halfAngleDegrees) return false;

        // Smoke blocks sight (but not bullets - see Fire()), so it's added to the mask only here.
        RaycastHit hit;
        if (Physics.Raycast(eye, toPlayer.normalized, out hit, distance, blockingMask | LayerMask.GetMask("Smoke")))
        {
            return false;
        }

        return true;
    }

void Fire()
    {
        Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1f;

        // Alerted but blind (smoke, cover, you're behind it): it can't know where you are now, so
        // it sprays where it last saw you instead of tracking you live.
        if (!CanSeePlayer())
        {
            FireBlind(origin);
            return;
        }

        // Aim at the same point CanSeePlayer() checks, so "it can see you" and "it can hit you"
        // agree: standing behind a crate leaves your head exposed, crouching doesn't.
        float aimHeight = playerController != null ? playerController.EyeHeightOffset : 0.9f;
        Vector3 end = player.position + Vector3.up * aimHeight;

        // Sight and line of fire use slightly different points (eye vs muzzle), so even with sight a
        // crate edge can still be in the way of the shot - it takes the bullet instead of the player.
        RaycastHit blockHit;
        if (Physics.Linecast(origin, end, out blockHit, blockingMask))
        {
            if (feedback != null) feedback.PlayShot(origin, blockHit.point);
            if (BulletImpactPool.Instance != null)
                BulletImpactPool.Instance.SpawnDecal(blockHit.point, blockHit.normal, blockHit.collider.transform, false);
            return;
        }

        if (feedback != null) feedback.PlayShot(origin, end);

        if (Random.value < MissChanceAtCurrentDistance())
        {
            SpawnMissImpact(origin, end);
            return;
        }

        var health = player.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(damagePerHit, transform.position);
    }

    /// <summary>
    /// A shot at the last known position plus a little spread, resolved as a real ray: it only hurts
    /// the player if it physically reaches their collider. Stay put after shooting from smoke and the
    /// burst finds you; sidestep and it chews up the empty spot (bullet holes show where it went).
    /// Smoke isn't in the mask, so these pass through it - cover still stops them.
    /// </summary>
    void FireBlind(Vector3 origin)
    {
        Vector2 jitter = Random.insideUnitCircle * blindSpreadRadius;
        Vector3 aim = lastKnownPlayerPosition + Vector3.up * blindAimHeight
            + new Vector3(jitter.x, Random.Range(-0.3f, 0.3f), jitter.y);
        Vector3 dir = (aim - origin).normalized;
        float maxDistance = range + 15f;

        RaycastHit hit;
        int mask = blockingMask | LayerMask.GetMask("Player");
        if (!Physics.Raycast(origin, dir, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
        {
            if (feedback != null) feedback.PlayShot(origin, origin + dir * maxDistance);
            return;
        }

        if (feedback != null) feedback.PlayShot(origin, hit.point);

        if (hit.collider.transform == player || hit.collider.transform.IsChildOf(player))
        {
            var health = player.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(damagePerHit, transform.position);
        }
        else if (BulletImpactPool.Instance != null)
        {
            BulletImpactPool.Instance.SpawnDecal(hit.point, hit.normal, hit.collider.transform, false);
        }
    }

    /// <summary>A missed shot still has to land somewhere - raycasts past the player along the same line the tracer took and leaves a bullet hole on whatever wall/cover is behind them.</summary>
    void SpawnMissImpact(Vector3 origin, Vector3 end)
    {
        if (BulletImpactPool.Instance == null) return;

        Vector3 dir = (end - origin).normalized;
        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, range + 15f, blockingMask))
        {
            BulletImpactPool.Instance.SpawnDecal(hit.point, hit.normal, hit.collider.transform, false);
        }
    }

    /// <summary>Near-perfect inside perfectAimDistance, ramping up to missChanceAtMaxRange by the time the player is at the edge of detection range - a long-range shot should feel beatable, a close one shouldn't.</summary>
    float MissChanceAtCurrentDistance()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= perfectAimDistance) return missChanceNear;
        if (range <= perfectAimDistance) return missChanceAtMaxRange;

        float t = Mathf.Clamp01((distance - perfectAimDistance) / (range - perfectAimDistance));
        return Mathf.Lerp(missChanceNear, missChanceAtMaxRange, t);
    }

/// <summary>Fired when this Sentinel dies, with whether the killing shot was a headshot - used by room-gate scripts (e.g. WeakPointRoomGate) that need to react to a kill.</summary>
    public event System.Action<bool> OnKilled;

    public void TakeDamage(float amount, bool isHeadshot = false)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            if (deathSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfxAtPoint(deathSound, transform.position);
            }
            gameObject.SetActive(false);
            if (OnKilled != null) OnKilled(isHeadshot);
            return;
        }

        // Getting shot from outside the detection cone/range (through smoke, from behind, a
        // peek-and-reposition) is no longer a free action - react even without line of sight.
        if (player != null) ForceAlert(player.position);
    }


/// <summary>Reverts to full health, original spawn position/facing, and Patrol/Hold idle state - used by CheckpointManager when resetting Rooms 5-8 on a checkpoint retry, regardless of whether this Sentinel was killed, pushed, blinded, or revealed since.</summary>
    public void ResetState()
    {
        currentHealth = maxHealth;
        blindRemaining = 0f;
        revealRemaining = 0f;
        isPushing = false;
        fireTimer = 0f;
        forcedAlertRemaining = 0f;
        investigateRemaining = 0f;

        state = State.Patrol;
        patrolIndex = 0;
        patrolDir = 1;

        gameObject.SetActive(true);
        agent.Warp(spawnPosition);
        transform.rotation = spawnRotation;

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
    }
}
