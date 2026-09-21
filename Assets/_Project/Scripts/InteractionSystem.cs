using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;

    // Só a layer 8 (Interactable). O filtro vive aqui e não no
    // m_QueriesHitTriggers global, que fica a 1: mexer no flag mudava o
    // comportamento de todas as queries do projecto, não só o deste raycast.
    [SerializeField] private LayerMask interactableMask;

    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TMP_Text interactionText;

    [Header("Estado")]
    [SerializeField] private PlayerStateMachine stateMachine;

    private IInteractable currentInteractable;

    // Quem está em mira, para quem precise de saber sem voltar a lançar o raio.
    public bool HasFocus => currentInteractable != null;

    private void Start()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        // Qualquer camada aberta — inspecção, fotografia, preview, álbum — ou o
        // cursor solto significa que não é o player que manda. Sem esta guarda o
        // raycast continuava a correr por trás da UI e o E abria portas que o
        // jogador nem estava a ver. A pergunta é feita à máquina de estados e não
        // a cada sistema, para uma camada nova não obrigar a voltar aqui.
        if (stateMachine == null ||
            stateMachine.OpenModeCount > 0 ||
            stateMachine.IsCursorFree)
        {
            ClearFocus();
            return;
        }

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

        if (playerCamera != null)
        {
            Ray ray = new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

            // Máscara + Ignore: sem elas o raio parava no primeiro collider
            // qualquer — um trigger volume ou uma grade não-interactiva à frente
            // da porta roubavam-lhe o foco e a porta deixava de responder.
            if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance,
                interactableMask,
                QueryTriggerInteraction.Ignore))
            {
                // Em parent e não no próprio collider: a gaveta e o interruptor
                // são objectos compostos, com a malha que se toca num filho do
                // GameObject que carrega o script.
                currentInteractable =
                    hit.collider.GetComponentInParent<IInteractable>();
            }
        }

        UpdateInteractionUI();
    }

    private void ClearFocus()
    {
        currentInteractable = null;
        UpdateInteractionUI();
    }

    // Dono único do prompt. Mais ninguém lhe toca: quando o InspectionSystem
    // também o ligava, sair da inspecção fazia-o piscar um frame com o texto
    // velho do objecto que já não estava em mira.
    private void UpdateInteractionUI()
    {
        if (interactionPrompt == null)
            return;

        if (currentInteractable == null)
        {
            interactionPrompt.SetActive(false);
            return;
        }

        interactionPrompt.SetActive(true);

        if (interactionText != null)
        {
            interactionText.text =
                "[E] " + currentInteractable.GetInteractionText();
        }
    }
}
