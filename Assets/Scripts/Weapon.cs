using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>One gun's numbers. Weapon copies these over its own fields when the gun is drawn, so the rest of the game keeps reading Weapon as before.</summary>
[System.Serializable]
public class GunProfile
{
    public string displayName = "RIFLE";
    [Tooltip("RoomManager room index the gun is first carried in. 0 = from the start.")]
    public int unlockRoomIndex;
    public int maxAmmo = 12;
    public float fireRate = 0.25f;
    [Tooltip("Off = one shot per click.")]
    public bool automatic = true;
    public float damage = 25f;
    public float reloadSeconds = 1.4f;
    public float standingSpreadDegrees;
    public float movingSpreadDegrees = 4.5f;
    public float[] recoilPitchPattern;
    public float[] recoilYawPattern;
    public AudioClip gunshotSound;
    [Range(0f, 1f)] public float gunshotVolume = 0.75f;
}

public class Weapon : MonoBehaviour
{
    [Header("Guns - [1] [2] [3] or the mouse wheel. The fields below describe the gun in hand; left empty, they are the only gun")]
    public GunProfile[] guns;
    [Tooltip("Time to put one gun away and bring the next up. The gun can't fire, aim or reload during it.")]
    public float switchSeconds = 0.4f;
    public AudioClip switchSound;

    public int maxAmmo = 12;
    public float fireRate = 0.25f;
    [Tooltip("Off = one shot per click.")]
    public bool automatic = true;
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

    [Header("Aiming - hold right-click. Zooms for small targets, but doesn't change where shots go")]
    public float aimInSeconds = 0.2f;

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
    [Tooltip("The layered shot peaks about 70% higher than the old laser clip. This keeps it heavier without drowning the drones.")]
    [Range(0f, 1f)] public float gunshotVolume = 0.75f;
    [Tooltip("First reload beat: the old magazine comes out.")]
    public AudioClip reloadSound;
    [Tooltip("Second reload beat: the new magazine goes in.")]
    public AudioClip reloadMagInSound;
    [Tooltip("Last reload beat, on the frame the ammo refills, so the player can hear when the gun is ready.")]
    public AudioClip reloadReadySound;
    [Tooltip("These clips peak about 60% higher than the gunshot, and a reload shouldn't be louder than the gun.")]
    [Range(0f, 1f)] public float reloadVolume = 0.6f;
    public AudioClip emptyClickSound;
    public AudioClip hitSound;

    [Header("Reload timing - shared with GunViewmodelAnimator so sound and animation stay in step")]
    [Range(0f, 1f)] public float reloadMagOutAt = 0.2f;
    [Range(0f, 1f)] public float reloadMagInAt = 0.62f;

    /// <summary>Where the visible tracer/muzzle flash originates. The hit-detection raycast still fires from the camera center.</summary>
    public Transform muzzle;

    int currentAmmo;
    float fireTimer;
    bool isReloading;
    float reloadTimer;
    int reloadBeatsPlayed;
    float currentSpreadDegrees;
    WeaponFeedback feedback;
    AudioSource audioSource;
    CharacterController cc;
    Camera cam;
    PlayerController playerController;
    FollowCamera followCamera;
    AbilityController abilities;
    GunViewmodelAnimator viewmodel;
    int sprayIndex;
    float lastShotTime = -999f;

    int currentGun;
    int pendingGun = -1;
    float switchTimer;
    int[] ammoByGun;
    bool[] gunAnnounced;
    int unlockedThroughRoom;

    public int CurrentAmmo { get { return currentAmmo; } }
    public int MaxAmmo { get { return maxAmmo; } }
    public bool IsReloading { get { return isReloading; } }
    public float ReloadProgress01 { get { return isReloading ? 1f - (reloadTimer / reloadSeconds) : 0f; } }
    /// <summary>0 = standing still/perfect accuracy, 1 = full-speed/max spread. Read by the crosshair to visualize spread.</summary>
    public float CurrentSpreadFraction01 { get; private set; }
    /// <summary>True while right-click is held and the gun can be aimed. Reloading drops out of it.</summary>
    public bool IsAiming { get; private set; }
    /// <summary>0 = at the hip, 1 = fully aimed. Read by GunViewmodelAnimator and PlayerController.</summary>
    public float AimAmount01 { get; private set; }
    /// <summary>True while the Flash is readied in hand - the gun is lowered and can't fire or aim.</summary>
    public bool HoldingFlash { get { return abilities != null && abilities.flash != null && abilities.flash.IsAiming; } }

