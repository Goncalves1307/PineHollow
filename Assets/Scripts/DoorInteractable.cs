using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform doorPivot;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 3f;

    private bool isOpen;
    private bool isMoving;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        closedRotation = doorPivot.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    private void Update()
    {
        if (!isMoving)
            return;

        Quaternion targetRotation =
            isOpen ? openRotation : closedRotation;

        doorPivot.localRotation = Quaternion.Slerp(
            doorPivot.localRotation,
            targetRotation,
            Time.deltaTime * openSpeed
        );

        if (Quaternion.Angle(
            doorPivot.localRotation,
            targetRotation) < 0.5f)
        {
            doorPivot.localRotation = targetRotation;
            isMoving = false;
        }
    }

    public void Interact()
    {
        if (isMoving)
            return;

        isOpen = !isOpen;
        isMoving = true;
    }

    public string GetInteractionText()
    {
        return isOpen ? "Fechar" : "Abrir";
    }
}