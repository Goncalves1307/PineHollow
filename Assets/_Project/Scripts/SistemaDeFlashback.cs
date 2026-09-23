using UnityEngine;

// Em que ponto do ciclo 2026 → 1986 → 2026 o jogo está.
public enum FaseDoFlashback
{
    Fora,

    // O clarão do alinhamento ainda está no ecrã. Espera-se por ele de
    // propósito — ver porquê no SistemaDeFlashback.Iniciar.
    Clarao,

    ACobrir,
    ADescobrir,
    Dentro,
    ASair,
    AVoltar
}

// O ciclo inteiro da task: 2026 → Alinhamento → Transição → 1986 → Gameplay →
// Evento → 2026.
//
// É quem consome o PhotoComparisonSystem.ConsumirAlinhamento(), que estava
// escrito desde a FASE 5 à espera de alguém. Não há aqui evento nenhum nem
// coroutine: o Update pergunta, como tudo neste projecto, e toda a lógica está
// em Avancar(delta), que recebe o tempo em parâmetro para ter teste — em
// EditMode o Time.deltaTime é zero e uma transição que dependesse dele nunca
// acabava.
public class SistemaDeFlashback : MonoBehaviour
{
    [Header("De onde vem o gatilho")]
    [SerializeField] private PhotoComparisonSystem comparacao;
    [SerializeField] private PhotoAlbumSystem album;

    [Header("Estado do player")]
    [SerializeField] private PlayerStateMachine stateMachine;

    [Header("Transição")]
    [SerializeField] private TransicaoDeEpoca transicao;
    [SerializeField] private AmbienteDeEpoca ambiente;
    [SerializeField] private AmbienteSonoroDeEpoca som;

    [Tooltip("Segundos a deixar o clarão do alinhamento respirar antes de cobrir.")]
    [SerializeField] private float esperaPeloClarao = 0.45f;

    [Header("O que acaba o flashback")]
    [SerializeField] private FimDeFlashback fim;

    [Header("A marca que o jogador deixa")]
    // «Não podes observar o passado sem deixar uma marca nele.» Quem esteve em
    // 1986 foi o jogador, e é a presença DELE que fica escrita — não uma acção
    // do Thomas. Pôr o Thomas em MarcaOPresente para isto fazia-o parar e
    // encarar o jogador no primeiro flashback, que é o degrau Inconsciente.
    //
    // Vazio é um flashback que não deixa marca, e isso contradiz a regra
    // central: o Start queixa-se.
    [Tooltip("stateId que fica escrito no mundo ao voltar deste flashback.")]
    [SerializeField] private string stateIdDaPresenca;

    public const string ChaveDaPresenca = "presente";

    public string StateIdDaPresenca => stateIdDaPresenca;

    private float tempo;

    public FaseDoFlashback Fase { get; private set; } = FaseDoFlashback.Fora;

    public bool EstaNoFlashback =>
        Fase != FaseDoFlashback.Fora;

    private void Start()
    {
        // Põe o mundo de acordo com a época em que está antes de o jogador ver
        // seja o que for. Um objecto de 1986 deixado activo no editor ficava
        // activo em 2026 até à primeira troca — e era visível na cena inteira.
        EstadoDoMundo.Instancia.Reaplicar();

        if (string.IsNullOrWhiteSpace(stateIdDaPresenca))
        {
            Debug.LogWarning(
                "SistemaDeFlashback em '" + name + "' sem stateIdDaPresenca: " +
                "voltar deste flashback não muda nada em 2026.",
                this
            );
        }
    }

    private void Update()
    {
        if (Fase == FaseDoFlashback.Fora &&
            comparacao != null &&
            comparacao.ConsumirAlinhamento())
        {
            Iniciar();
        }

        Avancar(Time.deltaTime);
    }

