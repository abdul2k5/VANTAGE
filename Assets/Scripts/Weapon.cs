using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class Weapon : MonoBehaviour
{
    public int maxAmmo = 12;
    public float fireRate = 0.25f;
    public float range = 50f;
    public float damage = 25f;
    public float reloadSeconds = 1.4f;
    public LayerMask hitMask;

    [Header("Accuracy - 'utility before you peek': only the stopped, patient shot is reliable")]
    public float standingSpreadDegrees = 0f;
    public float movingSpreadDegrees = 4.5f;
    public float fullSpeedForMaxSpread = 5f;
    [Tooltip("Multiplier applied to the moving/standing spread lerp while crouched (below 1 = more accurate). Crouched + stationary is the single most accurate state in the game.")]
    public float crouchSpreadMultiplier = 0.5f;


    [Header("Recoil - a fixed, learnable spray pattern: the first shot is always true, a held spray climbs")]
    [Tooltip("View kick (degrees up) applied AFTER shot N of a continuous spray - index 0 is the kick after the first shot. Past the end, the last value repeats.")]
    public float[] recoilPitchPattern = { 0.9f, 0.9f, 0.8f, 0.6f, 0.4f, 0.25f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f };
    [Tooltip("Sideways kick (degrees, + = right) after shot N - the spray drifts right, then snakes left/right once it tops out.")]
    public float[] recoilYawPattern = { 0f, 0f, 0.1f, 0.3f, 0.5f, -0.7f, -0.7f, 0.7f, 0.7f, -0.7f, -0.7f };
    [Tooltip("A shot fired more than this long after the previous one starts a fresh spray (pattern index back to 0). Must exceed fireRate or a held trigger would never count as a spray.")]
    public float sprayResetSeconds = 0.4f;

    [Header("Headshots")]
    public float headshotMultiplier = 2.5f;
    public AudioClip headshotSound;

    [Header("Audio")]
    public AudioClip gunshotSound;
    public AudioClip reloadSound;
    public AudioClip emptyClickSound;
    public AudioClip hitSound;

    /// <summary>Where the visible tracer/muzzle flash originates. The hit-detection raycast still fires from the camera center.</summary>
    public Transform muzzle;

    int currentAmmo;
    float fireTimer;
    bool isReloading;
    float reloadTimer;
    float currentSpreadDegrees;
    WeaponFeedback feedback;
    AudioSource audioSource;
    CharacterController cc;
    Camera cam;
    PlayerController playerController;
    FollowCamera followCamera;
    int sprayIndex;
    float lastShotTime = -999f;


    public int CurrentAmmo { get { return currentAmmo; } }
    public int MaxAmmo { get { return maxAmmo; } }
    public bool IsReloading { get { return isReloading; } }
    public float ReloadProgress01 { get { return isReloading ? 1f - (reloadTimer / reloadSeconds) : 0f; } }
    /// <summary>0 = standing still/perfect accuracy, 1 = full-speed/max spread. Read by the crosshair to visualize spread.</summary>
    public float CurrentSpreadFraction01 { get; private set; }

    void Awake()
    {
        currentAmmo = maxAmmo;
        feedback = GetComponent<WeaponFeedback>();
        if (feedback == null) feedback = gameObject.AddComponent<WeaponFeedback>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        cc = GetComponent<CharacterController>();
        cam = Camera.main;
        playerController = GetComponent<PlayerController>();
        if (cam != null) followCamera = cam.GetComponentInParent<FollowCamera>();

    }

    void Start()
    {
        if (AudioManager.Instance != null) audioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
    }

    void Update()
    {
        UpdateSpread();

        if (MainMenuController.IsOpen || Cursor.lockState != CursorLockMode.Locked) return;

        fireTimer -= Time.deltaTime;

        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
            {
                isReloading = false;
                currentAmmo = maxAmmo;
            }
        }

        // A click meant for UI (settings, crosshair options) must never also fire the gun.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed && !isReloading && fireTimer <= 0f && currentAmmo > 0)
        {
            Fire();
            fireTimer = fireRate;
        }
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && !isReloading && currentAmmo <= 0)
        {
            PlaySfx(emptyClickSound);
        }

        if (!isReloading && currentAmmo <= 0)
        {
            StartReload();
        }

        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame && !isReloading && currentAmmo < maxAmmo)
        {
            StartReload();
        }
    }

