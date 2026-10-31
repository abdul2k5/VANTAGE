using UnityEngine;

public abstract class Ability : MonoBehaviour
{
    public string abilityName = "Ability";
    public string keyLabel = "Q";
    /// <summary>Short lockout between uses (anti double-tap / no stacking), not the main limit - charges are.</summary>
    public float cooldown = 6f;
    /// <summary>Uses per room, refilled on every room entry (AbilityController.ResetAllCooldowns). A fixed
    /// budget instead of an endless cooldown loop makes *when* you spend utility a real decision.</summary>
    public int maxCharges = 1;
    public Sprite icon;
    public AudioClip activateSound;

    float cooldownRemaining;
    int charges = -1; // -1 = not yet initialised, treated as full

    public float CooldownRemaining { get { return cooldownRemaining; } }
    public float Cooldown { get { return cooldown; } }
    public int Charges { get { return charges < 0 ? maxCharges : charges; } }
    public int MaxCharges { get { return maxCharges; } }
    public bool IsReady { get { return cooldownRemaining <= 0f && Charges > 0; } }

    protected virtual void Update()
    {
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            if (cooldownRemaining < 0f) cooldownRemaining = 0f;
        }
    }

    /// <summary>Fired after a successful use - room gates (RoomExitGate) listen for this to know the room's utility was actually used.</summary>
    public event System.Action OnUsed;

    public bool TryUse()
    {
        if (!IsReady) return false;
        if (!OnUse()) return false;

        cooldownRemaining = cooldown;
        charges = Charges - 1;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(activateSound);
        if (OnUsed != null) OnUsed();
        return true;
    }

    public void ResetCooldown()
    {
        cooldownRemaining = 0f;
    }

    public void RefillCharges()
    {
        charges = maxCharges;
    }

    /// <summary>Perform the ability. Return false to abort without starting the cooldown.</summary>
    protected abstract bool OnUse();
}
