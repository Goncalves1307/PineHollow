using UnityEngine;

// Alguém que só existe no passado, a andar o seu caminho.
//
// O Thomas junto ao lago é o primeiro: o jogador chega a 1986, vê-o, pode
// segui-lo, e o flashback acaba quando ele sai de vista (GDD §15.1 «The
// Photograph»). Não fala, não espera pelo jogador e não lhe dá nada — o que ele
// faz é existir, e é isso que diz ao jogador onde está.
//
// Não é um sistema de NPCs. As rotinas, os horários e as conversas são a FASE
// 12; isto é o mecanismo mínimo para um flashback ter gente dentro, e os quatro
// degraus do NivelDeConsciencia para que a FASE 12 tenha onde os pendurar.
//
// Anda por Avancar(delta) e não pelo Update directamente, pela regra da casa:
// em EditMode o Time.deltaTime é zero e uma personagem que só ande em play mode
// é uma personagem sem teste.
[RequireComponent(typeof(ObjectoDeEpoca))]
public class PersonagemHistorica : MonoBehaviour
{
    [Header("Percurso")]
    [Tooltip("Por onde passa, por ordem. Ao chegar ao último, sai de vista.")]
    [SerializeField] private Transform[] percurso;

    [SerializeField] private float velocidade = 1.4f;

    [Tooltip("A que distância de um ponto se considera que lá chegou.")]
    [SerializeField] private float tolerancia = 0.15f;

    [Header("Consciência")]
    [SerializeField] private NivelDeConsciencia consciencia = NivelDeConsciencia.Inconsciente;

    [SerializeField] private Transform jogador;

    [Tooltip("A que distância a personagem repara no jogador, se o nível o permitir.")]
    [SerializeField] private float distanciaQueNota = 4f;

    [Header("Marca no presente")]
    [Tooltip("Só usado no nível MarcaOPresente: o que esta personagem deixa feito.")]
    [SerializeField] private string stateIdDaMarca;
    [SerializeField] private string chaveDaMarca = "alterado";

    private int destino;
    private bool saiuDeVista;
    private bool marcou;

    public NivelDeConsciencia Consciencia => consciencia;

    public bool SaiuDeVista => saiuDeVista;

    public int Destino => destino;

    // True quando o nível o permite e o jogador está perto. É o que a FASE 12
    // vai usar para decidir uma fala, um olhar ou uma fuga.
    public bool NotouOJogador { get; private set; }

    public bool Marcou => marcou;

    private void Update()
    {
        Avancar(Time.deltaTime);
    }

