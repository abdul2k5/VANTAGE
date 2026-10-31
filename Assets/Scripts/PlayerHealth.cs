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

/// <summary>Used by CheckpointManager on a checkpoint retry - full HP, no lingering invulnerability/damage-reduction from whatever was active at the moment of death.</summary>
    public void FullRestore()
    {
        currentHealth = maxHealth;
        Invulnerable = false;
        DamageMultiplier = 1f;
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
        if (RunStats.Instance != null) RunStats.Instance.RecordDamage(amount * DamageMultiplier);
        if (HitFeedbackUI.Instance != null) HitFeedbackUI.Instance.ShowDamageDirection(sourceWorldPosition);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(damageSound);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

void Die()
    {
        // Every death now ends the attempt - lecturer feedback was that dying anywhere but
        // The Round was a free respawn with no stakes. RoundController still needs to know so
        // it can stop its waves/timer, but it no longer owns the Game Over screen itself.
        if (RoundController.Instance != null && RoundController.Instance.IsRoundActive)
        {
            RoundController.Instance.OnPlayerDied();
        }

        if (GameOverController.Instance != null) GameOverController.Instance.ShowGameOver();
    }
}
