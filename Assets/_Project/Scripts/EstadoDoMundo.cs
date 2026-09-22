using System.Collections.Generic;
using UnityEngine;

// Em que época o mundo está, e o que nele ficou diferente.
//
// É o pré-requisito de tudo o que é narrativo, e até à FASE 7 não existia:
// nenhum script tinha noção de época, e o DoorInteractable guardava o seu
// estado num campo privado que ninguém escrevia de fora. O capítulo 4 conta com
// um flashback a criar uma porta que passa a existir em 2026 — sem um sítio
// único onde 1986 escreve e 2026 lê, isso não tem por onde acontecer.
//
// Vive no objecto do Bootstrap e alcança-se por Instancia, não por
// [SerializeField] — mesma razão que o RegistoDeConhecimento, e ela não é
// preguiça de ligar no inspector: o Unity NÃO serializa referências entre
// cenas, e um componente do Prototype_Player não consegue apontar para um
// objecto que vive no Bootstrap e é DontDestroyOnLoad.
//
// A Instancia cria o estado se não houver nenhum, porque entrar em play mode
// directamente no Prototype_Player é como este projecto se testa e como o
// Fase5Capturas corre. Sem isso, o mundo inteiro ficava em 2026 com o código
// todo escrito.
//
// NÃO grava em disco. Atravessa as trocas de época e a sessão inteira em
// memória, e mais nada — escrever um save é a FASE 21, que declara depender
// desta. O formato está preparado para isso (uma lista de AlteracaoDeMundo, que
// é exactamente o que lá se há-de gravar), mas o ficheiro não se escreve aqui.
public class EstadoDoMundo : MonoBehaviour
{
    // Lista e não dicionário: o Unity não serializa dicionários, e a FASE 21
    // grava isto tal e qual. O custo é uma varredura por leitura, sobre uma
    // lista que tem tantas entradas quantas as coisas a que o jogador mexeu —
    // e ninguém lê isto por frame.
    [SerializeField] private List<AlteracaoDeMundo> alteracoes = new List<AlteracaoDeMundo>();

    [SerializeField] private Epoca epocaActual = Epoca.Presente2026;

    private static EstadoDoMundo instancia;

    // True no que a Instancia criou de emergência, false no que alguém pôs numa
    // cena. Os dois não valem o mesmo — ver Assumir().
    private bool automatico;

    public bool Automatico => automatico;

    public Epoca EpocaActual => epocaActual;

    public bool Em1986 => epocaActual == Epoca.Passado1986;

    public IReadOnlyList<AlteracaoDeMundo> Alteracoes => alteracoes;

    public int Total => alteracoes.Count;

    public static EstadoDoMundo Instancia
    {
        get
        {
            if (instancia != null && !instancia.automatico)
                return instancia;

            // Um estado POSTO À MÃO ganha sempre a um criado de emergência, e o
            // de emergência sai da frente quando o verdadeiro aparece. Sem esta
            // regra, quem chegasse primeiro ficava com o lugar: entrar no
            // Prototype_Player criava um estado automático, e o do Bootstrap —
            // o que sobrevive às trocas de cena — ficava a apanhar pó enquanto
            // o jogo escrevia no outro.
            EstadoDoMundo autorado = Autorado();

            if (autorado != null)
            {
                // Delega em Assumir() e não repete a regra aqui. Repetida, as
                // duas cópias do RegistoDeConhecimento divergiram e o jogador
                // perdia em silêncio o que tinha aprendido — aqui perderia as
                // portas que abriu em 1986, que é a única coisa que a FASE 7
                // promete.
                autorado.Assumir();

                return instancia;
            }

            if (instancia != null)
                return instancia;

            GameObject anfitriao = new GameObject("EstadoDoMundo");

            instancia = anfitriao.AddComponent<EstadoDoMundo>();
            instancia.automatico = true;

            return instancia;
        }
    }

    private static EstadoDoMundo Autorado()
    {
        // Sem o FindObjectsSortMode: a sobrecarga que o leva está obsoleta no
        // Unity 6 e este projecto compila com zero avisos.
        EstadoDoMundo[] todos = FindObjectsByType<EstadoDoMundo>(
            FindObjectsInactive.Include
        );

        foreach (EstadoDoMundo candidato in todos)
        {
            if (candidato != null && !candidato.automatico)
                return candidato;
        }

        return null;
    }

    private void Awake()
    {
        Assumir();
    }