    // Arranca o ciclo. Público para os testes e para quem, mais tarde, quiser
    // entrar num flashback por outra porta que não o alinhamento.
    public void Iniciar()
    {
        if (Fase != FaseDoFlashback.Fora)
            return;

        // Começa pelo clarão e não por cobrir já, porque o clarão é a
        // confirmação de que o jogador alinhou — o GDD não lhe diz que duas
        // fotografias alinham, ele descobre-o, e tapar o ecrã no mesmo frame
        // roubava-lhe a única confirmação que existe.
        //
        // E NÃO se fecha a comparação aqui: o PhotoComparisonSystem.Fechar()
        // põe o flashRestante a zero, ou seja, fechar MATA o clarão. Ele fica
        // aberto até ao ecrã estar tapado, e só então se fecha.
        Fase = FaseDoFlashback.Clarao;
        tempo = 0f;
    }

    // Um passo do ciclo. Toda a máquina está aqui, sem input e sem Time.
    public void Avancar(float delta)
    {
        // Um Esc dentro do flashback não fecha nada — sai-se pelo evento, não
        // pelo teclado —, mas tem de ser CONSUMIDO enquanto o modo estiver na
        // pilha. Sem isto, o ResolveUnconsumedBack da máquina vê um Esc por
        // consumir com uma camada no topo, acusa o dono de não estar a correr
        // o Update (falso, está) e tira o Flashback da pilha à força: durante
        // 1986 o jogador voltava a poder abrir portas e a ver a mira, e um
        // segundo Esc — já com a pilha vazia — soltava o cursor e deixava-o
        // congelado até clicar.
        //
        // Está AQUI, antes do switch, e não dentro do case Dentro: o modo é
        // empurrado no fim de ACobrir e só sai em VoltarA2026, portanto está na
        // pilha durante ADescobrir e ASair também — cerca de meio segundo de
        // cada lado, com o ecrã preto, que é exactamente quando alguém carrega
        // no Esc. Pô-lo só no Dentro foi uma primeira correcção incompleta,
        // apanhada na segunda ronda de revisão.
        //
        // O ConsumeBack só devolve true a quem está no topo, portanto isto é
        // inofensivo se outra camada estiver por cima.
        if (stateMachine != null && stateMachine.IsModeOpen(PlayerState.Flashback))
            stateMachine.ConsumeBack(PlayerState.Flashback);

        switch (Fase)
        {
            case FaseDoFlashback.Fora:
                return;

            case FaseDoFlashback.Clarao:
                tempo += Mathf.Max(0f, delta);

                if (tempo >= esperaPeloClarao)
                    Fase = FaseDoFlashback.ACobrir;

                return;

            case FaseDoFlashback.ACobrir:
                if (Cobrir(delta))
                {
                    EntrarEm1986();

                    Fase = FaseDoFlashback.ADescobrir;
                }

                return;

            case FaseDoFlashback.ADescobrir:
                if (Descobrir(delta, Epoca.Passado1986))
                    Fase = FaseDoFlashback.Dentro;

                return;

            case FaseDoFlashback.Dentro:
                // O «Evento» do diagrama da task.
                if (fim == null)
                {
                    // Sem condição ligada ninguém acaba isto, e do modo
                    // Flashback não se sai pelo Esc: era o jogador preso em
                    // 1986 sem log, sem pista e sem saída. Os outros nulos
                    // deste ficheiro degradam com elegância; este trancava.
                    Debug.LogError(
                        "SistemaDeFlashback em '" + name + "' não tem FimDeFlashback " +
                        "ligado: nada acabaria o flashback. A voltar a 2026.",
                        this
                    );

                    Fase = FaseDoFlashback.ASair;

                    return;
                }

                if (fim.Terminou())
                    Fase = FaseDoFlashback.ASair;

                return;

            case FaseDoFlashback.ASair:
                if (Cobrir(delta))
                {
                    VoltarA2026();

                    Fase = FaseDoFlashback.AVoltar;
                }

                return;

            case FaseDoFlashback.AVoltar:
                if (Descobrir(delta, Epoca.Presente2026))
                    Fase = FaseDoFlashback.Fora;

                return;
        }
    }

