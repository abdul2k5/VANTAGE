using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;

    float currentHealth;
    Vector3 respawnPosition;

    public float CurrentHealth { get { return currentHealth; } }
    /// <summary>True during a Dash, so crossing an exposed lane is survivable by design.</summary>
    public bool Invulnerable { get; set; }
    /// <summary>Scales incoming damage; 1 = normal, &lt;1 = reduced (set by the ultimate).</summary>
    public float DamageMultiplier { get; set; } = 1f;
    public float MaxHealth { get { return maxHealth; } }
    public AudioClip damageSound;

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
        TakeDamage(amount, transform.position);
    }

    /// <summary>sourceWorldPosition drives the on-screen damage-direction indicator.</summary>
    public void TakeDamage(float amount, Vector3 sourceWorldPosition)
    {
        if (Invulnerable) return;

        currentHealth -= amount * DamageMultiplier;
        if (HitFeedbackUI.Instance != null) HitFeedbackUI.Instance.ShowDamageDirection(sourceWorldPosition);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(damageSound);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        // Dying during an active Breach Charge round is a loss, not a free respawn.
        if (RoundController.Instance != null && RoundController.Instance.IsRoundActive)
        {
            RoundController.Instance.OnPlayerDied();
            currentHealth = maxHealth;
            return;
        }

        currentHealth = maxHealth;

        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = respawnPosition;
        if (cc != null) cc.enabled = true;
    }
}
