using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Monta na cena o que a FASE 6 acrescentou: o registo de conhecimento no
// Bootstrap, a cadeia de progressão do GDD §17 como asset, e os dois ecrãs —
// o caderno e o quadro de investigação.
//
// Mesmas razões do Fase5Montagem: a alternativa era editar YAML à mão numa cena
// de 3.900 linhas, onde um GUID mal copiado parte todas as referências de um
// script sem dar erro nenhum. Isto é reprodutível, corre duas vezes sem duplicar
// nada, e actualiza o conteúdo de um asset que já exista em vez de sair cedo.
//
// Corre pelo menu ou em batchmode:
//   Unity -batchmode -nographics -quit -projectPath . \
//         -executeMethod Fase6Montagem.Correr -logFile -
public static class Fase6Montagem
{
    // Paleta do Pine_Hollow_Direccao_Artistica.md §7.2, como no Fase5Montagem.
    // Nunca branco puro.
    private static readonly Color AsfaltoHumido = De(0x33, 0x36, 0x3A);
    private static readonly Color TintaLascada = De(0xC8, 0xC4, 0xB8);
    private static readonly Color Nevoa = De(0xA8, 0xB2, 0xB5);
    private static readonly Color BetaoManchado = De(0x7A, 0x7B, 0x76);

