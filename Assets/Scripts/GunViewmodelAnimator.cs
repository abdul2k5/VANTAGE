using UnityEngine;

/// <summary>Poses the first-person gun for aiming and reloading, entirely from Weapon.AimAmount01 and Weapon.ReloadProgress01. Because the pose is worked out from those each frame, it pauses when the reload pauses, and an instant Weapon.Reload() puts the gun straight back.</summary>
public class GunViewmodelAnimator : MonoBehaviour
{
    /// <summary>One gun's model and how it is aimed. Listed in the same order as Weapon.guns.</summary>
    [System.Serializable]
    public class GunVisual
    {
        public Transform root;
        public Transform magazine;
        [Tooltip("Where this gun's tracer starts.")]
        public Transform muzzle;
        [Tooltip("Gun position when fully aimed.")]
        public Vector3 aimPosition;
        public float aimFieldOfView = 35f;
        [Tooltip("On = the scope view replaces the gun when fully aimed. Off = the gun stays in view, raised to the eye.")]
        public bool scoped = true;
    }

    public Weapon weapon;
    public Transform magazine;
    [Tooltip("Optional. With more than one gun to pick from, the entry for the gun in hand overrides Magazine and the aiming fields below.")]
    public GunVisual[] gunVisuals;

    [Header("Aiming")]
    [Tooltip("Gun position when fully aimed: the back of the scope up against the player's eye. The gun is hidden once the scope view has covered it.")]
    public Vector3 aimPosition = new Vector3(0f, -0.146f, 0.12f);
    public float aimFieldOfView = 35f;
    public ScopeOverlay scopeOverlay;
    [Tooltip("Aim amount at which the scope view starts to fade in.")]
    [Range(0f, 1f)] public float scopeFadeStart = 0.6f;
    [Tooltip("Aim amount at which the scope view is fully in and the gun disappears.")]
    [Range(0f, 1f)] public float scopeFullAt = 0.9f;

    [Header("Holding the flash - gun drops to free the other hand")]
    public Vector3 loweredPositionOffset = new Vector3(0.06f, -0.2f, -0.08f);
    public Vector3 loweredTiltDegrees = new Vector3(25f, -12f, 0f);
    public float lowerSeconds = 0.12f;

    [Header("Planting the charge - gun put away, both hands are on the charge")]
    [Tooltip("Must end with the whole gun below the bottom of the screen.")]
    public Vector3 stowedPositionOffset = new Vector3(0.05f, -0.65f, -0.1f);
    public Vector3 stowedTiltDegrees = new Vector3(55f, -20f, 0f);
    public float stowSeconds = 0.22f;

    [Header("Reload pose")]
    public Vector3 reloadPositionOffset = new Vector3(-0.05f, -0.07f, -0.04f);
    public Vector3 reloadTiltDegrees = new Vector3(-22f, 8f, 14f);
    [Tooltip("Reload progress at which the gun starts coming back up.")]
    [Range(0f, 1f)] public float returnStartsAt = 0.78f;

    [Header("Magazine")]
    [Tooltip("How far the old magazine falls, in metres. Must end off screen.")]
    public float magazineDropDistance = 0.52f;
    [Tooltip("Where the new magazine starts below the gun, in metres. Must start off screen.")]
    public float magazineInsertDistance = 0.48f;
    [Tooltip("Share of the reload the old magazine takes to fall.")]
    public float magazineFallDuration = 0.16f;
    [Tooltip("Share of the reload the new magazine takes to slide in, ending exactly on the mag-in sound.")]
    public float magazineSlideDuration = 0.24f;

    [Header("Seating the magazine")]
    [Tooltip("How far the gun bumps up when the new magazine goes in, in metres.")]
    public float seatJoltDistance = 0.012f;
    [Tooltip("Share of the reload the bump lasts.")]
    public float seatJoltDuration = 0.1f;

    Vector3 basePosition;
    Quaternion baseRotation;
    Vector3 magazineBasePosition;
    Vector3 magazineDownPerMetre;
    Camera cam;
    float baseFieldOfView;
    Renderer[] gunRenderers;
    bool gunHidden;
    float lowered;
    float stowed;
    int shownGun = -1;
    bool scoped = true;

    /// <summary>True once the scope view has fully replaced the gun. Weapon starts its tracer elsewhere then, since the muzzle is right in front of the player's eye.</summary>
    public bool ScopeViewUp { get { return gunHidden; } }

    void Awake()
    {
        gunRenderers = GetComponentsInChildren<Renderer>(true);
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
        cam = GetComponentInParent<Camera>();
        if (cam != null) baseFieldOfView = cam.fieldOfView;

        CacheMagazine();
        if (gunVisuals != null && gunVisuals.Length > 0) ShowGun(0);
    }

