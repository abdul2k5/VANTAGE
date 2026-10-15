using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float acceleration = 40f;
    public float mouseSensitivity = 2f;
    public float gravity = -20f;

    CharacterController cc;
    Vector2 moveInput;
    Vector3 currentVelocity;
    float verticalVelocity;

    public Vector2 MoveInput { get { return moveInput; } }
    /// <summary>Set while an ability (e.g. Dash) is driving the body itself.</summary>
    public bool SuppressMovement { get; set; }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
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
        ApplyMovement();
    }

    void ApplyYaw()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float mouseX = mouse.delta.ReadValue().x * mouseSensitivity * 0.1f;
        transform.Rotate(Vector3.up, mouseX, Space.World);
    }

    void ApplyMovement()
    {
        Vector3 desired = (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;

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
    }

    /// <summary>Moves the controller directly, used by DashAbility to bypass normal acceleration.</summary>
    public void DashMove(Vector3 worldVelocity)
    {
        cc.Move(worldVelocity * Time.deltaTime);
    }
}
