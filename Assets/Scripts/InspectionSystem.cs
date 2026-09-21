using UnityEngine;
using UnityEngine.InputSystem;

public class InspectionSystem : MonoBehaviour
{
    [Header("Inspection")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float inspectionDistance = 1.5f;
    [SerializeField] private float minDistance = 0.7f;
    [SerializeField] private float maxDistance = 2.5f;
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float rotationSpeed = 0.15f;

    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private GameObject inspectionControls;

    private GameObject inspectedObject;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;

    public bool IsInspecting => inspectedObject != null;

    public void Inspect(GameObject target)
    {
        if (IsInspecting)
            return;

        inspectionDistance = 1.5f;

        inspectedObject = target;

        originalPosition = target.transform.position;
        originalRotation = target.transform.rotation;
        originalParent = target.transform.parent;

        playerController.IsMovementLocked = true;
        playerController.IsLookLocked = true;

        interactionPrompt.SetActive(false);
        inspectionControls.SetActive(true);

        target.transform.SetParent(playerCamera.transform);

        target.transform.localPosition =
            new Vector3(0f, 0f, inspectionDistance);

        target.transform.localRotation =
            Quaternion.identity;
    }

    private void Update()
    {
        if (!IsInspecting)
            return;

        HandleRotation();
        HandleZoom();

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitInspection();
        }
    }

    private void HandleRotation()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float rotationX = mouseDelta.y * rotationSpeed;
        float rotationY = -mouseDelta.x * rotationSpeed;

        inspectedObject.transform.Rotate(
            Vector3.right,
            rotationX,
            Space.World
        );

        inspectedObject.transform.Rotate(
            Vector3.up,
            rotationY,
            Space.World
        );
    }

    private void HandleZoom()
    {
        if (Mouse.current == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) < 0.01f)
            return;

        inspectionDistance -= scroll * zoomSpeed * 0.01f;

        inspectionDistance = Mathf.Clamp(
            inspectionDistance,
            minDistance,
            maxDistance
        );

        inspectedObject.transform.localPosition =
            new Vector3(0f, 0f, inspectionDistance);
    }

    private void ExitInspection()
    {
        inspectedObject.transform.SetParent(originalParent);

        inspectedObject.transform.position = originalPosition;
        inspectedObject.transform.rotation = originalRotation;

        inspectedObject = null;

        playerController.IsMovementLocked = false;
        playerController.IsLookLocked = false;

        interactionPrompt.SetActive(true);
        inspectionControls.SetActive(false);
    }
}