void UpdateSpread()
    {
        bool airborne = cc != null && !cc.isGrounded;
        bool crouching = playerController != null && playerController.IsCrouching;

        float frac01;
        if (airborne)
        {
            // Jump-peeking should never be the accurate option - maximum spread, full stop.
            frac01 = 1f;
        }
        else
        {
            float speed = cc != null ? new Vector2(cc.velocity.x, cc.velocity.z).magnitude : 0f;
            frac01 = fullSpeedForMaxSpread > 0f ? Mathf.Clamp01(speed / fullSpeedForMaxSpread) : 0f;
            if (crouching) frac01 *= crouchSpreadMultiplier;
        }

        currentSpreadDegrees = Mathf.Lerp(standingSpreadDegrees, movingSpreadDegrees, frac01);
        CurrentSpreadFraction01 = frac01;

        if (CrosshairController.Instance != null) CrosshairController.Instance.SetSpreadFraction(frac01);
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    public void Fire()
    {
        currentAmmo--;
        PlaySfx(gunshotSound);
        if (RoomManager.Instance != null) RoomManager.Instance.CancelEntryGrace();
        if (RunStats.Instance != null) RunStats.Instance.RecordShot();
        Vector3 origin = cam != null ? cam.transform.position : transform.position;
        Vector3 forward = cam != null ? cam.transform.forward : transform.forward;
        Vector3 dir = ApplySpread(forward, currentSpreadDegrees);

        RaycastHit hit;
        // Bullets pass through Smoke Walls - smoke blocks sight, not shots. Excluded here rather than
        // only in the Inspector because hitMask is "Everything", which would silently include it.
        bool didHit = Physics.Raycast(origin, dir, out hit, range, hitMask & ~LayerMask.GetMask("Smoke"));

        Vector3 end = didHit ? hit.point : origin + dir * range;

        if (didHit)
        {
            var sentinel = hit.collider.GetComponentInParent<Sentinel>();
            if (sentinel != null)
            {
                var weakpoint = hit.collider.GetComponent<Weakpoint>();
                bool headshot = weakpoint != null;
                float dealt = headshot ? damage * headshotMultiplier : damage * sentinel.bodyDamageMultiplier;

                bool aliveBefore = sentinel.gameObject.activeSelf;
                sentinel.TakeDamage(dealt, headshot);
                if (RunStats.Instance != null) RunStats.Instance.RecordHit(true, headshot);
                if (aliveBefore && !sentinel.gameObject.activeSelf)
                {
                    if (RunStats.Instance != null) RunStats.Instance.RecordKill();
                    // Unarmed practice drones give nothing - the Weak Point door needs all 4 dead, which would fill the ult by Room 3.
                    if (sentinel.canShoot && UltimateController.Instance != null) UltimateController.Instance.AddCharge(UltimateController.Instance.chargePerKill);
                    if (KillFeedUI.Instance != null) KillFeedUI.Instance.AddKill(headshot);
                }

                if (HitFeedbackUI.Instance != null) HitFeedbackUI.Instance.ShowHitMarker(headshot);
                PlaySfx(headshot ? headshotSound : hitSound);

                if (BulletImpactPool.Instance != null) BulletImpactPool.Instance.SpawnSentinelSpark(hit.point, hit.normal);
            }
            else if (BulletImpactPool.Instance != null)
            {
                bool isRangeTarget = hit.collider.transform.root.name.Contains("RangeTarget") || hit.collider.name.Contains("RangeTarget");
                BulletImpactPool.Instance.SpawnDecal(hit.point, hit.normal, hit.collider.transform, isRangeTarget);

                var rangeTarget = hit.collider.GetComponentInParent<RangeTarget>();
                if (rangeTarget != null)
                {
                    rangeTarget.RegisterHit();
                    if (RunStats.Instance != null) RunStats.Instance.RecordHit(false, false);
                }
            }
        }

        // Raycast stays camera-centered for accuracy; the visible tracer starts at the gun's muzzle.
        Vector3 visualOrigin = muzzle != null ? muzzle.position : origin;
        if (feedback != null) feedback.PlayShot(visualOrigin, end);

        ApplyRecoil();
    }

    /// <summary>
    /// Kicks the view after the shot, so this shot went where the crosshair was and the next one goes where
    /// it has climbed to. Because the kick moves the camera (not a hidden bullet offset), the player sees the
    /// climb and can pull down against it - spraying is controllable with practice, tapping is reliable.
    /// </summary>
    void ApplyRecoil()
    {
        if (Time.time - lastShotTime > sprayResetSeconds) sprayIndex = 0;
        lastShotTime = Time.time;

        if (followCamera != null)
        {
            followCamera.AddRecoil(PatternValue(recoilPitchPattern, sprayIndex), PatternValue(recoilYawPattern, sprayIndex));
        }
        sprayIndex++;
    }

    static float PatternValue(float[] pattern, int index)
    {
        if (pattern == null || pattern.Length == 0) return 0f;
        return pattern[Mathf.Min(index, pattern.Length - 1)];
    }

    Vector3 ApplySpread(Vector3 forward, float spreadDegrees)
    {
        if (spreadDegrees <= 0f || cam == null) return forward;

        float pitch = Random.Range(-spreadDegrees, spreadDegrees);
        float yaw = Random.Range(-spreadDegrees, spreadDegrees);
        Quaternion deviation = Quaternion.AngleAxis(yaw, cam.transform.up) * Quaternion.AngleAxis(pitch, cam.transform.right);
        return deviation * forward;
    }

    public void StartReload()
    {
        isReloading = true;
        reloadTimer = reloadSeconds;
        PlaySfx(reloadSound);
    }

    /// <summary>Instant reload, kept for callers (e.g. RoomManager on respawn) that want ammo topped up with no delay.</summary>
    public void Reload()
    {
        isReloading = false;
        currentAmmo = maxAmmo;
    }
}
