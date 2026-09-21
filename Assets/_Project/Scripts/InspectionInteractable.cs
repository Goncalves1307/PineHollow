using UnityEngine;

public class InspectionInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        InspectionSystem inspectionSystem =
            FindFirstObjectByType<InspectionSystem>();

        if (inspectionSystem != null)
        {
            inspectionSystem.Inspect(gameObject);
        }
    }

    public string GetInteractionText()
    {
        return "Examinar";
    }
}