    private static Color De(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    private const string CaminhoDoBootstrap =
        "Assets/_Project/Scenes/Bootstrap.unity";

    private const string CaminhoDaCena =
        "Assets/_Project/Scenes/Prototype_Player.unity";

    private const string PastaDaInvestigacao =
        "Assets/_Project/Investigation";

    private const string CaminhoDaCadeia =
        PastaDaInvestigacao + "/CadeiaDeConhecimento.asset";

    private static int mudancas;

    [MenuItem("Pine Hollow/FASE 6 — montar a investigação")]
    public static void Correr()
    {
        mudancas = 0;

        // Isto abre cenas em Single, o que descarrega a que estiver aberta sem
        // perguntar. Corrido pelo menu com alterações por gravar, deitava-as
        // fora numa fracção de segundo. Em batchmode não há ninguém a quem
        // perguntar e o diálogo não aparece.
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("FASE 6: montagem cancelada — havia cenas por gravar.");

            return;
        }

        GarantirCadeia();

        MontarORegisto();

        Scene cena = EditorSceneManager.OpenScene(CaminhoDaCena, OpenSceneMode.Single);

        // O asset é RECARREGADO aqui, e não trazido de cima numa variável.
        // Abrir uma cena em Single descarrega os assets que mais ninguém
        // segura: a referência que o GarantirCadeia devolveu chega aqui como
        // um null disfarçado, e o Ligar escrevia null por cima de null sem se
        // queixar de nada. O ecrã ficava montado e a cadeia por ligar — e só o
        // PrototypePlayerSceneTests deu por isso.
        CadeiaDeConhecimento cadeia =
            AssetDatabase.LoadAssetAtPath<CadeiaDeConhecimento>(CaminhoDaCadeia);

        if (cadeia == null)
        {
            Falhar("A cadeia de progressão não está em " + CaminhoDaCadeia + ".");

            return;
        }

        PhotoViewerSystem viewer = Object.FindAnyObjectByType<PhotoViewerSystem>(
            FindObjectsInactive.Include
        );

        if (viewer == null)
        {
            Falhar("Não há PhotoViewerSystem na cena: sem ele não sei onde " +
                   "pendurar os ecrãs da investigação.");

            return;
        }

        GameObject anfitriao = GarantirAnfitriao(viewer);

        if (anfitriao == null)
            return;

        MontarCaderno(anfitriao, cadeia);
        MontarQuadro(anfitriao);

        if (mudancas > 0)
        {
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"FASE 6: investigação montada, {mudancas} alterações gravadas.");
        }
        else
        {
            Debug.Log("FASE 6: já estava montada, nada a fazer.");
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    // ---------------------------------------------------------------
    // A cadeia de progressão do GDD §17
    // ---------------------------------------------------------------

    // As dez frases são canon e vão à letra. A task cortou a lista a meio —
    // ficou-se nos «outros casos» — e quem só leia a task fica sem saber que a
    // cadeia acaba no protagonista a fazer parte do fenómeno.
    //
    // Os ids que cada elo exige são um CONTRATO com as fases que vêm depois: a
    // FASE 11 autora as descobertas do Ethan e é ela que tem de produzir estes
    // ids para os elos do meio acenderem. Hoje acendem três — o Thomas, a
    // fotografia de 1994 e a descoberta que sai do alinhamento — e os outros
    // sete ficam a traço, que é o estado honesto de quem ainda não tem conteúdo.
    private static List<EloDaCadeia> ElosDoGdd()
    {
        return new List<EloDaCadeia>
        {
            new EloDaCadeia(
                "Thomas",
                Conhecimento.Identificar(CategoriaDeConhecimento.Pessoa, "Thomas Hale")),

            new EloDaCadeia(
                "Thomas morreu em 1986",
                Conhecimento.Identificar(
                    CategoriaDeConhecimento.Documento, "obituario-thomas-hale")),

            new EloDaCadeia(
                "Thomas aparece em 1994",
                Conhecimento.Identificar(
                    CategoriaDeConhecimento.Fotografia, "Fotografia_Estudio_1994")),

            new EloDaCadeia(
                "Ethan investigou Thomas",
                Conhecimento.Identificar(CategoriaDeConhecimento.Pessoa, "Ethan Cole")),

            new EloDaCadeia(
                "Existem outros casos",
                Conhecimento.Identificar(
                    CategoriaDeConhecimento.Descoberta, "outros-casos")),

            new EloDaCadeia(
                "A fábrica possui túneis",
                Conhecimento.Identificar(
                    CategoriaDeConhecimento.Local, "Túneis da fábrica")),

            new EloDaCadeia(
                "Existe uma câmara",
                Conhecimento.Identificar(
                    CategoriaDeConhecimento.Local, "Câmara subterrânea")),

            new EloDaCadeia(
                "As fotografias ligam diferentes épocas",
                Conhecimento.Identificar(
                    CategoriaDeConhecimento.Descoberta,
                    "Fotografia_Estudio_1986",
                    "porta-lateral")),

            new EloDaCadeia(
                "O protagonista aparece nas fotografias",
                Conhecimento.Identificar(
                    CategoriaDeConhecimento.Descoberta, "protagonista-nas-fotografias")),

            new EloDaCadeia(
                "O protagonista faz parte do fenómeno",
                Conhecimento.Identificar(
                    CategoriaDeConhecimento.Descoberta, "protagonista-no-fenomeno"))
        };
    }

    private static void GarantirCadeia()
    {
        if (!AssetDatabase.IsValidFolder(PastaDaInvestigacao))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Investigation");

            mudancas++;
        }

        CadeiaDeConhecimento cadeia =
            AssetDatabase.LoadAssetAtPath<CadeiaDeConhecimento>(CaminhoDaCadeia);

        if (cadeia == null)
        {
            cadeia = ScriptableObject.CreateInstance<CadeiaDeConhecimento>();

            AssetDatabase.CreateAsset(cadeia, CaminhoDaCadeia);

            mudancas++;
        }

        List<EloDaCadeia> elos = ElosDoGdd();

        // E aqui NÃO se sai cedo por o asset já existir. O GarantirFotografia da
        // FASE 5 faz isso e o resultado é que mudar o conteúdo no script não
        // actualiza o asset que está em disco — corre-se a montagem, diz que
        // está tudo bem, e o jogo continua com o texto antigo.
        if (!Iguais(cadeia.Elos, elos))
        {
            cadeia.Definir(elos);

            EditorUtility.SetDirty(cadeia);

            AssetDatabase.SaveAssets();

            Debug.Log($"FASE 6: cadeia de progressão escrita com {elos.Count} elos.");

            mudancas++;
        }
    }

    private static bool Iguais(IReadOnlyList<EloDaCadeia> a, List<EloDaCadeia> b)
    {
        if (a == null || a.Count != b.Count)
            return false;

        for (int i = 0; i < b.Count; i++)
        {
            if (a[i] == null ||
                a[i].Texto != b[i].Texto ||
                a[i].ExigeId != b[i].ExigeId)
            {
                return false;
            }
        }

        return true;
    }

