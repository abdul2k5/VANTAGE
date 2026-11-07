using UnityEngine;

/// <summary>Poses the first-person gun for aiming and reloading, entirely from Weapon.AimAmount01 and Weapon.ReloadProgress01. Because the pose is worked out from those each frame, it pauses when the reload pauses, and an instant Weapon.Reload() puts the gun straight back.</summary>
public class GunViewmodelAnimator : MonoBehaviour
{
    public Weapon weapon;
    public Transform magazine;

    [Header("Aiming")]
    [Tooltip("Gun position when fully aimed. The scope is a solid model, so it sits just under the crosshair rather than on it, which would hide the target.")]
    public Vector3 aimPosition = new Vector3(0f, -0.2f, 0.4f);
    public float aimFieldOfView = 40f;

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

    void Awake()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
        cam = GetComponentInParent<Camera>();
        if (cam != null) baseFieldOfView = cam.fieldOfView;

        if (magazine != null)
        {
            magazineBasePosition = magazine.localPosition;
            // The magazine's parent is the model, scaled to 0.01, so one metre down the gun is 100 of its units.
            magazineDownPerMetre = magazine.parent.InverseTransformVector(-transform.up);
        }
    }

    void LateUpdate()
    {
        if (weapon == null) return;

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

        transform.localPosition = Vector3.Lerp(basePosition, aimPosition, aim) + reloadPositionOffset * weight + Vector3.up * jolt;
        transform.localRotation = baseRotation * Quaternion.Euler(reloadTiltDegrees * weight);

        if (magazine != null) magazine.localPosition = magazineBasePosition + magazineDownPerMetre * MagazineDrop(progress);
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
