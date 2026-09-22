using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Entra em play mode, faz o ciclo do flashback e fotografa cada estado.
//
// Mesma razão que o Fase5Capturas: «os testes passam» e «o ecrã lê-se» são duas
// afirmações diferentes, e a segunda não se prova com a primeira. Uma suite
// verde não diz se a transição tapa mesmo o ecrã, se 1986 tem outra luz, nem se
// o Thomas aparece — e essas três coisas são a fase inteira.
//
// Não dispara o alinhamento a fingir: põe o sinalizador do
// PhotoComparisonSystem a true e deixa o SistemaDeFlashback.Update consumi-lo
// como consome em jogo. O que se captura é o caminho real.
//
// Corre COM janela (não leva -nographics: sem render não há o que capturar):
//   Unity -projectPath . -executeMethod Fase7Capturas.Correr \
//         -capturasPara /caminho/para/a/pasta -logFile -
public static class Fase7Capturas
{
    private const string Chave = "fase7-capturas-passo";
    private const string ChaveDaPasta = "fase7-capturas-pasta";

    private const string CaminhoDaCena =
        "Assets/_Project/Scenes/Prototype_Player.unity";

    // Frames entre passos. O primeiro tem de ser generoso: a cena arranca, os
    // Start() correm e o URP tem de desenhar um frame antes de haver imagem.
    private const int FramesPorPasso = 45;

    // Um passo curto para apanhar a transição a meio — com 45 frames ela já
    // tinha acabado e a captura do véu saía igual à de 1986.
    private const int FramesDaTransicao = 12;

    private const string ChaveDoMenu = "fase7-capturas-por-menu";

    private static int frames;

    // Pelo menu, com o editor já aberto. É a forma que funciona quando o
    // arranque do Unity em modo gráfico a partir de uma shell não pega — e não
    // pega em todas as máquinas, o que deixava esta fase sem verificação
    // visual nenhuma.
    //
    // Guarda no SessionState que veio daqui, para o Terminar NÃO fechar o
    // editor por baixo de quem carregou no menu. O SessionState e não um campo
    // estático porque entrar em play mode pode levar domain reload, e aí o
    // campo perdia-se e o editor fechava-se na mesma.
    [MenuItem("Pine Hollow/FASE 7 — capturar o flashback")]
    public static void CorrerPeloMenu()
    {
        SessionState.SetBool(ChaveDoMenu, true);

        Correr();
    }

    public static void Correr()
    {
        string pasta = Pasta();

        Directory.CreateDirectory(pasta);

        SessionState.SetString(ChaveDaPasta, pasta);
        SessionState.SetInt(Chave, 0);

        EditorSceneManager.OpenScene(CaminhoDaCena, OpenSceneMode.Single);

        MaximizarAVistaDeJogo();

        // Registar AQUI e não só no InitializeOnLoadMethod: este projecto entra
        // em play mode sem domain reload, e nessa transição o
        // InitializeOnLoadMethod não corre. Sem isto a corrida ficava parada em
        // play mode sem capturar nada.
        EditorApplication.update -= Avancar;
        EditorApplication.update += Avancar;

        EditorApplication.EnterPlaymode();
    }

    // A captura é do Game view, e o Game view é do tamanho da janela. A
    // primeira corrida da FASE 5 saiu com 465 px e a legenda ficou cortada dos
    // dois lados.
    private static void MaximizarAVistaDeJogo()
    {
        System.Type tipo = typeof(Editor).Assembly.GetType("UnityEditor.GameView");

        if (tipo == null)
            return;

        EditorWindow janela = EditorWindow.GetWindow(tipo, false, "Game", true);

        if (janela == null)
            return;

        janela.position = new Rect(0f, 0f, 1600f, 900f);
        janela.maximized = true;

        janela.Focus();
    }

    [InitializeOnLoadMethod]
    private static void Retomar()
    {
        if (SessionState.GetInt(Chave, -1) < 0)
            return;

        EditorApplication.update -= Avancar;
        EditorApplication.update += Avancar;
    }

    private static void Avancar()
    {
        if (!EditorApplication.isPlaying)
            return;

        frames++;

        int passo = SessionState.GetInt(Chave, 0);

        // Os passos que apanham a transição são curtos: ela dura menos de um
        // segundo e com 45 frames já tinha passado.
        int espera = (passo == 2 || passo == 3) ? FramesDaTransicao : FramesPorPasso;

        if (frames < espera)
            return;

        frames = 0;

        SessionState.SetInt(Chave, passo + 1);

        Executar(passo);
    }

