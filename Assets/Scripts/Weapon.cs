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

    /// <summary>Where the visible tracer/muzzle flash originates. The hit-detection raycast still fires from the camera center.</summary>
    public Transform muzzle;

    int currentAmmo;
    float fireTimer;
    bool isReloading;
    float reloadTimer;
    WeaponFeedback feedback;
    Camera cam;

    public int CurrentAmmo { get { return currentAmmo; } }
    public int MaxAmmo { get { return maxAmmo; } }
    public bool IsReloading { get { return isReloading; } }
    public float ReloadProgress01 { get { return isReloading ? 1f - (reloadTimer / reloadSeconds) : 0f; } }

    void Awake()
    {
        currentAmmo = maxAmmo;
        feedback = GetComponent<WeaponFeedback>();
        if (feedback == null) feedback = gameObject.AddComponent<WeaponFeedback>();
        cam = Camera.main;
    }

    void Update()
    {
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

        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame && !isReloading && currentAmmo < maxAmmo)
        {
            StartReload();
        }
    }

    public void Fire()
    {
        currentAmmo--;
        Vector3 origin = cam != null ? cam.transform.position : transform.position;
        Vector3 dir = cam != null ? cam.transform.forward : transform.forward;
        RaycastHit hit;
        bool didHit = Physics.Raycast(origin, dir, out hit, range, hitMask);

        Vector3 end = didHit ? hit.point : origin + dir * range;

        if (didHit)
        {
            var sentinel = hit.collider.GetComponent<Sentinel>();
            if (sentinel != null)
            {
                sentinel.TakeDamage(damage);
            }
        }

        // Raycast stays camera-centered for accuracy; the visible tracer starts at the gun's muzzle.
        Vector3 visualOrigin = muzzle != null ? muzzle.position : origin;
        if (feedback != null) feedback.PlayShot(visualOrigin, end);
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
