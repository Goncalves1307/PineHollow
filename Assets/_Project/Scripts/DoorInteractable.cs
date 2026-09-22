using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    // A propriedade desta porta que o mundo guarda. Constante e não literal
    // espalhado: quem escreve e quem lê têm de concordar na string, e duas
    // grafias da mesma chave são uma porta que abre em 1986 e continua fechada
    // em 2026 sem nada dar erro.
    public const string ChaveAberta = "aberta";

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

    private bool preparada;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    public string StateId => stateId;

    // Estava enterrado num campo privado sem leitor. O capítulo 4 conta com um
    // flashback a criar uma porta que depois existe em 2026 — sem isto, quem
    // tiver de a ler não tem por onde.
    public bool IsOpen => isOpen;

    public bool IsMoving => isMoving;

    private void Start()
    {
        Preparar();
    }

    // Guarda as rotações e põe a porta como o mundo diz que ela está.
    //
    // Método público e não código dentro do Start porque o Start não corre em
    // EditMode: escrita lá dentro, esta regra não tinha como ser testada, e
    // esta porta é precisamente o critério de aceitação da subtask do world
    // state («o DoorInteractable lê o seu estado dali»). Uma regra que só o
    // Awake ou o Start executam é uma regra sem teste.
    public void Preparar()
    {
        if (doorPivot == null)
        {
            // Antes isto rebentava com um NullReferenceException na linha
            // seguinte. O resto do projecto queixa-se e desiste; uma porta sem
            // pivot é cenário mal montado, não é motivo para deitar abaixo a
            // cena inteira — e a FASE 7 passa a instanciar portas em runtime,
            // onde o estoiro sairia longe de quem o causou.
            Debug.LogError(
                "DoorInteractable em '" + name + "' não tem doorPivot ligado: " +
                "a porta não se move nem lê o seu estado do mundo.",
                this
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(stateId))
        {
            // Sem stateId a porta não tem identidade: o que ela fizer em 1986
            // não tem como ser reencontrado em 2026, e a promessa inteira da
            // FASE 7 cai em silêncio nesta porta. É o mesmo erro que a
            // FontesDeConhecimento já grita para os documentos.
            //
            // Queixa-se mas NÃO desiste, de propósito: uma porta sem stateId
            // continua a abrir e a fechar como sempre, só não persiste. Sair
            // aqui deixava-a sem rotações capturadas, ou seja imóvel — trocar
            // «não guarda o estado» por «não se mexe» era piorar. O LerBool
            // abaixo é inofensivo com um id em branco: o EstadoDoMundo.Obter
            // devolve null para ids vazios, portanto não há estado partilhado
            // entre portas sem id.
            Debug.LogError(
                "DoorInteractable em '" + name + "' não tem stateId: o que se " +
                "lhe fizer numa época não chega à outra.",
                this
            );
        }

        // Só captura se ainda ninguém tiver capturado.
        //
        // Recapturar aqui às cegas corrompia a porta: quem chame Aplicar(true)
        // ANTES do Start — uma porta instanciada em runtime, que é justamente o
        // que o world state passa a fazer — já deixou o pivot na pose ABERTA, e
        // esta linha tomava-a como sendo a fechada. A porta passava a abrir
        // 180º e a «fechar» para aberta. Apanhado por revisor externo.
        if (!preparada)
        {
            closedRotation = doorPivot.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

            preparada = true;
        }

        // O mundo manda sobre o que estava montado na cena. Uma porta autorada
        // fechada que o jogador abriu em 1986 tem de aparecer aberta em 2026 —
        // é literalmente o «feito quando» da subtask 869f4yb88.
        Aplicar(EstadoDoMundo.Instancia.LerBool(stateId, ChaveAberta, isOpen));
    }

    // Põe a porta num estado SEM a animar. É o que a leitura do mundo precisa:
    // uma porta que já estava aberta quando se chega à época não se abre agora
    // à frente do jogador, já lá estava assim.
    public void Aplicar(bool aberta)
    {
        isOpen = aberta;
        isMoving = false;

        if (doorPivot == null)
            return;

        if (!preparada)
        {
            // Aplicar antes de Preparar deixaria as rotações por capturar e a
            // porta saltava para uma orientação que não é nenhuma das duas.
            closedRotation = doorPivot.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

            preparada = true;
        }

        doorPivot.localRotation = isOpen ? openRotation : closedRotation;
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

        if (doorPivot == null)
            return;

        isOpen = !isOpen;
        isMoving = true;

        // Escreve no sítio único. Sem isto a porta continuava a saber o seu
        // estado só para si, que era exactamente o defeito que a subtask
        // 869f4yb88 descreve — e que a outra época nunca chegava a ver.
        if (!string.IsNullOrWhiteSpace(stateId))
            EstadoDoMundo.Instancia.Registar(stateId, ChaveAberta, isOpen);
    }

    public string GetInteractionText()
    {
        return isOpen ? "Fechar" : "Abrir";
    }
}
