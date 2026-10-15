using UnityEngine;

[RequireComponent(typeof(SightlineVisualizer))]
public class Sentinel : MonoBehaviour
{
    public Transform player;
    public LayerMask blockingMask;
    public float range = 6f;
    public float halfAngleDegrees = 28f;
    public float damagePerHit = 20f;
    public float fireInterval = 0.8f;
    public float maxHealth = 50f;

    float fireTimer;
    float currentHealth;
    float blindRemaining;
    SightlineVisualizer visualizer;

    public bool IsBlinded { get { return blindRemaining > 0f; } }

void Awake()
    {
        currentHealth = maxHealth;
        visualizer = GetComponent<SightlineVisualizer>();
    }

    public void Blind(float seconds)
    {
        if (seconds > blindRemaining) blindRemaining = seconds;
        if (visualizer != null) visualizer.SetBlinded(true);
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

void Update()
    {
        if (blindRemaining > 0f)
        {
            blindRemaining -= Time.deltaTime;
            if (blindRemaining <= 0f)
            {
                blindRemaining = 0f;
                if (visualizer != null) visualizer.SetBlinded(false);
            }
        }

        if (player == null) return;

        if (CanSeePlayer())
        {
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
        }
    }

public bool CanSeePlayer()
    {
        if (blindRemaining > 0f) return false;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;
        if (distance > range) return false;

        Vector2 forward = transform.up;
        float angle = Vector2.Angle(forward, toPlayer);
        if (angle > halfAngleDegrees) return false;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer.normalized, distance, blockingMask);
        if (hit.collider != null) return false;

        return true;
    }

    void Fire()
    {
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