    // Decide qual dos estados fica de pé quando aparece um segundo.
    //
    // Público porque em EditMode o Awake não corre, e esta regra não pode ficar
    // sem teste: escrita dentro do Awake no RegistoDeConhecimento, ela estava
    // INVERTIDA e só um revisor externo deu por isso. Este ficheiro nasce com o
    // erro já conhecido.
    public void Assumir()
    {
        if (instancia != null && instancia != this)
        {
            if (instancia.automatico && !automatico)
            {
                // O autorado toma o lugar do de emergência e leva com ele o que
                // já tinha mudado no mundo — a troca não pode custar ao jogador
                // a porta que ele abriu antes dela.
                foreach (AlteracaoDeMundo alteracao in instancia.alteracoes)
                {
                    if (alteracao != null)
                        Registar(alteracao.StateId, alteracao.Chave, alteracao.Valor);
                }

                // A época também vem com ele. Sem isto, assumir a meio de um
                // flashback devolvia o mundo a 2026 sem ninguém ter saído de
                // 1986 — e o jogador via a cidade mudar debaixo dos pés.
                epocaActual = instancia.epocaActual;

                Destruir(instancia.gameObject);
            }
            else
            {
                // Dois estados é o mundo partido ao meio: metade dos
                // interactables escreve num, a outra metade lê o outro, e uma
                // porta aberta em 1986 aparece fechada em 2026 sem explicação.
                Destruir(gameObject);

                return;
            }
        }

        instancia = this;

        // DontDestroyOnLoad só aceita objectos de raiz, e só existe em play
        // mode — fora dele queixa-se no log. Um estado pendurado noutra coisa
        // (como nos testes) não sobrevive à troca de cena, e isso está certo:
        // quem o quer permanente põe-no no Bootstrap.
        if (Application.isPlaying && transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }

    // Esquece a instância estática, e leva com ela a que tiver sido criada de
    // emergência. Público para os testes: sem isto, um teste herdava o mundo do
    // anterior e passava por razões que não são as suas.
    public static void Esquecer()
    {
        if (instancia != null && instancia.automatico)
            Destruir(instancia.gameObject);

        instancia = null;
    }

    private static void Destruir(GameObject alvo)
    {
        if (Application.isPlaying)
            Destroy(alvo);
        else
            DestroyImmediate(alvo);
    }

    // Põe o mundo numa época e manda todos os ObjectoDeEpoca acertarem-se.
    //
    // NÃO sai cedo quando a época já é a pedida, e isso é de propósito. Um
    // Garantir* que sai cedo nunca corrige o que já está montado — foi assim
    // que o flash e as fotografias da FASE 5 ficaram por ligar quando o painel
    // vinha de uma corrida anterior, e a FASE 6 repetiu o erro duas vezes no
    // ficheiro que o critica em comentário. Aqui, sair cedo significava: entrar
    // no Prototype_Player com um objecto de 1986 deixado activo no editor e
    // ele ficar activo em 2026, porque «a época já era 2026, não há nada a
    // fazer». Chamar isto é sempre pôr o mundo em ordem, nunca só mudá-lo.
    public void MudarPara(Epoca epoca)
    {
        epocaActual = epoca;

        ObjectoDeEpoca.AplicarATodos(epoca);
    }

    // Reaplica a época corrente. Para quem acabou de montar ou instanciar
    // objectos e precisa que eles se acertem sem fingir uma mudança de época.
    public void Reaplicar()
    {
        MudarPara(epocaActual);
    }

    public bool Sabe(string stateId, string chave)
    {
        return Obter(stateId, chave) != null;
    }

    public AlteracaoDeMundo Obter(string stateId, string chave)
    {
        if (string.IsNullOrWhiteSpace(stateId) || string.IsNullOrWhiteSpace(chave))
            return null;

        foreach (AlteracaoDeMundo alteracao in alteracoes)
        {
            if (alteracao != null && alteracao.Corresponde(stateId, chave))
                return alteracao;
        }

        return null;
    }

    // Escreve, e devolve true se isto mudou alguma coisa. Ao contrário do
    // RegistoDeConhecimento.Registar — que recusa a segunda vez porque aprender
    // duas vezes não é aprender — aqui a segunda vez É uma mudança: uma porta
    // abre e fecha as vezes que o jogador quiser, e o valor mais recente é o
    // que vale. Devolve false só quando o valor já era esse.
    public bool Registar(string stateId, string chave, string valor)
    {
        if (string.IsNullOrWhiteSpace(stateId))
        {
            // Sem stateId não há como voltar a perguntar por isto: nem o
            // interactable se reconhece na sessão seguinte, nem o save o
            // reencontra. É o mesmo erro que a FontesDeConhecimento já grita.
            Debug.LogError(
                "Alteração sem stateId não se regista — seria uma mudança no " +
                "mundo que nada consegue voltar a encontrar. Chave: " + chave
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(chave))
        {
            Debug.LogError(
                "Alteração sem chave não se regista — um objecto tem mais do " +
                "que uma coisa que pode mudar. stateId: " + stateId
            );

            return false;
        }

        AlteracaoDeMundo existente = Obter(stateId, chave);

        if (existente != null)
        {
            if (existente.Valor == valor)
                return false;

            existente.Actualizar(valor);

            return true;
        }

        alteracoes.Add(new AlteracaoDeMundo(stateId, chave, valor));

        return true;
    }

    public bool Registar(string stateId, string chave, bool valor)
    {
        return Registar(stateId, chave, AlteracaoDeMundo.Escrever(valor));
    }

    public string Ler(string stateId, string chave, string omissao = null)
    {
        AlteracaoDeMundo alteracao = Obter(stateId, chave);

        return alteracao != null ? alteracao.Valor : omissao;
    }

    public bool LerBool(string stateId, string chave, bool omissao = false)
    {
        AlteracaoDeMundo alteracao = Obter(stateId, chave);

        return alteracao != null
            ? AlteracaoDeMundo.Ler(alteracao.Valor, omissao)
            : omissao;
    }

    // Esvazia o mundo. Para os testes e para o recomeço de jogo.
    public void Limpar()
    {
        alteracoes.Clear();
    }
}
