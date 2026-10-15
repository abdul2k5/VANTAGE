using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Mouse-look pitch for the camera, which sits as a child of the player (which handles yaw).</summary>
public class FollowCamera : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    float pitch;

    void Update()
    {
        if (MainMenuController.IsOpen) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        float mouseY = mouse.delta.ReadValue().y * mouseSensitivity * 0.1f;
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
