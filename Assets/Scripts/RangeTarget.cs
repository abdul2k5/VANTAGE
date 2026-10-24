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
}
