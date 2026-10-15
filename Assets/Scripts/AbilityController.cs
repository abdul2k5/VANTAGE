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
    // Not yet gated per-room - Recon Dart is new in Phase 3 and the room sequence that assigns
    // it a proper unlock point is a Phase 5 job. Available from the start until then.
    public bool reconDartUnlocked = true;

    void Awake()
    {
        if (smokeWall == null) smokeWall = GetComponent<SmokeWallAbility>();
        if (flash == null) flash = GetComponent<FlashAbility>();
        if (dash == null) dash = GetComponent<DashAbility>();
        if (reconDart == null) reconDart = GetComponent<ReconDartAbility>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (smokeWallUnlocked && smokeWall != null && kb.qKey.wasPressedThisFrame) smokeWall.TryUse();
        if (flashUnlocked && flash != null && kb.eKey.wasPressedThisFrame) flash.TryUse();
        if (dashUnlocked && dash != null && kb.leftShiftKey.wasPressedThisFrame) dash.TryUse();
        if (reconDartUnlocked && reconDart != null && kb.fKey.wasPressedThisFrame) reconDart.TryUse();
    }

    public void UnlockForRoom(int roomIndex)
    {
        // Room 3 teaches Smoke Wall, room 4 Flash, room 5 Dash; room 6 has everything.
        smokeWallUnlocked = roomIndex >= 2;
        flashUnlocked = roomIndex >= 3;
        dashUnlocked = roomIndex >= 4;
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

    public void ResetAllCooldowns()
    {
        if (smokeWall != null) smokeWall.ResetCooldown();
        if (flash != null) flash.ResetCooldown();
        if (dash != null) dash.ResetCooldown();
        if (reconDart != null) reconDart.ResetCooldown();
    }
}
