using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Entra em play mode, percorre o fluxo da FASE 5 e fotografa cada ecrã.
//
// Existe porque «os testes passam» e «o ecrã lê-se» são duas afirmações
// diferentes, e a segunda não se prova com a primeira. O álbum desta fase
// esteve escrito, ligado e completamente inalcançável durante semanas sem que
// nenhum teste se queixasse — o que o teria apanhado era alguém olhar para o
// ecrã.
//
// Corre com janela (não leva -nographics: sem render não há o que capturar):
//   Unity -projectPath . -executeMethod Fase5Capturas.Correr \
//         -capturasPara /caminho/para/a/pasta -logFile -
public static class Fase5Capturas
{
    private const string Chave = "fase5-capturas-passo";
    private const string ChaveDaPasta = "fase5-capturas-pasta";

    private const string CaminhoDaCena =
        "Assets/_Project/Scenes/Prototype_Player.unity";

    // Frames entre passos. O primeiro tem de ser generoso: a cena tem de
    // arrancar, os Start() têm de correr e o URP tem de desenhar um frame
    // antes de haver imagem nenhuma para gravar.
    private const int FramesPorPasso = 45;

    private static int frames;

    public static void Correr()
    {
        string pasta = Pasta();

        Directory.CreateDirectory(pasta);

        SessionState.SetString(ChaveDaPasta, pasta);
        SessionState.SetInt(Chave, 0);

        EditorSceneManager.OpenScene(CaminhoDaCena, OpenSceneMode.Single);

        MaximizarAVistaDeJogo();

        // Registar AQUI e não só no InitializeOnLoadMethod. Este projecto tem
        // as Enter Play Mode Options ligadas com o reload desactivado — o log
        // diz «Entering Playmode with Reload Scene disabled» — por isso não há
        // domain reload ao entrar em play mode e o InitializeOnLoadMethod
        // nunca corre. Sem isto o contador não era registado e a corrida ficava
        // parada em play mode sem capturar nada.
        EditorApplication.update -= Avancar;
        EditorApplication.update += Avancar;

        EditorApplication.EnterPlaymode();
    }

    // A primeira corrida saiu com 465 px de largura e a legenda da comparação
    // ficou cortada dos dois lados. A captura é do Game view, e o Game view é
    // do tamanho da janela — maximizá-la é a diferença entre ver o ecrã e ver
    // uma tira dele.
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

    // Largar o que está na mão. O Esc não se simula só com RequestBack: a
    // PlayerStateMachine corre a -100 e o BeginFrame dela, no frame seguinte,
    // apaga o pedido antes de o consumidor o ver. Isto pede e entrega no mesmo
    // instante, que é o que os testes também fazem.
    private static void Largar(PlayerStateMachine maquina, MonoBehaviour consumidor)
    {
        if (maquina == null || consumidor == null)
            return;

        maquina.BeginFrame();
        maquina.RequestBack();

        MethodInfo update = consumidor.GetType().GetMethod(
            "Update",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (update != null)
            update.Invoke(consumidor, null);
    }

    // Para o caso de HAVER domain reload: aí os delegates não sobrevivem, e
    // o SessionState é que diz que havia uma corrida a meio.
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

        if (frames < FramesPorPasso)
            return;

        frames = 0;

        int passo = SessionState.GetInt(Chave, 0);

        SessionState.SetInt(Chave, passo + 1);

        Executar(passo);
    }