    public int GunCount { get { return guns != null ? guns.Length : 0; } }
    public int CurrentGunIndex { get { return currentGun; } }
    public bool IsSwitching { get { return switchTimer > 0f; } }
    /// <summary>0 = gun up, 1 = gun fully down (the moment the guns are swapped). Read by GunViewmodelAnimator.</summary>
    public float SwitchLowered01
    {
        get
        {
            if (switchTimer <= 0f || switchSeconds <= 0f) return 0f;
            float progress = 1f - switchTimer / switchSeconds;
            return 1f - Mathf.Abs(1f - 2f * progress);
        }
    }

    public bool IsGunUnlocked(int index)
    {
        return index >= 0 && index < GunCount && guns[index].unlockRoomIndex <= unlockedThroughRoom;
    }

    /// <summary>Name shown on the HUD for gun index.</summary>
    public string GunName(int index)
    {
        return index >= 0 && index < GunCount ? guns[index].displayName : "";
    }

    void Awake()
    {
        if (GunCount > 0)
        {
            ammoByGun = new int[guns.Length];
            gunAnnounced = new bool[guns.Length];
            for (int i = 0; i < guns.Length; i++) ammoByGun[i] = guns[i].maxAmmo;
            currentAmmo = ammoByGun[0];
            ApplyGun(0);
        }
        else
        {
            currentAmmo = maxAmmo;
        }
        feedback = GetComponent<WeaponFeedback>();
        if (feedback == null) feedback = gameObject.AddComponent<WeaponFeedback>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        cc = GetComponent<CharacterController>();
        cam = Camera.main;
        playerController = GetComponent<PlayerController>();
        abilities = GetComponent<AbilityController>();
        if (cam != null) followCamera = cam.GetComponentInParent<FollowCamera>();
        if (cam != null) viewmodel = cam.GetComponentInChildren<GunViewmodelAnimator>();

    }

    void Start()
    {
        if (AudioManager.Instance != null) audioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
    }