    // Um passo do caminho. Devolve true no frame em que sai de vista — quem
    // conduz o flashback usa isso para o terminar.
    //
    // Consome no máximo UM ponto do percurso por chamada, mesmo que o delta
    // desse para atravessar dois. Em jogo não se nota — são sessenta chamadas
    // por segundo e os pontos estão a metros uns dos outros —, e a alternativa
    // era um ciclo interno que, com um ponto repetido ou um delta grande vindo
    // de um frame perdido, teleportava a personagem até ao fim do caminho à
    // frente do jogador. Quem escrever testes que a façam andar tem de lhe dar
    // frames, não um delta grande.
    public bool Avancar(float delta)
    {
        if (saiuDeVista)
            return false;

        Reparar();

        // Quem reage ou vê o jogador pára enquanto ele estiver por perto. É a
        // diferença entre uma personagem que anda um trilho e uma que está ali:
        // parar é a coisa mais barata que existe e é a que se lê como atenção.
        if (NotouOJogador && consciencia != NivelDeConsciencia.Inconsciente)
        {
            Encarar();

            return false;
        }

        if (percurso == null || percurso.Length == 0)
        {
            // Sem percurso esta personagem nunca sai de vista, e o flashback
            // que espere por ela nunca acaba: o jogador fica preso em 1986 sem
            // Esc que valha, porque o modo Flashback não se fecha no Esc.
            // Queixa-se e sai de vista já — um flashback curto de mais é um
            // defeito visível, um jogo trancado é uma sessão perdida.
            Debug.LogError(
                "PersonagemHistorica '" + name + "' não tem percurso ligado: " +
                "sai de vista de imediato para não trancar o flashback.",
                this
            );

            return SairDeVista();
        }

        if (destino >= percurso.Length)
            return SairDeVista();

        Transform ponto = percurso[destino];

        if (ponto == null)
        {
            // Um ponto por ligar não pára o percurso inteiro: salta-se e
            // continua. Ficar preso num nulo era o flashback a nunca acabar, e
            // o jogador sem forma de sair de 1986 a não ser fechar o jogo.
            Debug.LogWarning(
                "Percurso de '" + name + "' tem um ponto por ligar no índice " +
                destino + ": saltado.",
                this
            );

            destino++;

            return false;
        }

        if (ponto.IsChildOf(transform))
        {
            // Um ponto pendurado na própria personagem NUNCA é alcançado: ele
            // anda com ela, a distância fica congelada e, com o Olhar a rodá-la,
            // o offset roda também — ela dá voltas para sempre. O flashback não
            // acabava, e do modo Flashback não se sai pelo Esc: sessão perdida,
            // sem uma linha no log.
            //
            // Foi exactamente o que a montagem da FASE 7 gerou à primeira, e
            // nenhum teste o apanhou porque os testes constroem o percurso na
            // raiz. Salta-se o ponto — um caminho curto de mais vê-se, um jogo
            // trancado não.
            Debug.LogError(
                "Percurso de '" + name + "' tem no índice " + destino +
                " um ponto ('" + ponto.name + "') que é descendente da própria " +
                "personagem: ela nunca lá chegaria, porque o ponto anda com ela. " +
                "Saltado — os pontos do percurso vivem fora da personagem.",
                this
            );

            destino++;

            return false;
        }

        Vector3 alvo = ponto.position;

        transform.position = Vector3.MoveTowards(
            transform.position,
            alvo,
            velocidade * Mathf.Max(0f, delta)
        );

        Olhar(alvo);

        if (Vector3.Distance(transform.position, alvo) <= tolerancia)
        {
            destino++;

            if (destino >= percurso.Length)
                return SairDeVista();
        }

        return false;
    }

    private void Reparar()
    {
        // O degrau mais baixo não repara em nada, e é o que esta fase autora.
        if (consciencia == NivelDeConsciencia.Inconsciente || jogador == null)
        {
            NotouOJogador = false;

            return;
        }

        NotouOJogador =
            Vector3.Distance(transform.position, jogador.position) <= distanciaQueNota;
    }

    private void Encarar()
    {
        // Reage é sentir a direcção; Ve é sustentar o olhar. A diferença entre
        // os dois degraus é esta linha, e é de propósito que ela é pequena — o
        // salto que o jogador sente não é de código, é de o passado deixar de
        // ser um filme.
        if (consciencia == NivelDeConsciencia.Ve ||
            consciencia == NivelDeConsciencia.MarcaOPresente)
        {
            Olhar(jogador.position);
        }
    }

    private void Olhar(Vector3 alvo)
    {
        Vector3 direccao = alvo - transform.position;

        // Só no plano: uma personagem que se incline para olhar o chão ou o
        // céu deixa de parecer uma pessoa.
        direccao.y = 0f;

        if (direccao.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direccao);
    }

    private bool SairDeVista()
    {
        saiuDeVista = true;

        Marcar();

        return true;
    }

    // O quarto degrau: o que se fez em 1986 fica escrito e o presente lê-o.
    public void Marcar()
    {
        if (consciencia != NivelDeConsciencia.MarcaOPresente || marcou)
            return;

        if (string.IsNullOrWhiteSpace(stateIdDaMarca))
        {
            Debug.LogError(
                "PersonagemHistorica '" + name + "' está em MarcaOPresente sem " +
                "stateIdDaMarca: o que ela fizer não chega a 2026.",
                this
            );

            return;
        }

        EstadoDoMundo.Instancia.Registar(stateIdDaMarca, chaveDaMarca, true);

        marcou = true;
    }

    // Volta ao princípio do percurso. O flashback pode ser revisto, e uma
    // personagem que ficasse no fim do trilho estaria ausente da segunda vez.
    public void Recomecar()
    {
        destino = 0;
        saiuDeVista = false;
        NotouOJogador = false;

        if (percurso != null && percurso.Length > 0 && percurso[0] != null)
            transform.position = percurso[0].position;
    }
}
