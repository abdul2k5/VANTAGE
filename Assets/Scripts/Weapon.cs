using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public int maxAmmo = 12;
    public float fireRate = 0.25f;
    public float range = 10f;
    public float damage = 25f;
    public LayerMask hitMask;

    int currentAmmo;
    float fireTimer;

    public int CurrentAmmo { get { return currentAmmo; } }
    public int MaxAmmo { get { return maxAmmo; } }

    void Awake()
    {
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        fireTimer -= Time.deltaTime;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed && fireTimer <= 0f && currentAmmo > 0)
        {
            Fire();
            fireTimer = fireRate;
        }

        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame)
        {
            Reload();
        }
    }

    public void Fire()
    {
        currentAmmo--;
        Vector2 origin = transform.position;
        Vector2 dir = transform.right;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, range, hitMask);
        if (hit.collider != null)
        {
            var sentinel = hit.collider.GetComponent<Sentinel>();
            if (sentinel != null)
            {
                sentinel.TakeDamage(damage);
            }
        }
    }

    public void Reload()
    {
        currentAmmo = maxAmmo;
    }
}
