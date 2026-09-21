using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Movimento")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 3f;

    [Header("Estado")]
    // Identidade estável para o world state 2026/1986 e para o save: o nome do
    // GameObject não serve porque muda com o cenário.
    [SerializeField] private string stateId;

    private bool isOpen;
    private bool isMoving;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    public string StateId => stateId;

    // Estava enterrado num campo privado sem leitor. O capítulo 4 conta com um
    // flashback a criar uma porta que depois existe em 2026 — sem isto, quem
    // tiver de a ler não tem por onde.
    public bool IsOpen => isOpen;

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