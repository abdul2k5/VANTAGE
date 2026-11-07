using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityController : MonoBehaviour
{
    public SmokeWallAbility smokeWall;
    public FlashAbility flash;
    public DashAbility dash;
    public ReconDartAbility reconDart;

    /// <summary>Abilities are revealed room by room so the tutorial never shows an unexplained key.</summary>
    public bool smokeWallUnlocked;
    public bool flashUnlocked;
    public bool dashUnlocked;
    public bool reconDartUnlocked;

    void Awake()
    {
        if (smokeWall == null) smokeWall = GetComponent<SmokeWallAbility>();
        if (flash == null) flash = GetComponent<FlashAbility>();
        if (dash == null) dash = GetComponent<DashAbility>();
        if (reconDart == null) reconDart = GetComponent<ReconDartAbility>();
    }

    void Update()
    {
        if (MainMenuController.IsOpen || AbilityUnlockCardUI.IsShowingCard) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (smokeWallUnlocked && smokeWall != null && kb.qKey.wasPressedThisFrame) smokeWall.TryUse();
        if (flashUnlocked && flash != null)
        {
            // Hold to ready it in hand (preview line), release to throw. One held ability at a time.
            if (kb.eKey.wasPressedThisFrame && (reconDart == null || !reconDart.IsAiming)) flash.BeginAim();
            if (flash.IsAiming && kb.eKey.wasReleasedThisFrame) flash.ReleaseAim();
        }
        if (dashUnlocked && dash != null && kb.leftShiftKey.wasPressedThisFrame) dash.TryUse();
        if (reconDartUnlocked && reconDart != null)
        {
            // Hold to aim (preview line), right-click to change bounces, release to fire.
            if (kb.fKey.wasPressedThisFrame && (flash == null || !flash.IsAiming)) reconDart.BeginAim();
            var mouse = Mouse.current;
            if (reconDart.IsAiming && mouse != null && mouse.rightButton.wasPressedThisFrame) reconDart.CycleBounces();
            if (reconDart.IsAiming && kb.fKey.wasReleasedThisFrame) reconDart.ReleaseAim();
        }
    }

    public void UnlockForRoom(int roomIndex)
    {
        // 8-room order: Range, Weak Point, Holding Angle, Recon, Smoke, Flash, Dash, The Round.
        reconDartUnlocked = roomIndex >= 3;
        smokeWallUnlocked = roomIndex >= 4;
        flashUnlocked = roomIndex >= 5;
        dashUnlocked = roomIndex >= 6;
    }

    public List<Ability> GetUnlockedAbilities()
    {
        var list = new List<Ability>();
        if (reconDartUnlocked && reconDart != null) list.Add(reconDart);
        if (smokeWallUnlocked && smokeWall != null) list.Add(smokeWall);
        if (flashUnlocked && flash != null) list.Add(flash);
        if (dashUnlocked && dash != null) list.Add(dash);
        return list;
    }

    /// <summary>Checkpoint retry: removes anything abilities left in the world (smoke walls, darts, flash bursts).</summary>
    public void ClearDeployed()
    {
        if (reconDart != null) reconDart.CancelAim();
        if (smokeWall != null) smokeWall.ClearActive();
        if (flash != null) flash.ClearActive();

        var darts = Object.FindObjectsByType<ReconDartProjectile>(FindObjectsSortMode.None);
        for (int i = 0; i < darts.Length; i++) Destroy(darts[i].gameObject);
        // The dart's reveal pulse is a separate object it spawns - destroy those too.
        var pulse = GameObject.Find("ReconPulse");
        while (pulse != null) { DestroyImmediate(pulse); pulse = GameObject.Find("ReconPulse"); }
    }

    /// <summary>Called on every room entry (including checkpoint retry): clears cooldowns and refills each ability's per-room charges.</summary>
    public void ResetAllCooldowns()
    {
        Ability[] all = { smokeWall, flash, dash, reconDart };
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            all[i].ResetCooldown();
            all[i].RefillCharges();
        }
    }
}
