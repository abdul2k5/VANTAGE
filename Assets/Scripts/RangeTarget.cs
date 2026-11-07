using System;
using UnityEngine;

/// <summary>Marks a Room 1 practice-target hit zone. Weapon.cs calls RegisterHit() on a successful hit; RangeRoomGate listens for all three to unlock the Range exit door.</summary>
public class RangeTarget : MonoBehaviour
{
    public event Action OnHit;

    bool hit;

    public void RegisterHit()
    {
        if (hit) return;
        hit = true;
        if (OnHit != null) OnHit();
    }

    /// <summary>Room Select: the target counts again, and its bullet holes (kept for the whole visit) are cleared.</summary>
    public void ResetHit()
    {
        hit = false;
        var parts = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].name == "RangeBulletHole") Destroy(parts[i].gameObject);
        }
    }
}