    private static void Executar(int passo)
    {
        string pasta = SessionState.GetString(ChaveDaPasta, Pasta());

        SistemaDeFlashback sistema = Object.FindAnyObjectByType<SistemaDeFlashback>(
            FindObjectsInactive.Include
        );

        PhotoComparisonSystem comparacao = Object.FindAnyObjectByType<PhotoComparisonSystem>(
            FindObjectsInactive.Include
        );

        PersonagemHistorica thomas = Object.FindAnyObjectByType<PersonagemHistorica>(
            FindObjectsInactive.Include
        );

        switch (passo)
        {
            case 0:
                Diagnosticar(sistema, thomas, "antes");
                Capturar(pasta, "1-2026");
                break;

            case 1:
                // O caminho real: põe-se o sinalizador do alinhamento e é o
                // Update do SistemaDeFlashback que o consome, como em jogo.
                if (comparacao == null)
                {
                    Debug.LogError("CAPTURAS: não há PhotoComparisonSystem na cena.");

                    Terminar();

                    return;
                }

                Alinhar(comparacao);
                break;

            case 2:
                Diagnosticar(sistema, thomas, "a-entrar");
                Capturar(pasta, "2-transicao");
                break;

            case 3:
                Capturar(pasta, "3-transicao-tardia");
                break;

            case 4:
                Diagnosticar(sistema, thomas, "em-1986");
                Capturar(pasta, "4-1986");
                break;

            case 5:
                // Faz o Thomas andar o caminho todo de uma vez. Ele anda a 1,4
                // m/s e os pontos estão a metros: esperar por ele em tempo real
                // eram treze segundos de captura parada.
                if (thomas != null)
                {
                    for (int i = 0; i < 10 && !thomas.SaiuDeVista; i++)
                        thomas.Avancar(10f);
                }

                break;

            case 6:
                Diagnosticar(sistema, thomas, "a-sair");
                Capturar(pasta, "5-regresso");
                break;

            case 7:
                Diagnosticar(sistema, thomas, "depois");
                Capturar(pasta, "6-2026-de-volta");
                break;

            default:
                Terminar();
                break;
        }
    }

    private static void Alinhar(PhotoComparisonSystem comparacao)
    {
        FieldInfo campo = typeof(PhotoComparisonSystem).GetField(
            "alinhamentoPorConsumir",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (campo == null)
        {
            Debug.LogError(
                "CAPTURAS: o campo alinhamentoPorConsumir mudou de nome — " +
                "a captura deixou de disparar o flashback."
            );

            return;
        }

        campo.SetValue(comparacao, true);

        Debug.Log("CAPTURAS: alinhamento marcado; o SistemaDeFlashback tem de o consumir.");
    }

    // Escreve no log o que a captura devia mostrar. Uma imagem preta pode ser
    // a transição a funcionar ou o jogo a não desenhar — sem isto não se
    // distingue uma da outra a olhar para o PNG.
    private static void Diagnosticar(
        SistemaDeFlashback sistema,
        PersonagemHistorica thomas,
        string momento)
    {
        string fase = sistema != null ? sistema.Fase.ToString() : "SEM SISTEMA";
        string epoca = EstadoDoMundo.Instancia.EpocaActual.ToString();

        string quemAnda = thomas == null
            ? "sem Thomas na cena"
            : "Thomas activo=" + thomas.gameObject.activeInHierarchy +
              " saiuDeVista=" + thomas.SaiuDeVista;

        Debug.Log(
            $"CAPTURAS [{momento}]: fase={fase} epoca={epoca} {quemAnda}"
        );
    }

    private static void Capturar(string pasta, string nome)
    {
        string caminho = Path.Combine(pasta, nome + ".png");

        ScreenCapture.CaptureScreenshot(caminho);

        Debug.Log($"CAPTURAS: {caminho}");
    }

    private static void Terminar()
    {
        bool porMenu = SessionState.GetBool(ChaveDoMenu, false);

        SessionState.EraseInt(Chave);
        SessionState.EraseBool(ChaveDoMenu);

        EditorApplication.update -= Avancar;
        EditorApplication.ExitPlaymode();

        Debug.Log(
            "CAPTURAS: terminado. As imagens estão em " +
            SessionState.GetString(ChaveDaPasta, Pasta())
        );

        // Quem veio do menu fica com o editor aberto: fechá-lo por baixo de
        // alguém que carregou num botão é das poucas coisas que um script de
        // editor não pode fazer.
        if (!porMenu)
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }

    // A pasta vem da linha de comandos. As capturas nunca vão para dentro do
    // checkout: são conteúdo de uma corrida, não do projecto.
    private static string Pasta()
    {
        string[] args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-capturasPara")
                return args[i + 1];
        }

        return Path.Combine(Path.GetTempPath(), "pine-hollow-capturas-fase7");
    }
}
