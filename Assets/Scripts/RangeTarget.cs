using System;
using UnityEngine;

/// <summary>Marks a Room 1 practice-target hit zone. Weapon.cs calls RegisterHit() on a successful hit; RangeRoomGate listens for all three to unlock the Range exit door.
/// A hit target turns from red to green and stays green, with a bell, so the player can see which ones are done.</summary>
public class RangeTarget : MonoBehaviour
{
    public event Action OnHit;

    [Header("Hit feedback")]
    [Tooltip("The plate behind the bullseye. Tinted as well, so the change reads from across the room - the bullseye alone is small.")]
    public Renderer face;
    public Color hitColor = new Color(0.12f, 0.78f, 0.25f, 1f);
    public Color faceHitColor = new Color(0.55f, 0.9f, 0.58f, 1f);
    public AudioClip hitSound;
    [Range(0f, 1f)] public float hitSoundVolume = 0.7f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    bool hit;
    Renderer bullseye;
    MaterialPropertyBlock block;

    void Awake()
    {
        bullseye = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
    }

    public void RegisterHit()
    {
        if (hit) return;
        hit = true;
        // Per-renderer overrides, so the three targets keep sharing one material.
        Tint(bullseye, hitColor);
        Tint(face, faceHitColor);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(hitSound, hitSoundVolume);
        if (OnHit != null) OnHit();
    }

    /// <summary>Room Select: the target counts again, goes back to red, and its bullet holes (kept for the whole visit) are cleared.</summary>
    public void ResetHit()
    {
        hit = false;
        if (bullseye != null) bullseye.SetPropertyBlock(null);
        if (face != null) face.SetPropertyBlock(null);
        var parts = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].name == "RangeBulletHole") Destroy(parts[i].gameObject);
        }
    }

    void Tint(Renderer r, Color color)
    {
        if (r == null) return;
        r.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        r.SetPropertyBlock(block);
    }
}
