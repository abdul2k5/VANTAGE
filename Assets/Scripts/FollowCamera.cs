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

    float pitch;
    PlayerController player;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        if (MainMenuController.IsOpen || Cursor.lockState != CursorLockMode.Locked) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        float sensitivity = player != null ? player.mouseSensitivity : mouseSensitivity;
        float mouseY = mouse.delta.ReadValue().y * sensitivity * 0.1f;
        pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    /// <summary>Called by RoomManager on every room transition so a carried-over head tilt doesn't leak into the next room.</summary>
    public void SnapToTarget()
    {
        pitch = 0f;
        transform.localRotation = Quaternion.identity;
    }
}
