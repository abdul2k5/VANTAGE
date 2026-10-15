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

    [Header("Headshots")]
    public float headshotMultiplier = 2.5f;
    public AudioClip headshotSound;

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
        float speed = cc != null ? new Vector2(cc.velocity.x, cc.velocity.z).magnitude : 0f;
        float speedFrac = fullSpeedForMaxSpread > 0f ? Mathf.Clamp01(speed / fullSpeedForMaxSpread) : 0f;
        currentSpreadDegrees = Mathf.Lerp(standingSpreadDegrees, movingSpreadDegrees, speedFrac);
        CurrentSpreadFraction01 = speedFrac;

        if (CrosshairController.Instance != null) CrosshairController.Instance.SetSpreadFraction(speedFrac);
    }

    public void Fire()
    {
        currentAmmo--;
        Vector3 origin = cam != null ? cam.transform.position : transform.position;
        Vector3 forward = cam != null ? cam.transform.forward : transform.forward;
        Vector3 dir = ApplySpread(forward, currentSpreadDegrees);

        RaycastHit hit;
        bool didHit = Physics.Raycast(origin, dir, out hit, range, hitMask);

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
                sentinel.TakeDamage(dealt);
                if (aliveBefore && !sentinel.gameObject.activeSelf && UltimateController.Instance != null)
                {
                    UltimateController.Instance.AddCharge(UltimateController.Instance.chargePerKill);
                }

                if (HitFeedbackUI.Instance != null) HitFeedbackUI.Instance.ShowHitMarker(headshot);
                if (headshot && headshotSound != null && audioSource != null) audioSource.PlayOneShot(headshotSound);
            }
        }

        // Raycast stays camera-centered for accuracy; the visible tracer starts at the gun's muzzle.
        Vector3 visualOrigin = muzzle != null ? muzzle.position : origin;
        if (feedback != null) feedback.PlayShot(visualOrigin, end);
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
    }

    /// <summary>Instant reload, kept for callers (e.g. RoomManager on respawn) that want ammo topped up with no delay.</summary>
    public void Reload()
    {
        isReloading = false;
        currentAmmo = maxAmmo;
    }
}
