using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;

    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TMP_Text interactionText;

    [SerializeField] private InspectionSystem inspectionSystem;

    private IInteractable currentInteractable;

    private void Start()
    {
        interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        if (inspectionSystem != null && inspectionSystem.IsInspecting)
            return;
            
        CheckForInteractable();

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame &&
            currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    private void CheckForInteractable()
    {
        currentInteractable = null;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance))
        {
            currentInteractable =
                hit.collider.GetComponent<IInteractable>();
        }

        UpdateInteractionUI();
    }

    private void UpdateInteractionUI()
    {
        if (currentInteractable != null)
        {
            interactionPrompt.SetActive(true);

            interactionText.text =
                "[E] " + currentInteractable.GetInteractionText();
        }
        else
        {
            interactionPrompt.SetActive(false);
        }
    }
}