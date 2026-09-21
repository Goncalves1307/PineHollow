using UnityEngine;

// Gaveta. Mesma forma da porta, eixo diferente: a porta roda no pivot, a
// gaveta corre para fora. O estado fica numa propriedade só-leitura e não
// num campo privado, porque a FASE 7 (world state 2026/1986) precisa de o
// ler e o save da FASE 21 de o gravar.
public class DrawerInteractable : MonoBehaviour, IInteractable
{
    [Header("Movimento")]
    [SerializeField] private Transform drawerBody;
    [SerializeField] private float openDistance = 0.35f;
    [SerializeField] private float openSpeed = 4f;

    [Header("Estado")]
    // Identidade estável para quem venha de fora — world state e save. O nome
    // do GameObject não serve: muda com o cenário.
    [SerializeField] private string stateId;

    private bool isOpen;
    private bool isMoving;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    public string StateId => stateId;
    public bool IsOpen => isOpen;

    private void Start()
    {
        if (drawerBody == null)
            return;

        closedPosition = drawerBody.localPosition;

        // Para fora é o +Z local: a gaveta corre na direcção que a frente do
        // móvel aponta.
        openPosition =
            closedPosition + Vector3.forward * openDistance;
    }

    private void Update()
    {
        if (!isMoving || drawerBody == null)
            return;

        Vector3 targetPosition =
            isOpen ? openPosition : closedPosition;

        drawerBody.localPosition = Vector3.MoveTowards(
            drawerBody.localPosition,
            targetPosition,
            Time.deltaTime * openSpeed * openDistance
        );

        if (Vector3.Distance(
            drawerBody.localPosition,
            targetPosition) < 0.001f)
        {
            drawerBody.localPosition = targetPosition;
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
