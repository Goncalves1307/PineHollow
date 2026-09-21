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

    // Partilha o AudioSource do Player com os passos, de propósito. Um segundo
    // AudioSource no mesmo GameObject punha o GetComponent de recurso do
    // FootstepSystem a depender da ordem dos componentes — e os dois nunca
    // tocam ao mesmo tempo, que o movimento está trancado enquanto se examina.
    [Header("Som")]
    [SerializeField] private AudioSource inspectionSource;
    [SerializeField] private AudioClip[] pickupClips;
    [SerializeField] private AudioClip[] putdownClips;
    [SerializeField] private float soundVolume = 0.6f;

    [Header("Pitch")]
    [SerializeField] private float minPitch = 0.92f;
    [SerializeField] private float maxPitch = 1.08f;

    private GameObject inspectedObject;

    // A distância a que o objecto está agora. O zoom mexe nesta, não no campo
    // configurado — antes o [SerializeField] era reescrito a cada inspecção.
    private float currentDistance;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;

    // O que está escrito nas costas do que se tem na mão. Vazio na esmagadora
    // maioria dos objectos: só as fotografias têm verso.
    private string descricaoActual;
    private string versoActual;

    public bool IsInspecting => inspectedObject != null;

    public bool TemVerso => !string.IsNullOrWhiteSpace(versoActual);

    public bool AMostrarVerso { get; private set; }

    private void Awake()
    {
        if (inspectionSource == null)
            inspectionSource = GetComponent<AudioSource>();

        if (inspectionSource != null)
        {
            // O som é do objecto que o jogador tem nas mãos: 2D, sem atenuação
            // por distância. O FootstepSystem já o põe assim, mas depender
            // disso partia-se no dia em que ele saísse do Player.
            inspectionSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        // Uma dependência de UI por ligar não pode ficar calada. Não trava a
        // inspecção — o objecto vem à mão à mesma —, mas diz-se uma vez, aqui,
        // e não a cada exame. O realce mudo, a LayerMask a zero e o stateId
        // vazio foram todos isto: nada acontece, e ninguém sabe porquê.
        if (inspectionControls == null || descriptionText == null)
        {
            Debug.LogError(
                $"{name}: ecrã da inspecção por ligar no inspector — " +
                "examinar não mostra os controlos nem a descrição.",
                this
            );
        }

        // Não depender do que ficou guardado na cena. Um GameObject deixado
        // activo no editor virava HUD permanente, que o GDD proíbe, e ninguém
        // dava por isso até abrir o jogo.
        ShowUI(false, null);
    }

    // A descrição e o verso são opcionais de propósito: quem já chamava isto
    // com um argumento só continua a compilar. O verso é o que a FASE 5
    // trouxe — uma fotografia tem duas faces, um cinzeiro não.
    public void Inspect(GameObject target, string descricao = null, string verso = null)
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

        descricaoActual = descricao;
        versoActual = verso;
        AMostrarVerso = false;

        // A UI primeiro, o modo depois. Ao contrário, uma falha a ligar o HUD
        // deixava o Inspecting na pilha com o objecto ainda no chão.
        ShowUI(true, descricao);

        Tocar(pickupClips);

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
            HandleFlip();
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

    private void HandleFlip()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
            Virar();
    }

    // Virar é uma meia-volta em torno do eixo vertical da câmara, e não do
    // eixo do objecto: depois de o jogador o rodar com o rato, o «para cima»
    // do objecto já não é o dele, e virá-lo pelo eixo próprio mandava a
    // fotografia para uma posição que não é de trás nenhuma.
    //
    // Devolve false no que não tem verso: um objecto de uma face só não se
    // vira, e virá-lo mostrava um painel vazio.
    public bool Virar()
    {
        if (!IsInspecting || !TemVerso)
            return false;

        AMostrarVerso = !AMostrarVerso;

        if (playerCamera != null)
        {
            inspectedObject.transform.Rotate(
                playerCamera.transform.up,
                180f,
                Space.World
            );
        }

        ShowUI(true, AMostrarVerso ? versoActual : descricaoActual);

        return true;
    }

    private void ExitInspection()
    {
        inspectedObject.transform.SetParent(originalParent);

        inspectedObject.transform.position = originalPosition;
        inspectedObject.transform.rotation = originalRotation;

        inspectedObject = null;

        descricaoActual = null;
        versoActual = null;
        AMostrarVerso = false;

        // O som antes do PopMode. Ao contrário, o movimento já estava
        // destrancado quando o clip arrancava, e o primeiro passo — que chega
        // aos 1,4 m em ~0,28 s — reescrevia o pitch do AudioSource partilhado
        // a meio da cauda de um clip de 0,30 a 0,41 s.
        Tocar(putdownClips);

        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.Inspecting);

        ShowUI(false, null);
    }

    // Sobrevive a não haver clips: até os haver, não toca nada e não estoira.
    private void Tocar(AudioClip[] clips)
    {
        if (inspectionSource == null || clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];

        if (clip == null)
            return;

        inspectionSource.pitch = Random.Range(minPitch, maxPitch);

        inspectionSource.PlayOneShot(clip, soundVolume);
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
