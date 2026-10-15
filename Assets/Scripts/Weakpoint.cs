using UnityEngine;

/// <summary>Marks a collider as a headshot zone. Weapon.cs checks for this on the hit collider directly.</summary>
public class Weakpoint : MonoBehaviour
{
    public float damageMultiplier = 2.5f;
}
