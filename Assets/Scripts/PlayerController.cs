using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float acceleration = 40f;
    public float mouseSensitivity = 2f;
    public float gravity = -20f;

    [Header("Walk (hold Left Alt) - slower but silent; running footsteps are what Sentinels hear")]
    public float walkSpeedMultiplier = 0.55f;

    [Header("Crouch")]
    public Transform cameraPivot;
    public float crouchHeight = 1.0f;
    public float standEyeHeight = 1.6f;
    public float crouchEyeHeight = 0.9f;
    public float crouchSpeedMultiplier = 0.5f;
    public float crouchTransitionSeconds = 0.15f;
    [Tooltip("Read by Sentinel.CanSeePlayer as the player's represented eye height. Standing (1.5) sits above 1.2m cover - a Sentinel can still see over it - while crouched (0.4) drops below it, so crouch is the thing that actually breaks line of sight behind cover, not just standing there.")]
    public float standEyeOffsetForSight = 1.5f;
    public float crouchEyeOffsetForSight = 0.4f;

    [Header("Jump")]
    [Tooltip("Apex height in meters. Kept low (default 0.8) so jumping can't get the player on top of or over 1.2m cover crates - it's a movement tool, not a combat one.")]
    public float jumpHeight = 0.8f;
    public float landingPenaltySeconds = 0.2f;
    public float landingSpeedMultiplier = 0.5f;
    public AudioClip jumpSound;
    public AudioClip landSound;

    CharacterController cc;
    Vector2 moveInput;
    Vector3 currentVelocity;
    float verticalVelocity;
    AudioSource audioSource;

    float standHeight;
    bool crouchHeld;
    bool walkHeld;
    /// <summary>0 = standing, 1 = fully crouched - smoothed over crouchTransitionSeconds.</summary>
    float crouchLerp;
    bool wasGrounded = true;
    float landingPenaltyRemaining;

    public Vector2 MoveInput { get { return moveInput; } }
    /// <summary>Set while an ability (e.g. Dash) is driving the body itself.</summary>
    public bool SuppressMovement { get; set; }
    public bool IsCrouching { get { return crouchHeld; } }
    public bool IsWalking { get { return walkHeld; } }
    /// <summary>Grounded horizontal speed, for footsteps.</summary>
    public float HorizontalSpeed { get { return cc != null ? new Vector2(cc.velocity.x, cc.velocity.z).magnitude : 0f; } }
    public bool IsAirborne { get { return cc != null && !cc.isGrounded; } }
    /// <summary>1 = fully standing, 0 = fully crouched - see standEyeOffsetForSight tooltip.</summary>
    public float EyeHeightOffset { get { return Mathf.Lerp(standEyeOffsetForSight, crouchEyeOffsetForSight, crouchLerp); } }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        mouseSensitivity = PlayerPrefs.GetFloat("VantageMouseSensitivity", mouseSensitivity);
        standHeight = cc.height;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (AudioManager.Instance != null) audioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
    }

    /// <summary>Live-applied from the settings menu's sensitivity slider.</summary>
    public void SetSensitivity(float value)
    {
        mouseSensitivity = Mathf.Clamp(value, 0.1f, 10f);
        PlayerPrefs.SetFloat("VantageMouseSensitivity", mouseSensitivity);
    }

    void Update()
    {
        if (landingPenaltyRemaining > 0f)
        {
            landingPenaltyRemaining -= Time.deltaTime;
            if (landingPenaltyRemaining < 0f) landingPenaltyRemaining = 0f;
        }

        if (MainMenuController.IsOpen || Cursor.lockState != CursorLockMode.Locked) return;

        var kb = Keyboard.current;
        float x = 0f, y = 0f;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
        }
        moveInput = new Vector2(x, y);
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        ApplyYaw();
        walkHeld = kb != null && kb.leftAltKey.isPressed;
        UpdateCrouch(kb);
        UpdateJump(kb);
        ApplyMovement();
    }

    void ApplyYaw()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float mouseX = mouse.delta.ReadValue().x * mouseSensitivity * 0.1f;
        transform.Rotate(Vector3.up, mouseX, Space.World);
    }

    void UpdateCrouch(Keyboard kb)
    {
        bool wantsCrouch = kb != null && (kb.leftCtrlKey.isPressed || kb.cKey.isPressed);

        // Don't let the player stand up into something overhead - stay crouched until it's clear.
        if (crouchHeld && !wantsCrouch && HeadBlocked()) wantsCrouch = true;

        crouchHeld = wantsCrouch;

        float target = crouchHeld ? 1f : 0f;
        float rate = crouchTransitionSeconds > 0f ? Time.deltaTime / crouchTransitionSeconds : 1f;
        crouchLerp = Mathf.MoveTowards(crouchLerp, target, rate);

        float height = Mathf.Lerp(standHeight, crouchHeight, crouchLerp);
        cc.height = height;
        cc.center = new Vector3(0f, height * 0.5f, 0f);

        if (cameraPivot != null)
        {
            Vector3 p = cameraPivot.localPosition;
            p.y = Mathf.Lerp(standEyeHeight, crouchEyeHeight, crouchLerp);
            cameraPivot.localPosition = p;
        }
    }

    bool HeadBlocked()
    {
        float checkDistance = standHeight - cc.height + 0.1f;
        if (checkDistance <= 0f) return false;
        Vector3 origin = transform.position + Vector3.up * cc.height;
        return Physics.Raycast(origin, Vector3.up, checkDistance);
    }

    void UpdateJump(Keyboard kb)
    {
        if (kb == null || !kb.spaceKey.wasPressedThisFrame) return;
        if (!cc.isGrounded) return;
        if (crouchHeld) return;
        if (BreachChargeSite.AnyPlantInProgress) return;

        verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
        if (jumpSound != null && audioSource != null) audioSource.PlayOneShot(jumpSound);
    }

    void ApplyMovement()
    {
        float speedMultiplier = 1f;
        if (crouchHeld) speedMultiplier *= crouchSpeedMultiplier;
        else if (walkHeld) speedMultiplier *= walkSpeedMultiplier;
        if (landingPenaltyRemaining > 0f) speedMultiplier *= landingSpeedMultiplier;

        Vector3 desired = (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed * speedMultiplier;

        if (SuppressMovement)
        {
            // An ability (Dash) is driving horizontal motion directly via Move(); still apply gravity.
            verticalVelocity += gravity * Time.deltaTime;
            cc.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
            return;
        }

        currentVelocity = Vector3.MoveTowards(currentVelocity, desired, acceleration * Time.deltaTime);

        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = (currentVelocity + new Vector3(0f, verticalVelocity, 0f)) * Time.deltaTime;
        cc.Move(motion);

        // Landed this frame (was airborne, now grounded) - short speed penalty so bunny-hopping can't
        // just chain full-speed jumps back to back.
        if (!wasGrounded && cc.isGrounded)
        {
            landingPenaltyRemaining = landingPenaltySeconds;
            if (landSound != null && audioSource != null) audioSource.PlayOneShot(landSound);
        }
        wasGrounded = cc.isGrounded;
    }

    /// <summary>Moves the controller directly, used by DashAbility to bypass normal acceleration.</summary>
    public void DashMove(Vector3 worldVelocity)
    {
        cc.Move(worldVelocity * Time.deltaTime);
    }
}
