using UnityEngine;

// Gaveta. Mesma forma da porta, eixo diferente: a porta roda no pivot, a
// gaveta corre para fora. O estado fica numa propriedade só-leitura e não
// num campo privado, porque a FASE 7 (world state 2026/1986) precisa de o
// ler e o save da FASE 21 de o gravar.
public class DrawerInteractable : MonoBehaviour, IInteractable
{
    [Header("Movimento")]
    [SerializeField] private Transform drawerBody;

    // Ambos exigem valor positivo, e o Start recusa-se a arrancar sem ele.
    // Com openDistance negativo o MoveTowards afasta-se do alvo e a gaveta
    // parte para o infinito; com openSpeed a zero nunca converge. Nos dois
    // casos o isMoving ficava preso e o objecto morria em silêncio.
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

    private bool isUsable;

    private void Start()
    {
        if (drawerBody == null)
        {
            Debug.LogError(
                $"[{name}] Gaveta sem drawerBody: não vai responder ao E.",
                this);
            return;
        }

        if (openDistance <= 0f || openSpeed <= 0f)
        {
            Debug.LogError(
                $"[{name}] Gaveta com openDistance={openDistance} e " +
                $"openSpeed={openSpeed}: ambos têm de ser positivos. Para " +
                "abrir ao contrário, roda o móvel.",
                this);
            return;
        }

        isUsable = true;

        closedPosition = drawerBody.localPosition;

        // Para fora é o +Z local: a gaveta corre na direcção que a frente do
        // móvel aponta.
        openPosition =
            closedPosition + Vector3.forward * openDistance;
    }

    private void Update()
    {
        if (!isMoving || !isUsable)
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
        // Sem isUsable nem se comuta o estado: comutar e não se mexer deixava
        // a gaveta a dizer "Fechar" sem nunca ter aberto.
        if (isMoving || !isUsable)
            return;

        isOpen = !isOpen;
        isMoving = true;
    }

    public string GetInteractionText()
    {
        return isOpen ? "Fechar" : "Abrir";
    }
}
