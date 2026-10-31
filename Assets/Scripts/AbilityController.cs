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
        if (flashUnlocked && flash != null && kb.eKey.wasPressedThisFrame) flash.TryUse();
        if (dashUnlocked && dash != null && kb.leftShiftKey.wasPressedThisFrame) dash.TryUse();
        if (reconDartUnlocked && reconDart != null && kb.fKey.wasPressedThisFrame) reconDart.TryUse();
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
