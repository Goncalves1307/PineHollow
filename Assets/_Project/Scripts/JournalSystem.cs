using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// O caderno: o que o jogador já sabe, por gavetas, mais a cadeia de progressão
// do GDD §17.
//
// A progressão deste jogo é este ecrã. Não há XP nem níveis — o que ele pode
// fazer a seguir é função do que já sabe, e é isto que lho mostra.
//
// Segue a receita das camadas do AGENTS.md, com uma diferença deliberada em
// relação ao álbum: este componente vive FORA do seu painel. O PhotoAlbumSystem
// vive dentro do dele, e por isso o Update dele não corre com o painel desligado
// e o Tab teve de ser lido noutro sítio — foi assim que o álbum passou semanas
// inalcançável. Aqui o J é lido por quem está sempre a correr, como o
// ReadingSystem já fazia.
public class JournalSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject painel;

    [SerializeField] private Transform listaDosSeparadores;
    [SerializeField] private GameObject modeloDoSeparador;

    [SerializeField] private Transform listaDasEntradas;
    [SerializeField] private GameObject modeloDaEntrada;

    [SerializeField] private TMP_Text detalhe;
    [SerializeField] private TMP_Text cadeiaTexto;

    [Header("Cores")]
    // Paleta do Pine_Hollow_Direccao_Artistica.md §7.2 — névoa para o que está
    // em repouso, tinta lascada para o que está escolhido. Nunca branco puro.
    [SerializeField] private Color corEmRepouso = new Color(0.659f, 0.698f, 0.710f);
    [SerializeField] private Color corEscolhida = new Color(0.784f, 0.769f, 0.722f);

    [Header("Progressão")]
    [SerializeField] private CadeiaDeConhecimento cadeia;

    [Header("State")]
    [SerializeField] private PlayerStateMachine stateMachine;

    public bool IsOpen { get; private set; }

    public CategoriaDeConhecimento Categoria { get; private set; } =
        CategoriaDeConhecimento.Pessoa;

    public Conhecimento Seleccionado { get; private set; }

    private readonly List<SeparadorDoJournal> separadores =
        new List<SeparadorDoJournal>();

    private readonly List<EntradaDoJournal> entradas =
        new List<EntradaDoJournal>();

    // Legível de fora para os testes poderem ver o que o ecrã mostra sem ter de
    // contar filhos de um Transform — e para não se repetir o erro de dar por
    // testado um ecrã que na verdade não desenhou nada.
    public IReadOnlyList<EntradaDoJournal> Entradas => entradas;
    public IReadOnlyList<SeparadorDoJournal> Separadores => separadores;

    private static readonly CategoriaDeConhecimento[] Gavetas =
    {
        CategoriaDeConhecimento.Pessoa,
        CategoriaDeConhecimento.Local,
        CategoriaDeConhecimento.Data,
        CategoriaDeConhecimento.Fotografia,
        CategoriaDeConhecimento.Documento,
        CategoriaDeConhecimento.Descoberta
    };

    private void Start()
    {
        if (painel != null)
            painel.SetActive(false);
    }

    private void Update()
    {
        if (IsOpen &&
            stateMachine != null &&
            stateMachine.ConsumeBack(PlayerState.ViewingJournal))
        {
            Fechar();

            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.jKey.wasPressedThisFrame)
        {
            Alternar();
        }
    }

    // Público pela mesma razão que o TogglePhotoAlbum: o J é lido no Update e o
    // Keyboard.current é null em EditMode. Um caminho que só corre em play mode
    // é um caminho sem teste, e foi assim que o álbum ficou morto.
    public void Alternar()
    {
        if (IsOpen)
        {
            // Só fecha se for ele o de cima. Com outra camada por cima, isto
            // fazia PopMode do meio da pilha.
            if (stateMachine != null &&
                !stateMachine.IsTopMode(PlayerState.ViewingJournal))
            {
                return;
            }

            Fechar();

            return;
        }

        // Só se abre com o player livre: o caderno solta o cursor, e abri-lo por
        // cima de uma inspecção deixava o objecto na mão por trás do painel.
        if (stateMachine != null && stateMachine.OpenModeCount > 0)
            return;

        Abrir();
    }

    public void Abrir()
    {
        if (IsOpen)
            return;

        // Sem painel não se empurra o modo — trancava o jogador num ecrã que
        // não existe, e só a saída de emergência da máquina de estados o
        // resgatava, com aviso no log.
        if (painel == null)
        {
            Debug.LogError(
                $"{name}: painel do caderno por ligar no inspector — " +
                "o J não abre nada.",
                this
            );

            return;
        }

        IsOpen = true;

        painel.SetActive(true);

        Reconstruir();

        if (stateMachine != null)
            stateMachine.PushMode(PlayerState.ViewingJournal);
    }

    public void Fechar()
    {
        if (!IsOpen)
            return;

        IsOpen = false;

        Seleccionado = null;

        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.ViewingJournal);

        if (painel != null)
            painel.SetActive(false);
    }

    public void Mostrar(CategoriaDeConhecimento categoria)
    {
        Categoria = categoria;

        Seleccionado = null;

        Reconstruir();
    }

    public void Seleccionar(Conhecimento conhecimento)
    {
        Seleccionado = conhecimento;

        DesenharEntradas();

        DesenharDetalhe();
    }

    public void Reconstruir()
    {
        DesenharSeparadores();

        DesenharEntradas();

        DesenharDetalhe();

        DesenharCadeia();
    }

    private void DesenharSeparadores()
    {
        if (listaDosSeparadores == null || modeloDoSeparador == null)
        {
            Debug.LogError(
                $"{name}: separadores do caderno por ligar no inspector — " +
                "o caderno abre sem gavetas.",
                this
            );

            return;
        }

        Limpar(separadores);

        foreach (CategoriaDeConhecimento gaveta in Gavetas)
        {
            GameObject linha = Instantiate(modeloDoSeparador, listaDosSeparadores);

            linha.SetActive(true);

            SeparadorDoJournal separador = linha.GetComponent<SeparadorDoJournal>();

            if (separador == null)
            {
                // Destruir e não só saltar — ver a nota igual no quadro.
                Remover(linha);

                Debug.LogError(
                    "SeparadorDoJournal não encontrado no modelo do separador."
                );

                continue;
            }

            separador.Configurar(
                this,
                gaveta,
                gaveta == Categoria ? corEscolhida : corEmRepouso
            );

            separadores.Add(separador);
        }
    }

    private void DesenharEntradas()
    {
        if (listaDasEntradas == null || modeloDaEntrada == null)
        {
            Debug.LogError(
                $"{name}: lista ou modelo da entrada por ligar no inspector — " +
                "o caderno abre vazio.",
                this
            );

            return;
        }

        Limpar(entradas);

        RegistoDeConhecimento registo = RegistoDeConhecimento.Instancia;

        foreach (Conhecimento conhecimento in registo.Da(Categoria))
        {
            GameObject linha = Instantiate(modeloDaEntrada, listaDasEntradas);

            linha.SetActive(true);

            EntradaDoJournal entrada = linha.GetComponent<EntradaDoJournal>();

            if (entrada == null)
            {
                Remover(linha);

                Debug.LogError(
                    "EntradaDoJournal não encontrado no modelo da entrada."
                );

                continue;
            }

            entrada.Configurar(
                this,
                conhecimento,
                conhecimento == Seleccionado ? corEscolhida : corEmRepouso
            );

            entradas.Add(entrada);
        }
    }

    private void DesenharDetalhe()
    {
        if (detalhe == null)
            return;

        if (Seleccionado != null)
        {
            detalhe.text = TextoDe(Seleccionado);

            return;
        }

        detalhe.text = entradas.Count > 0
            ? string.Empty
            : "Nada ainda.";
    }

    public static string TextoDe(Conhecimento conhecimento)
    {
        if (conhecimento == null)
            return string.Empty;

        StringBuilder texto = new StringBuilder();

        texto.Append(conhecimento.Titulo);

        if (!string.IsNullOrWhiteSpace(conhecimento.Texto))
        {
            texto.Append('\n');
            texto.Append('\n');
            texto.Append(conhecimento.Texto);
        }

        return texto.ToString();
    }

    private void DesenharCadeia()
    {
        if (cadeiaTexto == null)
            return;

        cadeiaTexto.text = TextoDaCadeia(cadeia, RegistoDeConhecimento.Instancia);
    }

    // A cadeia do GDD §17. Os elos por saber aparecem como um traço: o jogador
    // vê que há caminho pela frente sem lhe ser contado o que lá está — mostrar
    // a frase de um elo por acender era contar a história por antecipação.
    //
    // E não leva contador de «x de y». A task pede para evitar checklist, e um
    // número a dizer quanto falta é exactamente isso.
    public static string TextoDaCadeia(
        CadeiaDeConhecimento cadeia,
        RegistoDeConhecimento registo)
    {
        if (cadeia == null || cadeia.Total == 0)
            return string.Empty;

        StringBuilder texto = new StringBuilder();

        foreach (EloDaCadeia elo in cadeia.Elos)
        {
            if (elo == null)
                continue;

            if (texto.Length > 0)
                texto.Append('\n');

            texto.Append(cadeia.Sabido(elo, registo) ? elo.Texto : "—");
        }

        return texto.ToString();
    }

    private static void Limpar<T>(List<T> linhas) where T : Component
    {
        foreach (T linha in linhas)
        {
            if (linha != null)
                Remover(linha.gameObject);
        }

        linhas.Clear();
    }

    // Destroy() em EditMode só acontece no fim do frame, e num teste isso deixa
    // as linhas antigas penduradas enquanto se conta as novas. Em jogo é
    // Destroy() como em todo o lado — mas desligado primeiro: o Destroy de play
    // mode também é adiado até ao fim do frame, e até lá o objecto continua
    // activo a ocupar o seu lugar no layout, ao lado do que o substitui.
    private static void Remover(GameObject alvo)
    {
        if (alvo == null)
            return;

        alvo.SetActive(false);

        if (Application.isPlaying)
            Destroy(alvo);
        else
            DestroyImmediate(alvo);
    }
}
