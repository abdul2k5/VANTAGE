using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Mouse-look pitch for the camera, which sits as a child of the player (which handles yaw).</summary>
public class FollowCamera : MonoBehaviour
{
    /// <summary>Fallback only - pitch normally uses the PlayerController's sensitivity, so the settings slider
    /// drives both axes. (It used to use this field alone, fixed at 2, so the slider only changed yaw.)</summary>
    public float mouseSensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Recoil recovery - Weapon.Fire kicks via AddRecoil, this settles it back")]
    public float recoilRecoveryDegreesPerSecond = 10f;
    [Tooltip("How long after a shot before recovery starts, so a held spray keeps climbing instead of fighting itself.")]
    public float recoilRecoveryDelay = 0.3f;

    float pitch;
    float recoilPitch;
    float recoilYaw;
    float recoveryDelayRemaining;
    PlayerController player;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    /// <summary>Spray-pattern kick: shifts the view (and so the crosshair and the next shot) up/sideways. Kept
    /// separate from the player's own pitch so it can recover back without undoing their aim.</summary>
    public void AddRecoil(float pitchDegrees, float yawDegrees)
    {
        recoilPitch += pitchDegrees;
        recoilYaw += yawDegrees;
        recoveryDelayRemaining = recoilRecoveryDelay;
        ApplyRotation();
    }

    void ApplyRotation()
    {
        transform.localRotation = Quaternion.Euler(Mathf.Clamp(pitch - recoilPitch, minPitch, maxPitch), recoilYaw, 0f);
    }

    void RecoverRecoil()
    {
        if (recoveryDelayRemaining > 0f)
        {
            recoveryDelayRemaining -= Time.deltaTime;
            return;
        }
        float step = recoilRecoveryDegreesPerSecond * Time.deltaTime;
        recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, step);
        recoilYaw = Mathf.MoveTowards(recoilYaw, 0f, step);
        ApplyRotation();
    }

    void Update()
    {
        if (MainMenuController.IsOpen || Cursor.lockState != CursorLockMode.Locked) return;

        RecoverRecoil();

        var mouse = Mouse.current;
        if (mouse == null) return;

        float sensitivity = player != null ? player.mouseSensitivity : mouseSensitivity;
        float mouseY = mouse.delta.ReadValue().y * sensitivity * 0.1f;
        pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);
        ApplyRotation();
    }

    /// <summary>Called by RoomManager on every room transition so a carried-over head tilt doesn't leak into the next room.</summary>
    public void SnapToTarget()
    {
        pitch = 0f;
        recoilPitch = 0f;
        recoilYaw = 0f;
        transform.localRotation = Quaternion.identity;
    }
}