    private bool Cobrir(float delta)
    {
        // Sem véu ligado a troca faz-se à mesma, só que vê-se. É feio e está
        // escrito no log de quem montou a cena — não é motivo para o jogador
        // ficar preso a meio de uma transição que nunca completa.
        return transicao == null || transicao.Avancar(delta, true);
    }

    private bool Descobrir(float delta, Epoca epoca)
    {
        bool imagem = transicao == null || transicao.Avancar(delta, false);
        bool audio = som == null || som.Avancar(delta, epoca);

        return imagem && audio;
    }

    // A troca, com o ecrã já tapado. A ordem não é livre.
    private void EntrarEm1986()
    {
        // Primeiro fecham-se as camadas de UI, e só depois se empurra o
        // Flashback. Ao contrário, o Flashback ficava debaixo do álbum na
        // pilha: o topo mandaria, o cursor ficava solto e o jogador chegava a
        // 1986 a olhar para um álbum de fotografias sem poder andar.
        if (comparacao != null)
        {
            comparacao.Fechar();

            // Esvazia um alinhamento que tenha sido marcado DEPOIS do que nos
            // trouxe aqui. Durante a fase Clarao o painel continua aberto e o
            // ComparingPhotos continua no topo, portanto o jogador — que
            // acabou de arrastar e ainda tem o botão em baixo — pode sair e
            // voltar a entrar na tolerância e marcar outro. O Fechar() não
            // limpa o sinalizador, e ele ficava à espera: no primeiro frame
            // depois de se voltar a 2026, o ciclo arrancava sozinho outra vez
            // sem o jogador ter tocado em nada.
            comparacao.ConsumirAlinhamento();
        }

        if (album != null)
            album.CloseAlbum();

        EstadoDoMundo.Instancia.MudarPara(Epoca.Passado1986);

        if (ambiente != null)
            ambiente.Aplicar(Epoca.Passado1986);

        // A condição de fim é rearmada aqui e não à saída: rever um flashback
        // com a condição já satisfeita da vez anterior acabava-o no primeiro
        // frame, e o jogador via 1986 durante meio segundo.
        if (fim != null)
            fim.Recomecar();

        if (stateMachine != null)
        {
            stateMachine.PushMode(PlayerState.Flashback);

            if (stateMachine.OpenModeCount > 1)
            {
                // Alguma outra camada ficou aberta por baixo. O jogador não
                // fica trancado — o Flashback está no topo e não tranca nada —
                // mas o Esc dentro do flashback vai fechar essa camada de
                // surpresa, e quem montou a cena tem de saber.
                Debug.LogWarning(
                    "Entrou em 1986 com " + stateMachine.OpenModeCount +
                    " camadas abertas: o Flashback devia ser a única. " +
                    "Modo no topo: " + stateMachine.Current
                );
            }
        }
    }

    // O regresso ao presente, com o que 1986 escreveu já no mundo.
    private void VoltarA2026()
    {
        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.Flashback);

        // A marca é escrita ANTES de mudar de época: o MudarPara acerta o mundo
        // inteiro, e o que se acertar nele tem de já encontrar 1986 mexido.
        // Escreve-se aqui, com o ecrã tapado, e não à entrada: a fotografia só
        // pode mudar quando o jogador já não está a olhar para ela em 1986.
        if (!string.IsNullOrWhiteSpace(stateIdDaPresenca))
            EstadoDoMundo.Instancia.Registar(stateIdDaPresenca, ChaveDaPresenca, true);

        EstadoDoMundo.Instancia.MudarPara(Epoca.Presente2026);

        if (ambiente != null)
            ambiente.Aplicar(Epoca.Presente2026);
    }
}
