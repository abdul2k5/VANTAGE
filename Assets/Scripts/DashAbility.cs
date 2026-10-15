using System.Collections;
using UnityEngine;

public class DashAbility : Ability
{
    public float dashSpeed = 18f;
    public float dashSeconds = 0.18f;
    public bool immuneWhileDashing = true;

    PlayerController controller;
    PlayerHealth health;

    void Awake()
    {
        abilityName = "Dash";
        keyLabel = "SHIFT";
        controller = GetComponent<PlayerController>();
        health = GetComponent<PlayerHealth>();
    }

    protected override bool OnUse()
    {
        Vector2 input = controller != null ? controller.MoveInput : Vector2.zero;
        // With no movement input, dash the way the player is facing.
        Vector3 dir = input.sqrMagnitude < 0.01f
            ? transform.forward
            : (transform.right * input.x + transform.forward * input.y).normalized;

        StartCoroutine(DoDash(dir));
        return true;
    }

    IEnumerator DoDash(Vector3 dir)
    {
        if (controller != null) controller.SuppressMovement = true;
        if (immuneWhileDashing && health != null) health.Invulnerable = true;

        float t = 0f;
        while (t < dashSeconds)
        {
            t += Time.deltaTime;
            if (controller != null) controller.DashMove(dir * dashSpeed);
            yield return null;
        }

        if (controller != null) controller.SuppressMovement = false;
        if (immuneWhileDashing && health != null) health.Invulnerable = false;
    }
}
