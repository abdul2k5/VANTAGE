using UnityEngine;

public abstract class Ability : MonoBehaviour
{
    public string abilityName = "Ability";
    public string keyLabel = "Q";
    public float cooldown = 6f;
    public Sprite icon;
    public AudioClip activateSound;

    float cooldownRemaining;

    public float CooldownRemaining { get { return cooldownRemaining; } }
    public float Cooldown { get { return cooldown; } }
    public bool IsReady { get { return cooldownRemaining <= 0f; } }

    protected virtual void Update()
    {
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            if (cooldownRemaining < 0f) cooldownRemaining = 0f;
        }
    }

    public bool TryUse()
    {
        if (!IsReady) return false;
        if (!OnUse()) return false;

        cooldownRemaining = cooldown;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(activateSound);
        return true;
    }

    public void ResetCooldown()
    {
        cooldownRemaining = 0f;
    }

    /// <summary>Perform the ability. Return false to abort without starting the cooldown.</summary>
    protected abstract bool OnUse();
}