    void Update()
    {
        UpdateSpread();
        UpdateAim();

        if (MainMenuController.IsOpen || Cursor.lockState != CursorLockMode.Locked) return;

        fireTimer -= Time.deltaTime;

        UpdateGunSwitch();
        if (IsSwitching) return;

        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadBeatsPlayed == 0 && ReloadProgress01 >= reloadMagOutAt)
            {
                PlaySfx(reloadSound, reloadVolume);
                reloadBeatsPlayed = 1;
            }
            if (reloadBeatsPlayed == 1 && ReloadProgress01 >= reloadMagInAt)
            {
                PlaySfx(reloadMagInSound, reloadVolume);
                reloadBeatsPlayed = 2;
            }
            if (reloadTimer <= 0f)
            {
                isReloading = false;
                currentAmmo = maxAmmo;
                PlaySfx(reloadReadySound, reloadVolume);
            }
        }

        // A click meant for UI (settings, crosshair options) must never also fire the gun.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var mouse = Mouse.current;
        if (HoldingFlash) return;

        bool trigger = mouse != null && (automatic ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame);
        if (trigger && !isReloading && fireTimer <= 0f && currentAmmo > 0)
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

    /// <summary>Number keys pick a gun, the wheel steps through the unlocked ones. The swap itself happens half-way through the switch, with the gun out of sight.</summary>
    void UpdateGunSwitch()
    {
        if (GunCount > 1 && !HoldingFlash)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            int wanted = -1;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) wanted = 0;
                else if (kb.digit2Key.wasPressedThisFrame) wanted = 1;
                else if (kb.digit3Key.wasPressedThisFrame) wanted = 2;
            }
            if (wanted < 0 && mouse != null && !IsSwitching)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (wheel != 0f) wanted = NextUnlockedGun(wheel < 0f ? 1 : -1);
            }
            SwitchTo(wanted);
        }

        if (switchTimer <= 0f) return;
        switchTimer -= Time.deltaTime;
        if (pendingGun >= 0 && switchTimer <= switchSeconds * 0.5f)
        {
            ApplyGun(pendingGun);
            pendingGun = -1;
        }
    }

    int NextUnlockedGun(int step)
    {
        for (int i = 1; i < GunCount; i++)
        {
            int index = ((currentGun + step * i) % GunCount + GunCount) % GunCount;
            if (IsGunUnlocked(index)) return index;
        }
        return currentGun;
    }

    /// <summary>Starts putting the gun in hand away for gun index. Ignored if that gun is locked or already the one coming up.</summary>
    public void SwitchTo(int index)
    {
        // The second half of a switch is the new gun coming up - let it arrive before starting another.
        if (IsSwitching && pendingGun < 0) return;
        int target = pendingGun >= 0 ? pendingGun : currentGun;
        if (index == target || !IsGunUnlocked(index)) return;

        // A reload in progress is lost - the new magazine wasn't in yet.
        isReloading = false;
        // Changing your mind while the old gun is still going down only changes which one comes up.
        if (!IsSwitching) switchTimer = switchSeconds;
        pendingGun = index;
        PlaySfx(switchSound, reloadVolume);
    }

    /// <summary>Puts gun index in hand: its numbers replace this component's, and its own magazine comes with it.</summary>
    void ApplyGun(int index)
    {
        if (ammoByGun != null && currentGun < ammoByGun.Length) ammoByGun[currentGun] = currentAmmo;
        currentGun = index;
        var gun = guns[index];
        maxAmmo = gun.maxAmmo;
        fireRate = gun.fireRate;
        automatic = gun.automatic;
        damage = gun.damage;
        reloadSeconds = gun.reloadSeconds;
        standingSpreadDegrees = gun.standingSpreadDegrees;
        movingSpreadDegrees = gun.movingSpreadDegrees;
        if (gun.recoilPitchPattern != null && gun.recoilPitchPattern.Length > 0) recoilPitchPattern = gun.recoilPitchPattern;
        if (gun.recoilYawPattern != null && gun.recoilYawPattern.Length > 0) recoilYawPattern = gun.recoilYawPattern;
        if (gun.gunshotSound != null) gunshotSound = gun.gunshotSound;
        gunshotVolume = gun.gunshotVolume;
        currentAmmo = ammoByGun != null ? ammoByGun[index] : maxAmmo;
        isReloading = false;
        fireTimer = 0f;
        sprayIndex = 0;
    }

    /// <summary>Called on every room entry, like AbilityController.UnlockForRoom: which guns are carried depends only on the room,
    /// so a checkpoint retry or a replayed room gets it right for free. Announces a gun the first time its room is reached.</summary>
    public void UnlockForRoom(int roomIndex)
    {
        unlockedThroughRoom = roomIndex;
        if (GunCount == 0) return;

        if (!IsGunUnlocked(currentGun) || (pendingGun >= 0 && !IsGunUnlocked(pendingGun)))
        {
            pendingGun = -1;
            switchTimer = 0f;
            if (currentGun != 0) ApplyGun(0);
        }

        for (int i = 1; i < guns.Length; i++)
        {
            if (gunAnnounced[i] || !IsGunUnlocked(i)) continue;
            gunAnnounced[i] = true;
            if (KillFeedUI.Instance != null) KillFeedUI.Instance.AddMessage("NEW WEAPON: " + guns[i].displayName + "  -  PRESS [" + (i + 1) + "]");
        }
    }

    void UpdateAim()
    {
        var mouse = Mouse.current;
        // Right-click adds bounces while the Recon Dart is being aimed, so it can't also zoom.
        bool aimingDart = abilities != null && abilities.reconDart != null && abilities.reconDart.IsAiming;
        IsAiming = mouse != null && mouse.rightButton.isPressed && !isReloading && !IsSwitching && !aimingDart && !HoldingFlash
            && !MainMenuController.IsOpen && Cursor.lockState == CursorLockMode.Locked;

        // Real time when paused, so a card or Game Over opening mid-aim doesn't leave the scope view stuck behind it.
        float dt = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
        float rate = aimInSeconds > 0f ? dt / aimInSeconds : 1f;
        AimAmount01 = Mathf.MoveTowards(AimAmount01, IsAiming ? 1f : 0f, rate);
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

    void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
    }

    public void Fire()
    {
        currentAmmo--;
        PlaySfx(gunshotSound, gunshotVolume);
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
                // Whichever collider caught the bullet, it's a headshot only if it touches the eye you can see.
                var weakpoint = sentinel.GetComponentInChildren<Weakpoint>();
                bool headshot = weakpoint != null && weakpoint.IsHitBy(new Ray(origin, dir), range);
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
        // Scoped, the muzzle is at the player's eye and the tracer would cover the target. Start it ahead and below, so it reads as a streak rising to the crosshair.
        if (viewmodel != null && viewmodel.ScopeViewUp) visualOrigin = origin + forward * 2.5f - cam.transform.up * 0.25f;
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
        reloadBeatsPlayed = 0;
    }

    /// <summary>Instant reload, kept for callers (e.g. RoomManager on respawn) that want ammo topped up with no delay.</summary>
    public void Reload()
    {
        isReloading = false;
        currentAmmo = maxAmmo;
        // Every gun carried, not only the one in hand.
        if (ammoByGun != null) for (int i = 0; i < ammoByGun.Length; i++) ammoByGun[i] = guns[i].maxAmmo;
    }
}