    // ---------------------------------------------------------------
    // O registo, no Bootstrap
    // ---------------------------------------------------------------

    // Vai para a cena do Bootstrap e não para a do jogo: é o objecto que
    // sobrevive às trocas de cena, e a FASE 7 troca a cena por baixo do jogador
    // a cada flashback. Um registo na cena de jogo perdia tudo à primeira
    // viagem a 1986.
    private static void MontarORegisto()
    {
        Scene bootstrap = EditorSceneManager.OpenScene(
            CaminhoDoBootstrap,
            OpenSceneMode.Single
        );

        BootstrapLoader carregador = Object.FindAnyObjectByType<BootstrapLoader>(
            FindObjectsInactive.Include
        );

        if (carregador == null)
        {
            Debug.LogWarning(
                "FASE 6: não há BootstrapLoader na cena do Bootstrap — o " +
                "registo de conhecimento não foi montado lá. Em jogo ele " +
                "cria-se sozinho, mas não sobrevive à troca de cena."
            );

            return;
        }

        if (carregador.GetComponent<RegistoDeConhecimento>() != null)
            return;

        Undo.AddComponent<RegistoDeConhecimento>(carregador.gameObject);

        EditorSceneManager.MarkSceneDirty(bootstrap);
        EditorSceneManager.SaveScene(bootstrap);

        Debug.Log(
            "FASE 6: RegistoDeConhecimento acrescentado ao Bootstrap — " +
            "o que o jogador sabe passa a atravessar as trocas de cena."
        );

        mudancas++;
    }

    // ---------------------------------------------------------------
    // Onde os ecrãs vivem
    // ---------------------------------------------------------------

    // Um objecto sempre activo, irmão do painel do viewer, com os dois sistemas.
    //
    // Os sistemas ficam aqui e não dentro dos painéis de propósito: o
    // PhotoAlbumSystem vive dentro do painel dele, o Update dele não corre com o
    // painel desligado, e por isso o Tab teve de ser lido no PhotographySystem —
    // foi assim que o álbum ficou inalcançável durante semanas. O J e o B são
    // lidos por quem está sempre a correr.
    private static GameObject GarantirAnfitriao(PhotoViewerSystem viewer)
    {
        GameObject painelDoViewer = Referencia(
            new SerializedObject(viewer),
            "photoViewer"
        ) ?? viewer.gameObject;

        Transform pai = painelDoViewer.transform.parent;

        if (pai == null)
        {
            // Sem pai, o anfitrião nascia fora do Canvas: os dois ecrãs ficavam
            // invisíveis e sem cliques, e não havia uma linha no log a dizê-lo.
            Falhar(
                "O painel do viewer não tem pai — não há Canvas onde pendurar " +
                "os ecrãs da investigação."
            );

            return null;
        }

        Transform existente = pai.Find("Investigacao");

        if (existente != null)
            return existente.gameObject;

        GameObject anfitriao = new GameObject(
            "Investigacao",
            typeof(RectTransform),
            typeof(JournalSystem),
            typeof(InvestigationBoardSystem)
        );

        anfitriao.transform.SetParent(pai, false);

        EsticarAoEcra((RectTransform)anfitriao.transform);

        mudancas++;

        return anfitriao;
    }

    // ---------------------------------------------------------------
    // O caderno
    // ---------------------------------------------------------------

