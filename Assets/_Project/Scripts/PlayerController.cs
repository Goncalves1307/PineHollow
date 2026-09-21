using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 5f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Feel")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 35f;

    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float crouchTransitionSpeed = 6f;

    [Header("Head Bob")]
    [SerializeField] private float bobWalkFrequency = 8f;
    [SerializeField] private float bobWalkAmplitude = 0.035f;
    [SerializeField] private float bobSprintFrequency = 11f;
    [SerializeField] private float bobSprintAmplitude = 0.055f;
    [SerializeField] private float bobCrouchFrequency = 5f;
    [SerializeField] private float bobCrouchAmplitude = 0.02f;
    [SerializeField] private float bobBlendSpeed = 10f;

    [Header("Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("State")]
    [SerializeField] private PlayerStateMachine stateMachine;

    private CharacterController characterController;

    private float verticalVelocity;
    private float cameraPitch;

    private Vector3 horizontalVelocity;
    private bool isSprinting;
    private bool isCrouching;

    private float standHeight;
    private Vector3 standCenter;
    private Vector3 cameraStandLocalPosition;
    private float currentHeight;

    private float bobTimer;
    private float bobAmount;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        standHeight = characterController.height;
        standCenter = characterController.center;
        currentHeight = standHeight;

        if (cameraTransform != null)
        {
            cameraStandLocalPosition = cameraTransform.localPosition;
        }

        if (stateMachine == null)
        {
            Debug.LogError(
                "PlayerController sem PlayerStateMachine: o cursor e o Esc " +
                "ficam sem dono. Liga o componente no inspector."
            );
        }
    }

    private void Update()
    {
        bool movementLocked =
            stateMachine != null && stateMachine.IsMovementLocked;

        bool lookLocked =
            stateMachine != null && stateMachine.IsLookLocked;

        if (movementLocked)
        {
            // Sem deslize ao voltar de uma camada de UI.
            horizontalVelocity = Vector3.zero;
            isSprinting = false;

            if (stateMachine != null)
                stateMachine.SetLocomotion(PlayerState.Normal);
        }
        else
        {
            HandleCrouch();
            HandleMovementInput();
            ReportLocomotion();
        }

        // A gravidade corre sempre, mesmo com o input trancado: abrir uma
        // camada de UI a meio de uma queda não pode deixar o player suspenso.
        ApplyMotion();

        if (!lookLocked)
            HandleLook();

        ApplyCrouchHeight();
        HandleHeadBob(movementLocked);
    }

    private void HandleMovementInput()
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

        Vector3 direction =
            transform.right * input.x +
            transform.forward * input.y;

        isSprinting =
            !isCrouching &&
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed &&
            input.y > 0f;

        float speed = walkSpeed;

        if (isCrouching)
        {
            speed = crouchSpeed;
        }
        else if (isSprinting)
        {
            speed = sprintSpeed;
        }

        Vector3 targetVelocity = direction * speed;

        float rate = targetVelocity.sqrMagnitude > 0.0001f
            ? acceleration
            : deceleration;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            rate * Time.deltaTime
        );
    }

    // Gravidade e deslocação. Separado do input de propósito: corre a cada
    // frame, tenha o player controlo ou não.
    private void ApplyMotion()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity;
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

        if (cameraTransform != null)
        {
            cameraTransform.localRotation =
                Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void HandleCrouch()
    {
        bool wantsCrouch =
            Keyboard.current != null &&
            Keyboard.current.leftCtrlKey.isPressed;

        // Largar o agachar com tecto por cima não levanta o player.
        if (!wantsCrouch && isCrouching && !HasHeadroom())
            return;

        isCrouching = wantsCrouch;
    }

    // Há espaço para voltar a pôr-se de pé?
    private bool HasHeadroom()
    {
        float distance = standHeight - currentHeight;

        if (distance <= 0.001f)
            return true;

        float radius = Mathf.Max(0.01f, characterController.radius - 0.05f);

        Vector3 origin =
            transform.position +
            Vector3.up * (currentHeight - characterController.radius);

        // A esfera de partida está dentro da própria cápsula do player, por
        // isso a layer dele sai da máscara: sem isto arriscava-se um acerto em
        // si próprio e o player nunca mais se levantava depois de agachar.
        // Não é a layer mask da interacção — essa é da FASE 3.
        int mask = ~(1 << gameObject.layer);

        return !Physics.SphereCast(
            origin,
            radius,
            Vector3.up,
            out RaycastHit _,
            distance,
            mask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void ApplyCrouchHeight()
    {
        float targetHeight = isCrouching ? crouchHeight : standHeight;

        currentHeight = Mathf.MoveTowards(
            currentHeight,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        characterController.height = currentHeight;

        characterController.center = new Vector3(
            standCenter.x,
            standCenter.y - (standHeight - currentHeight) * 0.5f,
            standCenter.z
        );
    }

    private void HandleHeadBob(bool movementLocked)
    {
        if (cameraTransform == null)
            return;

        bool moving =
            !movementLocked &&
            characterController.isGrounded &&
            horizontalVelocity.sqrMagnitude > 0.05f;

        float frequency = bobWalkFrequency;
        float targetAmplitude = 0f;

        if (moving)
        {
            if (isCrouching)
            {
                frequency = bobCrouchFrequency;
                targetAmplitude = bobCrouchAmplitude;
            }
            else if (isSprinting)
            {
                frequency = bobSprintFrequency;
                targetAmplitude = bobSprintAmplitude;
            }
            else
            {
                frequency = bobWalkFrequency;
                targetAmplitude = bobWalkAmplitude;
            }
        }

        // Suavização independente do framerate.
        bobAmount = Mathf.Lerp(
            bobAmount,
            targetAmplitude,
            1f - Mathf.Exp(-bobBlendSpeed * Time.deltaTime)
        );

        if (moving)
        {
            bobTimer += Time.deltaTime * frequency;
        }
        else if (bobAmount < 0.0005f)
        {
            bobTimer = 0f;
            bobAmount = 0f;
        }

        // A câmara desce com o agachar; o bob é um desvio sobre essa base.
        float heightRatio = standHeight > 0.001f
            ? currentHeight / standHeight
            : 1f;

        float baseY = cameraStandLocalPosition.y * heightRatio;

        cameraTransform.localPosition = new Vector3(
            cameraStandLocalPosition.x + Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f,
            baseY + Mathf.Sin(bobTimer) * bobAmount,
            cameraStandLocalPosition.z
        );
    }

    private void ReportLocomotion()
    {
        if (stateMachine == null)
            return;

        if (isCrouching)
        {
            stateMachine.SetLocomotion(PlayerState.Crouching);
        }
        else if (isSprinting)
        {
            stateMachine.SetLocomotion(PlayerState.Sprinting);
        }
        else
        {
            stateMachine.SetLocomotion(PlayerState.Normal);
        }
    }
}