    private static void Executar(int passo)
    {
        string pasta = SessionState.GetString(ChaveDaPasta, Pasta());

        PhotographySystem fotografia = Object.FindAnyObjectByType<PhotographySystem>();
        InspectionSystem inspeccao = Object.FindAnyObjectByType<InspectionSystem>();
        PhotoAlbumSystem album = Object.FindAnyObjectByType<PhotoAlbumSystem>(
            FindObjectsInactive.Include
        );
        PhotoComparisonSystem comparacao = Object.FindAnyObjectByType<PhotoComparisonSystem>(
            FindObjectsInactive.Include
        );
        PlayerStateMachine maquina = Object.FindAnyObjectByType<PlayerStateMachine>();

        switch (passo)
        {
            case 0:
                Capturar(pasta, "1-jogo");
                break;

            case 1:
                // Apanhar a fotografia de 1986: vai para a mão e para o álbum.
                Apanhar("Fotografia_Estudio_1986");
                break;

            case 2:
                Diagnosticar("Fotografia_Estudio_1986");
                Capturar(pasta, "2-fotografia-na-mao");
                break;

            case 3:
                if (inspeccao != null)
                    inspeccao.Virar();
                break;

            case 4:
                Capturar(pasta, "3-verso");
                break;

            case 5:
                // Largar e apanhar a segunda.
                Largar(maquina, inspeccao);
                break;

            case 6:
                Apanhar("Fotografia_Estudio_1994");
                break;

            case 7:
                Largar(maquina, inspeccao);
                break;

            case 8:
                // Se a pilha não ficou vazia, o Tab não abre — e é assim que
                // tem de ser. Uma captura tirada a contornar as guardas do
                // jogo mostra um ecrã que o jogador nunca alcança.
                if (maquina != null && maquina.OpenModeCount > 0)
                {
                    Debug.LogError(
                        "CAPTURAS: ainda há " + maquina.OpenModeCount +
                        " camada(s) abertas — largar não funcionou."
                    );
                }

                if (fotografia != null)
                    fotografia.TogglePhotoAlbum();
                break;

            case 9:
                Capturar(pasta, "4-album");
                break;

            case 10:
                if (album != null && !album.IsOpen)
                {
                    Debug.LogError("CAPTURAS: o álbum não abriu — não há o que comparar.");
                    break;
                }

                // Marcar as duas para comparar — é o clique direito nas
                // miniaturas, sem passar pelo rato.
                if (album != null && fotografia != null)
                {
                    foreach (PhotoData foto in fotografia.CapturedPhotos)
                        album.AlternarSeleccao(foto);
                }
                break;

            case 11:
                Capturar(pasta, "5-comparacao-lado-a-lado");
                break;

            case 12:
                // Sobrepor e capturar NO MESMO tick: o clarão dura 0.45 s e
                // um passo inteiro são ~0.75 s — esperar pelo passo seguinte
                // fotografava o ecrã já sem ele.
                if (comparacao != null)
                {
                    comparacao.AlternarModo();

                    Capturar(pasta, "6-alinhadas-com-clarao");
                }
                break;

            case 13:
                Capturar(pasta, "7-sobrepostas");
                break;

            case 14:
                Relatar(comparacao);
                break;

            default:
                Terminar();
                break;
        }
    }

    private static void Apanhar(string nome)
    {
        GameObject go = GameObject.Find(nome);

        if (go == null)
        {
            Debug.LogError($"CAPTURAS: {nome} não está na cena.");
            return;
        }

        PhotoInteractable interactable = go.GetComponent<PhotoInteractable>();

        if (interactable == null)
        {
            Debug.LogError($"CAPTURAS: {nome} não tem PhotoInteractable.");
            return;
        }

        interactable.Interact();
    }

    // Onde está de facto o objecto que devia estar na mão. A captura mostrou
    // o ecrã sem fotografia nenhuma e adivinhar porquê não é diagnóstico.
    private static void Diagnosticar(string nome)
    {
        GameObject go = GameObject.Find(nome);

        if (go == null)
        {
            Debug.LogError($"CAPTURAS: {nome} desapareceu da cena.");
            return;
        }

        Renderer renderer = go.GetComponent<Renderer>();
        Camera camara = Camera.main;

        Vector3 local = camara != null
            ? camara.transform.InverseTransformPoint(go.transform.position)
            : Vector3.zero;

        Debug.Log(
            $"CAPTURAS: {nome} pai={(go.transform.parent != null ? go.transform.parent.name : "<nenhum>")} " +
            $"mundo={go.transform.position} localAcamara={local} " +
            $"escala={go.transform.lossyScale} layer={LayerMask.LayerToName(go.layer)} " +
            $"activo={go.activeInHierarchy} visivel={(renderer != null && renderer.isVisible)} " +
            $"cullingMask={(camara != null ? camara.cullingMask : 0)}"
        );
    }

    private static void Relatar(PhotoComparisonSystem comparacao)
    {
        if (comparacao == null)
        {
            Debug.LogError("CAPTURAS: não há comparação na cena.");
            return;
        }

        Debug.Log(
            $"CAPTURAS: modo={comparacao.Modo} alinhada={comparacao.Alinhada} " +
            $"clarao={comparacao.IntensidadeDoFlash:F2} " +
            $"descobertas={comparacao.DescobertasReveladas.Count}"
        );

        foreach (PhotoDiscovery descoberta in comparacao.DescobertasReveladas)
            Debug.Log($"CAPTURAS: descoberta {descoberta.Tipo} — {descoberta.Descricao}");
    }

    private static void Capturar(string pasta, string nome)
    {
        string caminho = Path.Combine(pasta, nome + ".png");

        ScreenCapture.CaptureScreenshot(caminho);

        Debug.Log($"CAPTURAS: {caminho}");
    }

    private static void Terminar()
    {
        SessionState.EraseInt(Chave);

        EditorApplication.update -= Avancar;
        EditorApplication.ExitPlaymode();

        EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }

    // A pasta vem da linha de comandos. As capturas nunca vão para dentro do
    // checkout: é conteúdo de uma corrida, não do projecto.
    private static string Pasta()
    {
        string[] args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-capturasPara")
                return args[i + 1];
        }

        return Path.Combine(Path.GetTempPath(), "pine-hollow-capturas");
    }
}