    private static void MontarCaderno(GameObject anfitriao, CadeiaDeConhecimento cadeia)
    {
        JournalSystem caderno = anfitriao.GetComponent<JournalSystem>();

        if (caderno == null)
            caderno = anfitriao.AddComponent<JournalSystem>();

        GameObject painel = GarantirPainel(anfitriao, "Journal");

        // Tudo ancorado em fracções do ecrã, e não em pixéis a contar do
        // centro. A primeira montagem usou posições fixas pensadas para 1600 de
        // largura: numa vista de jogo mais estreita os separadores das pontas
        // saíam pelas margens fora e a lista de entradas entrava pela esquerda.
        // A captura correu maximizada e não deu por nada.
        TMP_Text titulo = GarantirTexto(painel, "Titulo", 34f,
            TextAlignmentOptions.Center, TintaLascada, "Caderno");
        Ancorar(titulo, new Vector2(0f, 0.92f), new Vector2(1f, 1f));

        Transform separadores = GarantirFila(painel, "Separadores");
        Ancorar(separadores, new Vector2(0.04f, 0.83f), new Vector2(0.96f, 0.91f));

        GameObject modeloDoSeparador = GarantirModeloDeLinha(
            separadores.gameObject,
            "ModeloDoSeparador",
            new Vector2(180f, 44f),
            typeof(SeparadorDoJournal),
            TintaLascada
        );

        Transform entradas = GarantirColuna(painel, "Entradas");
        Ancorar(entradas, new Vector2(0.04f, 0.10f), new Vector2(0.46f, 0.81f));

        GameObject modeloDaEntrada = GarantirModeloDeLinha(
            entradas.gameObject,
            "ModeloDaEntrada",
            new Vector2(500f, 40f),
            typeof(EntradaDoJournal),
            TintaLascada
        );

        TMP_Text detalhe = GarantirTexto(painel, "Detalhe", 22f,
            TextAlignmentOptions.TopLeft, Nevoa, string.Empty);
        Ancorar(detalhe, new Vector2(0.52f, 0.44f), new Vector2(0.96f, 0.81f));

        TMP_Text cadeiaTexto = GarantirTexto(painel, "Cadeia", 18f,
            TextAlignmentOptions.TopLeft, BetaoManchado, string.Empty);
        Ancorar(cadeiaTexto, new Vector2(0.52f, 0.08f), new Vector2(0.96f, 0.41f));

        SerializedObject so = new SerializedObject(caderno);

        Ligar(so, "painel", painel);
        Ligar(so, "listaDosSeparadores", separadores);
        Ligar(so, "modeloDoSeparador", modeloDoSeparador);
        Ligar(so, "listaDasEntradas", entradas);
        Ligar(so, "modeloDaEntrada", modeloDaEntrada);
        Ligar(so, "detalhe", detalhe);
        Ligar(so, "cadeiaTexto", cadeiaTexto);
        Ligar(so, "cadeia", cadeia);
        Ligar(so, "stateMachine", Object.FindAnyObjectByType<PlayerStateMachine>(
            FindObjectsInactive.Include
        ));

        so.ApplyModifiedPropertiesWithoutUndo();

        painel.SetActive(false);
    }

    // ---------------------------------------------------------------
    // O quadro
    // ---------------------------------------------------------------

    private static void MontarQuadro(GameObject anfitriao)
    {
        InvestigationBoardSystem quadro =
            anfitriao.GetComponent<InvestigationBoardSystem>();

        if (quadro == null)
            quadro = anfitriao.AddComponent<InvestigationBoardSystem>();

        GameObject painel = GarantirPainel(anfitriao, "Board");

        TMP_Text titulo = GarantirTexto(painel, "Titulo", 34f,
            TextAlignmentOptions.Center, TintaLascada, "Quadro");
        Ancorar(titulo, new Vector2(0f, 0.92f), new Vector2(1f, 1f));

        RectTransform superficie = GarantirSuperficie(painel);
        Ancorar(superficie, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.90f));

        GameObject modeloDoCartao = GarantirModeloDeLinha(
            superficie.gameObject,
            "ModeloDoCartao",
            new Vector2(220f, 64f),
            typeof(CartaoDoQuadro),

            // ESCURO, ao contrário das linhas do caderno. Um cartão é um papel
            // claro, e a cor de texto das outras listas é clara também: a
            // primeira captura deu nove rectângulos em branco. É a mesma
            // armadilha do objecto branco na cena bege da FASE 5, e continua a
            // ser só a captura a apanhá-la.
            AsfaltoHumido
        );

        GameObject modeloDaLigacao = GarantirLinha(superficie.gameObject);

