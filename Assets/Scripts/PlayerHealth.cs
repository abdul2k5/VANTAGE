using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    [Tooltip("Armor: takes damage before health does. Doesn't recover mid-room - RoomManager refills it on every room entry, the same per-room budget as ability charges.")]
    public float maxShield = 50f;

    float currentHealth;
    float currentShield;
    Vector3 respawnPosition;

    public float CurrentHealth { get { return currentHealth; } }
    /// <summary>True during a Dash, so crossing an exposed lane is survivable by design.</summary>
    public bool Invulnerable { get; set; }
    /// <summary>Scales incoming damage; 1 = normal, &lt;1 = reduced (set by the ultimate).</summary>
    public float DamageMultiplier { get; set; } = 1f;
    /// <summary>True from Game Over until a retry restores the player. Drones keep firing during the retry fade, and a hit then would reopen Game Over.</summary>
    public bool AttemptOver { get; set; }
    public float MaxHealth { get { return maxHealth; } }
    public float CurrentShield { get { return currentShield; } }
    public float MaxShield { get { return maxShield; } }
    public AudioClip damageSound;

    void Awake()
    {
        currentHealth = maxHealth;
        currentShield = maxShield;
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
        currentShield = maxShield;
        Invulnerable = false;
        DamageMultiplier = 1f;
        AttemptOver = false;
    }


    /// <summary>Called by RoomManager on every room entry.</summary>
    public void RefillShield()
    {
        currentShield = maxShield;
    }

    /// <summary>Health only - armor already refills per room. Used by MedkitPickup.</summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

public void TakeDamage(float amount)
    {
        TakeDamage(amount, transform.position);
    }

    /// <summary>sourceWorldPosition drives the on-screen damage-direction indicator.</summary>
    public void TakeDamage(float amount, Vector3 sourceWorldPosition)
    {
        if (Invulnerable || AttemptOver) return;

        float dealt = amount * DamageMultiplier;
        float absorbed = Mathf.Min(currentShield, dealt);
        currentShield -= absorbed;
        currentHealth -= dealt - absorbed;
        if (RunStats.Instance != null) RunStats.Instance.RecordDamage(dealt, absorbed);
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