    void CacheMagazine()
    {
        if (magazine == null) return;
        magazineBasePosition = magazine.localPosition;
        // The magazine's parent is the model, which may be scaled (the rifle's is 0.01), so one metre down the gun is worked out in its units.
        magazineDownPerMetre = magazine.parent.InverseTransformVector(-transform.up);
    }

    /// <summary>Swaps the model in view to the gun picked for the run.</summary>
    void ShowGun(int index)
    {
        if (index < 0 || index >= gunVisuals.Length) return;

        // Leave the outgoing gun as it was found: magazine seated, nothing hidden by the scope view.
        if (magazine != null && shownGun >= 0) magazine.localPosition = magazineBasePosition;
        if (gunRenderers != null) for (int i = 0; i < gunRenderers.Length; i++) gunRenderers[i].enabled = true;
        gunHidden = false;

        shownGun = index;
        for (int i = 0; i < gunVisuals.Length; i++)
        {
            if (gunVisuals[i].root != null) gunVisuals[i].root.gameObject.SetActive(i == index);
        }

        var visual = gunVisuals[index];
        gunRenderers = visual.root != null ? visual.root.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        magazine = visual.magazine;
        CacheMagazine();
        aimPosition = visual.aimPosition;
        aimFieldOfView = visual.aimFieldOfView;
        scoped = visual.scoped;
        if (visual.muzzle != null && weapon != null) weapon.muzzle = visual.muzzle;
    }

    void LateUpdate()
    {
        if (weapon == null) return;

        if (gunVisuals != null && gunVisuals.Length > 0 && weapon.CurrentGunIndex != shownGun) ShowGun(weapon.CurrentGunIndex);

        float progress = weapon.IsReloading ? weapon.ReloadProgress01 : 0f;

        float weight = 0f;
        if (progress > 0f)
        {
            float raise = Mathf.SmoothStep(0f, 1f, progress / weapon.reloadMagOutAt);
            float lower = Mathf.SmoothStep(0f, 1f, (progress - returnStartsAt) / (1f - returnStartsAt));
            weight = raise * (1f - lower);
        }

        float jolt = 0f;
        float sinceSeated = (progress - weapon.reloadMagInAt) / seatJoltDuration;
        if (sinceSeated > 0f && sinceSeated < 1f) jolt = Mathf.Sin(sinceSeated * Mathf.PI) * seatJoltDistance;

        float aim = Mathf.SmoothStep(0f, 1f, weapon.AimAmount01);
        if (cam != null) cam.fieldOfView = Mathf.Lerp(baseFieldOfView, aimFieldOfView, aim);
        if (scopeOverlay != null) scopeOverlay.SetAmount(scoped ? Mathf.InverseLerp(scopeFadeStart, scopeFullAt, aim) : 0f);
        SetGunHidden(scoped && aim >= scopeFullAt);

        float lowerRate = lowerSeconds > 0f ? Time.deltaTime / lowerSeconds : 1f;
        lowered = Mathf.MoveTowards(lowered, weapon.HoldingFlash ? 1f : 0f, lowerRate);
        float low = Mathf.SmoothStep(0f, 1f, lowered);

        float stowRate = stowSeconds > 0f ? Time.deltaTime / stowSeconds : 1f;
        stowed = Mathf.MoveTowards(stowed, BreachChargeSite.AnyPlantInProgress ? 1f : 0f, stowRate);
        float stow = Mathf.SmoothStep(0f, 1f, stowed);

        transform.localPosition = Vector3.Lerp(basePosition, aimPosition, aim) + reloadPositionOffset * weight + loweredPositionOffset * low + stowedPositionOffset * stow + Vector3.up * jolt;
        transform.localRotation = baseRotation * Quaternion.Euler(reloadTiltDegrees * weight + loweredTiltDegrees * low + stowedTiltDegrees * stow);

        if (magazine != null) magazine.localPosition = magazineBasePosition + magazineDownPerMetre * MagazineDrop(progress);
    }

    void SetGunHidden(bool hidden)
    {
        if (hidden == gunHidden) return;
        gunHidden = hidden;
        for (int i = 0; i < gunRenderers.Length; i++) gunRenderers[i].enabled = !hidden;
    }

    /// <summary>Metres below the gun: the old magazine accelerates away, then the new one eases up and seats on the mag-in beat.</summary>
    float MagazineDrop(float progress)
    {
        if (progress <= weapon.reloadMagOutAt || progress >= weapon.reloadMagInAt) return 0f;

        float slideStart = weapon.reloadMagInAt - magazineSlideDuration;
        if (progress >= slideStart)
        {
            return magazineInsertDistance * (1f - Mathf.SmoothStep(0f, 1f, (progress - slideStart) / magazineSlideDuration));
        }

        float fall = Mathf.Clamp01((progress - weapon.reloadMagOutAt) / magazineFallDuration);
        return magazineDropDistance * fall * fall;
    }
}