        TMP_Text aviso = GarantirTexto(painel, "Aviso", 18f,
            TextAlignmentOptions.Center, BetaoManchado, string.Empty);
        Ancorar(aviso, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.09f));

        SerializedObject so = new SerializedObject(quadro);

        Ligar(so, "painel", painel);
        Ligar(so, "superficie", superficie);
        Ligar(so, "modeloDoCartao", modeloDoCartao);
        Ligar(so, "modeloDaLigacao", modeloDaLigacao);
        Ligar(so, "aviso", aviso);
        Ligar(so, "stateMachine", Object.FindAnyObjectByType<PlayerStateMachine>(
            FindObjectsInactive.Include
        ));

        // Três colunas e não quatro: com quatro, as das pontas encostavam às
        // margens da superfície numa vista de jogo estreita.
        Inteiro(so, "colunas", 3);
        Vector(so, "espacamento", new Vector2(250f, 104f));

        so.ApplyModifiedPropertiesWithoutUndo();

        painel.SetActive(false);
    }

    // ---------------------------------------------------------------
    // Peças
    // ---------------------------------------------------------------

    private static GameObject GarantirPainel(GameObject pai, string nome)
    {
        Color fundo = AsfaltoHumido;
        fundo.a = 0.96f;

        Transform existente = pai.transform.Find(nome);

        if (existente != null)
        {
            Pintar(existente.GetComponent<Image>(), fundo);

            return existente.gameObject;
        }

        GameObject painel = new GameObject(nome, typeof(RectTransform), typeof(Image));

        painel.transform.SetParent(pai.transform, false);

        EsticarAoEcra((RectTransform)painel.transform);

        painel.GetComponent<Image>().color = fundo;

        mudancas++;

        return painel;
    }

    private static void Pintar(Image imagem, Color cor)
    {
        if (imagem == null || imagem.color == cor)
            return;

        imagem.color = cor;

        EditorUtility.SetDirty(imagem);

        mudancas++;
    }

    // A fila dos separadores. As definições do layout são reaplicadas a cada
    // corrida: o childForceExpandWidth é o que faz as seis gavetas repartirem a
    // largura que houver em vez de somarem 180 pixéis cada uma e transbordarem.
    private static Transform GarantirFila(GameObject pai, string nome)
    {
        Transform fila = pai.transform.Find(nome);

        if (fila == null)
        {
            GameObject go = new GameObject(nome, typeof(RectTransform));

            go.transform.SetParent(pai.transform, false);

            fila = go.transform;

            mudancas++;
        }

        HorizontalLayoutGroup disposicao =
            fila.GetComponent<HorizontalLayoutGroup>();

        if (disposicao == null)
        {
            disposicao = fila.gameObject.AddComponent<HorizontalLayoutGroup>();

            mudancas++;
        }

        disposicao.spacing = 10f;
        disposicao.childAlignment = TextAnchor.MiddleCenter;
        disposicao.childControlWidth = true;
        disposicao.childControlHeight = true;
        disposicao.childForceExpandWidth = true;
        disposicao.childForceExpandHeight = true;

        return fila;
    }

    // A coluna das entradas. As linhas ocupam a largura toda da coluna e
    // mantêm a sua altura — com o childControlHeight ligado, uma linha de texto
    // curto encolhia até não se conseguir clicar nela.
    private static Transform GarantirColuna(GameObject pai, string nome)
    {
        Transform coluna = pai.transform.Find(nome);

        if (coluna == null)
        {
            GameObject go = new GameObject(nome, typeof(RectTransform));

            go.transform.SetParent(pai.transform, false);

            coluna = go.transform;

            mudancas++;
        }

        VerticalLayoutGroup disposicao = coluna.GetComponent<VerticalLayoutGroup>();

        if (disposicao == null)
        {
            disposicao = coluna.gameObject.AddComponent<VerticalLayoutGroup>();

            mudancas++;
        }

        disposicao.spacing = 6f;
        disposicao.childAlignment = TextAnchor.UpperLeft;
        disposicao.childControlWidth = true;
        disposicao.childControlHeight = false;
        disposicao.childForceExpandWidth = true;
        disposicao.childForceExpandHeight = false;

        return coluna;
    }

    private static RectTransform GarantirSuperficie(GameObject pai)
    {
        Transform existente = pai.transform.Find("Superficie");

        if (existente != null)
            return (RectTransform)existente;

        GameObject superficie = new GameObject("Superficie", typeof(RectTransform));

        superficie.transform.SetParent(pai.transform, false);

        mudancas++;

        return (RectTransform)superficie.transform;
    }

    // Um modelo de linha: fica desligado dentro da lista e é dele que saem as
    // cópias em runtime. Modelo na cena e não prefab em disco porque assim a
    // montagem inteira é uma coisa só — e porque um prefab por linha de UI era
    // um asset novo para cada ecrã que se acrescentasse.
    private static GameObject GarantirModeloDeLinha(
        GameObject pai,
        string nome,
        Vector2 tamanho,
        System.Type componente,
        Color corDoTexto)
    {
        Transform existente = pai.transform.Find(nome);

        if (existente != null)
        {
            // E aqui NÃO se sai cedo sem mais nada. Sair cedo é o defeito do
            // GarantirFotografia da FASE 5, e reproduzi-o à primeira: mudar a
            // cor do rótulo no script não mudava nada no modelo que já estava
            // na cena, e a montagem dizia «nada a fazer» com os cartões a
            // continuarem em branco.
            Recolorir(existente.gameObject, corDoTexto);

            // E confirmar que o componente lá está. Um modelo criado antes de o
            // componente existir, ou com o campo religado à mão, produzia
            // clones sem ninguém a atendê-los.
            if (existente.GetComponent(componente) == null)
            {
                existente.gameObject.AddComponent(componente);

                mudancas++;
            }

            return existente.gameObject;
        }

        GameObject modelo = new GameObject(nome, typeof(RectTransform), typeof(Image));

        modelo.transform.SetParent(pai.transform, false);

        RectTransform rect = (RectTransform)modelo.transform;
        Centrar(rect, Vector2.zero, tamanho);

        Image fundo = modelo.GetComponent<Image>();

        Color cor = BetaoManchado;
        cor.a = 0.35f;
        fundo.color = cor;

        // Tem de apanhar o rato: sem um gráfico com raycastTarget, clicar numa
        // linha não chega ao IPointerClickHandler e o ecrã fica bonito e morto.
        fundo.raycastTarget = true;

        GameObject texto = new GameObject("Rotulo", typeof(RectTransform));
        texto.transform.SetParent(modelo.transform, false);

        TextMeshProUGUI rotulo = texto.AddComponent<TextMeshProUGUI>();
        rotulo.fontSize = 18f;
        rotulo.alignment = TextAlignmentOptions.Center;
        rotulo.color = corDoTexto;
        rotulo.raycastTarget = false;
        rotulo.text = string.Empty;

        EsticarAoEcra((RectTransform)texto.transform);

        modelo.AddComponent(componente);

        LigarPecas(modelo.GetComponent(componente), rotulo, fundo);

        modelo.SetActive(false);

        mudancas++;

        return modelo;
    }

    private static void Recolorir(GameObject modelo, Color corDoTexto)
    {
        TMP_Text rotulo = modelo.GetComponentInChildren<TMP_Text>(true);

        if (rotulo == null || rotulo.color == corDoTexto)
            return;

        rotulo.color = corDoTexto;

        EditorUtility.SetDirty(rotulo);

        mudancas++;
    }

    // O `fundo` só existe no cartão do quadro; nas linhas do caderno o
    // FindProperty devolve null e o Ligar não faz nada. É de propósito: um
    // modelo de linha é a mesma peça com componentes diferentes.
    private static void LigarPecas(Component componente, TMP_Text rotulo, Image fundo)
    {
        SerializedObject so = new SerializedObject(componente);

        Ligar(so, "rotulo", rotulo);
        Ligar(so, "fundo", fundo);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // A linha de uma ligação. Um Image fino que o quadro roda e estica entre os
    // dois cartões — não apanha o rato, ou tapava os papéis que une.
    private static GameObject GarantirLinha(GameObject pai)
    {
        Transform existente = pai.transform.Find("ModeloDaLigacao");

        if (existente != null)
        {
            Pintar(existente.GetComponent<Image>(), Nevoa);

            return existente.gameObject;
        }

        GameObject linha = new GameObject(
            "ModeloDaLigacao",
            typeof(RectTransform),
            typeof(Image)
        );

        linha.transform.SetParent(pai.transform, false);

        Image traco = linha.GetComponent<Image>();
        traco.color = Nevoa;
        traco.raycastTarget = false;

        Centrar((RectTransform)linha.transform, Vector2.zero, new Vector2(10f, 3f));

        linha.SetActive(false);

        mudancas++;

        return linha;
    }

    private static TMP_Text GarantirTexto(
        GameObject pai,
        string nome,
        float corpo,
        TextAlignmentOptions alinhamento,
        Color cor,
        string conteudo)
    {
        Transform existente = pai.transform.Find(nome);

        if (existente != null)
        {
            TMP_Text jaLa = existente.GetComponent<TMP_Text>();

            if (jaLa != null)
            {
                // Não se sai cedo sem reaplicar. Mudar o corpo, a cor ou o
                // título aqui no script tem de chegar à cena que já existe —
                // foi por sair cedo que o GarantirFotografia da FASE 5 deixou
                // assets desactualizados, e eu repeti-o duas vezes nesta.
                Reaplicar(jaLa, corpo, alinhamento, cor, conteudo);

                return jaLa;
            }
        }

        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai.transform, false);

        TextMeshProUGUI texto = go.AddComponent<TextMeshProUGUI>();
        texto.raycastTarget = false;

        Reaplicar(texto, corpo, alinhamento, cor, conteudo);

        mudancas++;

        return texto;
    }

    private static void Reaplicar(
        TMP_Text texto,
        float corpo,
        TextAlignmentOptions alinhamento,
        Color cor,
        string conteudo)
    {
        bool mudou =
            texto.fontSize != corpo ||
            texto.alignment != alinhamento ||
            texto.color != cor;

        // O conteúdo só se escreve quando o script tem alguma coisa a dizer:
        // os textos que o jogo preenche em runtime (detalhe, cadeia, aviso)
        // entram aqui com string vazia e não se apagam por causa disso.
        if (!string.IsNullOrEmpty(conteudo) && texto.text != conteudo)
        {
            texto.text = conteudo;

            mudou = true;
        }

        texto.fontSize = corpo;
        texto.alignment = alinhamento;
        texto.color = cor;

        if (mudou)
        {
            EditorUtility.SetDirty(texto);

            mudancas++;
        }
    }

    // Âncoras em fracções do ecrã. Devolve true se mexeu em alguma coisa — e é
    // reaplicado a cada corrida de propósito: um Garantir que sai cedo nunca
    // corrige o que já está montado, que foi o defeito que a FASE 5 deixou no
    // GarantirFotografia e que eu repeti aqui na primeira versão.
    private static bool Ancorar(Component alvo, Vector2 minimo, Vector2 maximo)
    {
        if (!(alvo is RectTransform rect))
        {
            if (alvo == null || !(alvo.transform is RectTransform doTransform))
                return false;

            rect = doTransform;
        }

        if (rect.anchorMin == minimo &&
            rect.anchorMax == maximo &&
            rect.offsetMin == Vector2.zero &&
            rect.offsetMax == Vector2.zero)
        {
            return false;
        }

        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = minimo;
        rect.anchorMax = maximo;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        mudancas++;

        return true;
    }

    private static void Inteiro(SerializedObject so, string campo, int valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || prop.intValue == valor)
            return;

        prop.intValue = valor;

        mudancas++;
    }

    private static void Vector(SerializedObject so, string campo, Vector2 valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || prop.vector2Value == valor)
            return;

        prop.vector2Value = valor;

        mudancas++;
    }

    private static void Centrar(RectTransform rect, Vector2 posicao, Vector2 tamanho)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
    }

    private static void EsticarAoEcra(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject Referencia(SerializedObject so, string campo)
    {
        SerializedProperty prop = so.FindProperty(campo);

        return prop != null ? prop.objectReferenceValue as GameObject : null;
    }

    private static void Ligar(SerializedObject so, string campo, Object valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || prop.objectReferenceValue == valor)
            return;

        prop.objectReferenceValue = valor;

        mudancas++;
    }

    private static void Falhar(string mensagem)
    {
        Debug.LogError("FASE 6: " + mensagem);

        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}
