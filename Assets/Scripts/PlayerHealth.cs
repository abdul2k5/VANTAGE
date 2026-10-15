using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;

    float currentHealth;
    Vector3 respawnPosition;

    public float CurrentHealth { get { return currentHealth; } }
    /// <summary>True during a Dash, so crossing an exposed lane is survivable by design.</summary>
    public bool Invulnerable { get; set; }
    public float MaxHealth { get { return maxHealth; } }

    void Awake()
    {
        currentHealth = maxHealth;
        respawnPosition = transform.position;
    }

    public void SetRespawnPoint(Vector3 point)
    {
        respawnPosition = point;
    }

public void TakeDamage(float amount)
    {
        if (Invulnerable) return;

        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        currentHealth = maxHealth;

        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = respawnPosition;
        if (cc != null) cc.enabled = true;
    }
}
