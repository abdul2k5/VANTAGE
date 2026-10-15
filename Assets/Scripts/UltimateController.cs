using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// "Overwatch Protocol" - charges from kills and ult orbs, not cooldown-based like the other
/// abilities. Activating reveals every Sentinel through walls and cuts incoming damage for a
/// few seconds: a wide, high-commitment version of "utility before you peek" for the push into
/// a defended site.
/// </summary>
public class UltimateController : MonoBehaviour
{
    public static UltimateController Instance;

    public string abilityName = "Overwatch Protocol";
    public KeyCode activateKeyDisplay = KeyCode.X; // display only; actual check uses the new Input System below

    public float maxCharge = 100f;
    public float chargePerKill = 25f;
    public float ultimateDuration = 6f;
    public float damageReductionMultiplier = 0.5f;

    float currentCharge;
    bool active;

    public float CurrentCharge01 { get { return maxCharge <= 0f ? 0f : Mathf.Clamp01(currentCharge / maxCharge); } }
    public bool IsReady { get { return currentCharge >= maxCharge; } }
    public bool IsActive { get { return active; } }

    PlayerHealth health;

    void Awake()
    {
        Instance = this;
        health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (MainMenuController.IsOpen || Cursor.lockState != CursorLockMode.Locked) return;

        var kb = Keyboard.current;
        if (kb != null && kb.xKey.wasPressedThisFrame)
        {
            TryActivate();
        }
    }

    public void AddCharge(float amount)
    {
        if (active) return;
        currentCharge = Mathf.Clamp(currentCharge + amount, 0f, maxCharge);
    }

    public bool TryActivate()
    {
        if (!IsReady || active) return false;

        currentCharge = 0f;
        StartCoroutine(RunUltimate());
        return true;
    }

    IEnumerator RunUltimate()
    {
        active = true;

        var sentinels = Object.FindObjectsByType<Sentinel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var s in sentinels)
        {
            s.Reveal(ultimateDuration);
        }

        if (health != null) health.DamageMultiplier = damageReductionMultiplier;
        // The effect itself (reveal, damage reduction) has no guaranteed on-screen presence -
        // this is what actually tells the player it fired and is still running.
        if (UltimateActiveOverlay.Instance != null) UltimateActiveOverlay.Instance.Play(ultimateDuration);

        yield return new WaitForSeconds(ultimateDuration);

        if (health != null) health.DamageMultiplier = 1f;
        active = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
