using System.Collections;
using UnityEngine;

public class DashAbility : Ability
{
    public float dashSpeed = 18f;
    public float dashSeconds = 0.18f;
    public bool immuneWhileDashing = true;

    PlayerController controller;
    Rigidbody2D rb;
    PlayerHealth health;

    void Awake()
    {
        abilityName = "Dash";
        keyLabel = "SHIFT";
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<PlayerHealth>();
    }

    protected override bool OnUse()
    {
        Vector2 dir = controller != null ? controller.MoveInput : Vector2.zero;
        // With no movement input, dash the way the player is facing.
        if (dir.sqrMagnitude < 0.01f) dir = transform.right;
        dir.Normalize();

        StartCoroutine(DoDash(dir));
        return true;
    }

    IEnumerator DoDash(Vector2 dir)
    {
        if (controller != null) controller.SuppressMovement = true;
        if (immuneWhileDashing && health != null) health.Invulnerable = true;

        float t = 0f;
        while (t < dashSeconds)
        {
            t += Time.deltaTime;
            if (rb != null) rb.linearVelocity = dir * dashSpeed;
            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (controller != null) controller.SuppressMovement = false;
        if (immuneWhileDashing && health != null) health.Invulnerable = false;
    }
}
