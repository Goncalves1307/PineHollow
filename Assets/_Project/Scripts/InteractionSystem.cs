using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;

    // O que o raio CONSIDERA — geometria do mundo incluída, não só a layer 8.
    // Restringir a máscara aos interactables parecia limpo e tirava a oclusão:
    // sem as paredes no raio, o foco atravessava-as e o E abria a porta do
    // outro lado do tabique. Quem responde decide-se no fim, por ter ou não
    // IInteractable; o que fica de fora daqui é só quem nunca deve travar o
    // raio — o próprio player, a UI e as layers de render.
    [SerializeField] private LayerMask raycastMask;

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

            // Ignore nos triggers: o m_QueriesHitTriggers do projecto está a 1
            // e fica a 1 — mexer no flag mudava todas as queries. Sem isto, o
            // primeiro trigger volume à frente da porta roubava-lhe o foco.
            //
            // O sólido continua a travar o raio, e é isso que dá a oclusão: se
            // o que estiver mais perto não for interactivo, não há foco, que é
            // o que se espera de uma parede.
            if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance,
                raycastMask,
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
