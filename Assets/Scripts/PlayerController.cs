using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 5f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float maxLookAngle = 85f;

    private CharacterController characterController;

    public bool IsMovementLocked { get; set; }
    public bool IsLookLocked { get; set; }
    private float verticalVelocity;
    private float cameraPitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        LockCursor();
    }

private void Update()
{
    if (!IsMovementLocked)
        HandleMovement();

    if (!IsLookLocked)
        HandleLook();

    HandleCursor();
}

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            input = new Vector2(
                (Keyboard.current.dKey.isPressed ? 1 : 0) -
                (Keyboard.current.aKey.isPressed ? 1 : 0),

                (Keyboard.current.wKey.isPressed ? 1 : 0) -
                (Keyboard.current.sKey.isPressed ? 1 : 0)
            );
        }

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 movement =
            transform.right * input.x +
            transform.forward * input.y;

        bool isSprinting =
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed &&
            input.y > 0f;

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = movement * speed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleLook()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -maxLookAngle,
            maxLookAngle
        );

        cameraTransform.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleCursor()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}