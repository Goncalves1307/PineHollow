using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InspectionSystem : MonoBehaviour
{
    [Header("Inspection")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private float inspectionDistance = 1.5f;
    [SerializeField] private float minDistance = 0.7f;
    [SerializeField] private float maxDistance = 2.5f;
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float rotationSpeed = 0.15f;

    [Header("UI")]
    [SerializeField] private GameObject inspectionControls;

    // Só o TMP, e não também o GameObject que o carrega: são o mesmo objecto,
    // e duas referências para a mesma coisa é mais uma que pode ficar por
    // ligar. O InteractionSystem tem as duas porque lá o texto é filho do
    // painel.
    [SerializeField] private TMP_Text descriptionText;

    private GameObject inspectedObject;

    // A distância a que o objecto está agora. O zoom mexe nesta, não no campo
    // configurado — antes o [SerializeField] era reescrito a cada inspecção.
    private float currentDistance;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;

    public bool IsInspecting => inspectedObject != null;

    private void Start()
    {
        // Não depender do que ficou guardado na cena. Um GameObject deixado
        // activo no editor virava HUD permanente, que o GDD proíbe, e ninguém
        // dava por isso até abrir o jogo.
        ShowUI(false, null);
    }

    // A descrição é opcional de propósito: o PhotoInteractable da FASE 5 vai
    // reutilizar este sistema e a assinatura antiga continua a compilar.
    public void Inspect(GameObject target, string descricao = null)
    {
        if (IsInspecting)
            return;

        if (target == null)
            return;

        // Sem estes dois não há inspecção nenhuma, e é melhor dizê-lo do que
        // deixar uma exception a meio a prender o modo na pilha.
        if (playerCamera == null || stateMachine == null)
        {
            Debug.LogError(
                $"{name}: câmara ou máquina de estados por ligar — " +
                "examinar não faz nada.",
                this
            );

            return;
        }

        // Clamp na distância inicial: o campo do inspector podia estar fora
        // dos limites do zoom e o objecto aparecia num sítio onde o scroll
        // nunca mais o punha.
        currentDistance = Mathf.Clamp(
            inspectionDistance,
            minDistance,
            maxDistance
        );

        inspectedObject = target;

        originalPosition = target.transform.position;
        originalRotation = target.transform.rotation;
        originalParent = target.transform.parent;

        // A UI primeiro, o modo depois. Ao contrário, uma falha a ligar o HUD
        // deixava o Inspecting na pilha com o objecto ainda no chão.
        ShowUI(true, descricao);

        stateMachine.PushMode(PlayerState.Inspecting);

        target.transform.SetParent(playerCamera.transform);

        target.transform.localPosition =
            new Vector3(0f, 0f, currentDistance);

        target.transform.localRotation =
            Quaternion.identity;
    }

    private void Update()
    {
        if (!IsInspecting)
            return;

        // Só roda e faz zoom quem está no topo da pilha. Sem isto, qualquer
        // camada aberta por cima — o verso da fotografia da FASE 5, um
        // flashback — deixava o rato a rodar o objecto por trás dela.
        if (stateMachine != null &&
            stateMachine.IsTopMode(PlayerState.Inspecting))
        {
            HandleRotation();
            HandleZoom();
        }

        if (stateMachine != null &&
            stateMachine.ConsumeBack(PlayerState.Inspecting))
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

        currentDistance -= scroll * zoomSpeed * 0.01f;

        currentDistance = Mathf.Clamp(
            currentDistance,
            minDistance,
            maxDistance
        );

        inspectedObject.transform.localPosition =
            new Vector3(0f, 0f, currentDistance);
    }

    private void ExitInspection()
    {
        inspectedObject.transform.SetParent(originalParent);

        inspectedObject.transform.position = originalPosition;
        inspectedObject.transform.rotation = originalRotation;

        inspectedObject = null;

        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.Inspecting);

        ShowUI(false, null);
    }

    // Um objecto sem descrição não mostra uma caixa vazia: a linha desaparece
    // e ficam só os controlos.
    private void ShowUI(bool visivel, string descricao)
    {
        if (inspectionControls != null)
            inspectionControls.SetActive(visivel);

        if (descriptionText == null)
            return;

        bool temTexto = visivel && !string.IsNullOrWhiteSpace(descricao);

        descriptionText.text = temTexto ? descricao : string.Empty;
        descriptionText.gameObject.SetActive(temTexto);
    }
}
