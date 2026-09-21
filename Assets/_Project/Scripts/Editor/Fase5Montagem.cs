using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Monta na cena o que a FASE 5 acrescentou: o painel da comparação, as duas
// linhas de texto do viewer (legenda e verso), e as referências novas entre
// componentes.
//
// Existe porque a alternativa era editar o Prototype_Player.unity à mão em
// YAML. A cena tem 3.900 linhas e um GUID mal copiado parte todas as
// referências de um script de uma vez — e não dá erro nenhum, só deixa de
// funcionar. Isto é reprodutível, corre duas vezes sem duplicar nada e
// diz o que fez.
//
// Corre pelo menu ou em batchmode:
//   Unity -batchmode -nographics -quit -projectPath . \
//         -executeMethod Fase5Montagem.Correr -logFile -
public static class Fase5Montagem
{
    // A paleta e a do Pine_Hollow_Direccao_Artistica.md, §7.2. O sistema de
    // design do repo manda -- nao se inventam cores para UI num jogo que tem
    // a paleta escrita com valores. "Nunca branco puro" e regra de la, e por
    // isso ate o clarao do alinhamento e tinta lascada e nao branco.
    private static readonly Color AsfaltoHumido = De(0x33, 0x36, 0x3A);
    private static readonly Color TintaLascada = De(0xC8, 0xC4, 0xB8);
    private static readonly Color Nevoa = De(0xA8, 0xB2, 0xB5);
    private static readonly Color BetaoManchado = De(0x7A, 0x7B, 0x76);

    private static Color De(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    private const string CaminhoDaCena =
        "Assets/_Project/Scenes/Prototype_Player.unity";

    private static int mudancas;

    [MenuItem("Pine Hollow/FASE 5 — montar a UI da fotografia")]
    public static void Correr()
    {
        mudancas = 0;

        Scene cena = EditorSceneManager.OpenScene(CaminhoDaCena, OpenSceneMode.Single);

        PhotographySystem fotografia = Object.FindAnyObjectByType<PhotographySystem>(
            FindObjectsInactive.Include
        );

        PhotoAlbumSystem album = Object.FindAnyObjectByType<PhotoAlbumSystem>(
            FindObjectsInactive.Include
        );

        PhotoViewerSystem viewer = Object.FindAnyObjectByType<PhotoViewerSystem>(
            FindObjectsInactive.Include
        );

        if (fotografia == null || album == null || viewer == null)
        {
            Falhar("Faltam componentes de fotografia na cena.");
            return;
        }

        LigarACamaraDeCaptura(fotografia);
        EscurecerOFundoDoAlbum(album);
        MontarTextosDoViewer(viewer);
        MontarComparacao(album, viewer);
        MontarFotografiasDoMundo(fotografia);

        if (mudancas > 0)
        {
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"FASE 5: cena montada, {mudancas} alterações gravadas.");
        }
        else
        {
            Debug.Log("FASE 5: a cena já estava montada, nada a fazer.");
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    // A PhotoCaptureCamera está no GameObject que nasce desligado na cena e
    // nada no código o activa — o PhotographySystem liga o `photoCamera`, que
    // é o CameraBody (a máquina na mão), outra coisa. Render() sobre uma
    // câmara num GameObject inactivo não desenha nada.
    private static void LigarACamaraDeCaptura(PhotographySystem fotografia)
    {
        SerializedObject so = new SerializedObject(fotografia);

        SerializedProperty prop = so.FindProperty("photoCaptureCamera");

        if (prop == null || prop.objectReferenceValue == null)
        {
            Debug.LogWarning("photoCaptureCamera por ligar no inspector.");
            return;
        }

        Camera camara = (Camera)prop.objectReferenceValue;

        if (camara.gameObject.activeSelf)
            return;

        camara.gameObject.SetActive(true);

        // Continua a não desenhar para o ecrã: tem uma RenderTexture como
        // alvo e uma profundidade abaixo da câmara do jogador.
        Debug.Log(
            $"FASE 5: {camara.name} estava inactiva — activada. " +
            "Sem isto, Render() não produzia imagem nenhuma."
        );

        mudancas++;
    }

    // O painel do álbum era quase transparente. Enquanto o Tab não abria
    // ninguém reparou; aberto, o mundo lê-se por trás das miniaturas e o
    // título quase desaparece. Mesmo fundo do painel da comparação, que é a
    // mesma categoria de ecrã.
    private static void EscurecerOFundoDoAlbum(PhotoAlbumSystem album)
    {
        Image fundo = album.GetComponent<Image>();

        if (fundo == null)
            return;

        Color escuro = AsfaltoHumido;
        escuro.a = 0.96f;

        if (fundo.color == escuro)
            return;

        fundo.color = escuro;

        mudancas++;
    }

    private static void MontarTextosDoViewer(PhotoViewerSystem viewer)
    {
        SerializedObject so = new SerializedObject(viewer);

        GameObject painel = Referencia(so, "photoViewer") ?? viewer.gameObject;

        TMP_Text legenda = GarantirTexto(
            painel,
            "Legenda",
            new Vector2(0f, -320f),
            new Vector2(900f, 90f),
            24f,
            TextAlignmentOptions.Top
        );

        TMP_Text verso = GarantirTexto(
            painel,
            "Verso",
            Vector2.zero,
            new Vector2(700f, 400f),
            32f,
            TextAlignmentOptions.Center
        );

        // O verso nasce desligado: abre-se sempre pela frente.
        if (verso.gameObject.activeSelf)
        {
            verso.gameObject.SetActive(false);
            mudancas++;
        }

        Ligar(so, "legenda", legenda);
        Ligar(so, "versoTexto", verso);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void MontarComparacao(PhotoAlbumSystem album, PhotoViewerSystem viewer)
    {
        SerializedObject soAlbum = new SerializedObject(album);

        // Nada de sair cedo por já haver painel. Sair cedo é o que deixou o
        // `flash` por ligar quando o painel foi criado numa corrida anterior:
        // um script de montagem que salta peças por a primeira já existir não
        // é idempotente, é frágil.
        PhotoComparisonSystem jaLa = Object.FindAnyObjectByType<PhotoComparisonSystem>(
            FindObjectsInactive.Include
        );

        if (jaLa != null)
        {
            ReligarComparacao(jaLa, soAlbum);
            return;
        }

        // Pendurado no mesmo pai do viewer: é uma camada de ecrã inteiro como
        // ele, e assim herda o Canvas sem o procurar.
        GameObject painelDoViewer = Referencia(
            new SerializedObject(viewer),
            "photoViewer"
        ) ?? viewer.gameObject;

        Transform pai = painelDoViewer.transform.parent;

        GameObject painel = new GameObject(
            "PhotoComparison",
            typeof(RectTransform),
            typeof(Image),
            typeof(PhotoComparisonSystem)
        );

        painel.transform.SetParent(pai, false);

        RectTransform rect = (RectTransform)painel.transform;
        EsticarAoEcra(rect);

        Image fundo = painel.GetComponent<Image>();

        Color escuro = AsfaltoHumido;
        escuro.a = 0.96f;
        fundo.color = escuro;

        RawImage fixa = GarantirImagem(painel, "Fixa", new Vector2(-260f, 40f));
        RawImage movel = GarantirImagem(painel, "Movel", new Vector2(260f, 40f));

        TMP_Text legenda = GarantirTexto(
            painel,
            "Legenda",
            new Vector2(0f, -340f),
            new Vector2(1100f, 110f),
            22f,
            TextAlignmentOptions.Top,
            Nevoa
        );

        PhotoComparisonSystem comparacao = painel.GetComponent<PhotoComparisonSystem>();

        SerializedObject so = new SerializedObject(comparacao);

        Ligar(so, "painel", painel);
        Ligar(so, "imagemFixa", fixa);
        Ligar(so, "imagemMovel", movel);
        Ligar(so, "legenda", legenda);
        Ligar(so, "flash", GarantirFlash(painel));
        Ligar(so, "stateMachine", Object.FindAnyObjectByType<PlayerStateMachine>(
            FindObjectsInactive.Include
        ));

        so.ApplyModifiedPropertiesWithoutUndo();

        painel.SetActive(false);

        Ligar(soAlbum, "photoComparisonSystem", comparacao);
        soAlbum.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("FASE 5: painel PhotoComparison criado e ligado ao álbum.");

        mudancas++;
    }

    // Volta a ligar o que faltar num painel de comparação já existente. As
    // referências perdem-se quando um asset é apagado e recriado, e um campo
    // por ligar aqui é um NullReference em play mode e nada no editor.
    private static void ReligarComparacao(
        PhotoComparisonSystem comparacao,
        SerializedObject soAlbum)
    {
        GameObject painel = comparacao.gameObject;

        SerializedObject so = new SerializedObject(comparacao);

        Ligar(so, "painel", painel);
        Ligar(so, "imagemFixa", GarantirImagem(painel, "Fixa", new Vector2(-260f, 40f)));
        Ligar(so, "imagemMovel", GarantirImagem(painel, "Movel", new Vector2(260f, 40f)));
        Ligar(so, "flash", GarantirFlash(painel));
        Ligar(so, "stateMachine", Object.FindAnyObjectByType<PlayerStateMachine>(
            FindObjectsInactive.Include
        ));

        so.ApplyModifiedPropertiesWithoutUndo();

        Ligar(soAlbum, "photoComparisonSystem", comparacao);
        soAlbum.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------
    // Conteúdo: as duas fotografias do canon, e onde se apanham
    // ---------------------------------------------------------------

    private const string PastaDasFotografias = "Assets/_Project/Photography";

    // Sem isto a fase inteira é inalcançável em play mode: o álbum abre vazio,
    // a comparação só compara capturas (que nascem sem alinhamentos) e o
    // alinhamento nunca dispara. O código estava todo escrito e não havia uma
    // única fotografia no jogo.
    //
    // O par é o que a task descreve — 1986 com 1994 — e o verso é canon,
    // citado à letra do Pine_Hollow_Historia.md:431, onde é a fotografia de
    // 17 de Julho de 1986 com o Thomas, o Elias e o protagonista. A de 1994
    // fica SEM verso de propósito: não há verso canónico para ela, e inventar
    // um era pior — placeholder tem o hábito de sobreviver.
    private static void MontarFotografiasDoMundo(PhotographySystem fotografia)
    {
        InspectionSystem inspeccao = Object.FindAnyObjectByType<InspectionSystem>(
            FindObjectsInactive.Include
        );

        if (inspeccao == null)
            return;

        PhotoAsset mil986 = GarantirFotografia(
            "Fotografia_Estudio_1986",
            1986,
            "17 de Julho de 1986",
            "Estúdio do Ethan",
            new[] { "Thomas Hale", "Elias Ward" },
            "Três pessoas à porta da fábrica. A terceira está de costas.",
            "Ele ainda não chegou.",
            alinhaCom: "Fotografia_Estudio_1994",
            descoberta: "A porta lateral da fábrica, que em 1994 está tapada."
        );

        PhotoAsset mil994 = GarantirFotografia(
            "Fotografia_Estudio_1994",
            1994,
            "3 de Outubro de 1994",
            "Estúdio do Ethan",
            new[] { "Thomas Hale" },
            "A mesma fachada, oito anos depois. Falta uma porta.",
            verso: null,
            alinhaCom: null,
            descoberta: null
        );

        Apanhavel(inspeccao, fotografia, mil986, new Vector3(-0.35f, 0f, 0f));
        Apanhavel(inspeccao, fotografia, mil994, new Vector3(0.35f, 0f, 0f));
    }

    // Sem imagem, o material por omissão do URP é branco — e a cena é bege
    // clara. Uma fotografia branca sobre uma parede bege não se vê, e foi
    // isso que a primeira captura mostrou: o objecto estava no sítio certo,
    // visível e à frente da câmara, e na imagem não havia fotografia nenhuma.
    //
    // «Betão manchado» é da paleta do repo (§7.2) e fica dentro da disciplina
    // de albedo (50–240 sRGB). O nome do asset diz que é provisório: a imagem
    // a sério chega na FASE 18.
    private static Material MaterialDaFotografia()
    {
        const string caminho = "Assets/_Project/Materials/Fotografia_SemImagem.mat";

        Material existente = AssetDatabase.LoadAssetAtPath<Material>(caminho);

        if (existente != null)
            return existente;

        if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
            AssetDatabase.CreateFolder("Assets/_Project", "Materials");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            return null;

        Material material = new Material(shader);
        material.SetColor("_BaseColor", BetaoManchado);

        AssetDatabase.CreateAsset(material, caminho);

        Debug.Log($"FASE 5: material provisório da fotografia criado em {caminho}.");

        mudancas++;

        return material;
    }

    private static PhotoAsset GarantirFotografia(
        string nome,
        int ano,
        string data,
        string local,
        string[] personagens,
        string frente,
        string verso,
        string alinhaCom,
        string descoberta)
    {
        string caminho = $"{PastaDasFotografias}/{nome}.asset";

        PhotoAsset existente = AssetDatabase.LoadAssetAtPath<PhotoAsset>(caminho);

        if (existente != null)
            return existente;

        if (!AssetDatabase.IsValidFolder(PastaDasFotografias))
            AssetDatabase.CreateFolder("Assets/_Project", "Photography");

        PhotoAsset asset = ScriptableObject.CreateInstance<PhotoAsset>();

        // Preenchido por JsonUtility porque os campos sao privados e
        // [SerializeField] -- e o mesmo caminho que o Unity usa para os
        // desserializar de um .asset.
        string pessoas = string.Join(",", System.Array.ConvertAll(
            personagens, pessoa => "\"" + pessoa + "\""
        ));

        string alinhamentos = alinhaCom == null
            ? "[]"
            : "[{\"comId\":\"" + alinhaCom + "\",\"escalaAlvo\":1," +
              "\"toleranciaPosicao\":0.06,\"toleranciaRotacao\":6," +
              "\"toleranciaEscala\":0.1}]";

        string descobertas = descoberta == null
            ? "[]"
            : "[{\"id\":\"porta-lateral\",\"tipo\":2,\"descricao\":\"" +
              descoberta + "\",\"revelaAoAlinharCom\":\"" + alinhaCom + "\"}]";

        JsonUtility.FromJsonOverwrite(
            "{\"fotografia\":{" +
            $"\"id\":\"{nome}\",\"ano\":{ano}," +
            $"\"data\":\"{data}\",\"local\":\"{local}\"," +
            $"\"personagens\":[{pessoas}]," +
            $"\"frente\":\"{frente}\"," +
            $"\"verso\":\"{verso ?? string.Empty}\"," +
            $"\"alinhamentos\":{alinhamentos}," +
            $"\"descobertas\":{descobertas}" +
            "}}",
            asset
        );

        AssetDatabase.CreateAsset(asset, caminho);

        Debug.Log($"FASE 5: fotografia autorada criada em {caminho}.");

        mudancas++;

        return asset;
    }

    // Um volume fino na layer Interactable, com colisor, que o raycast de
    // interacção apanha e o E pega. Sem arte: o material é o do URP por
    // omissão e a imagem da fotografia só chega na FASE 18.
    //
    // CUBO e não Quad. Um Quad tem uma face só: com a rotação que a inspecção
    // lhe dá, ficava de costas para a câmara e não se via nada na mão — e ao
    // virar para ler o verso desaparecia de vez. Uma fotografia tem duas
    // faces, e o objecto que a representa também tem de ter.
    private static void Apanhavel(
        InspectionSystem inspeccao,
        PhotographySystem fotografia,
        PhotoAsset asset,
        Vector3 desvio)
    {
        string nome = asset.name;

        GameObject jaLa = GameObject.Find(nome);

        if (jaLa != null)
        {
            // Já está na cena, mas o asset pode ter sido recriado e a
            // referência ter ficado a apontar a nada. Religa-se sempre.
            PhotoInteractable existente = jaLa.GetComponent<PhotoInteractable>();

            if (existente != null)
            {
                SerializedObject soExistente = new SerializedObject(existente);

                Ligar(soExistente, "inspectionSystem", inspeccao);
                Ligar(soExistente, "photographySystem", fotografia);
                Ligar(soExistente, "fotografia", asset);

                soExistente.ApplyModifiedPropertiesWithoutUndo();
            }

            return;
        }

        GameObject referencia = GameObject.Find("InspectionObject");

        Vector3 posicao = referencia != null
            ? referencia.transform.position + desvio + Vector3.up * 0.25f
            : new Vector3(desvio.x, 1f, desvio.z + 2f);

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nome;
        go.transform.position = posicao;

        // 18 × 12 cm e 2 mm de espessura: uma fotografia de 35 mm ampliada.
        go.transform.localScale = new Vector3(0.18f, 0.12f, 0.002f);
        go.layer = LayerMask.NameToLayer("Interactable");

        Renderer renderer = go.GetComponent<Renderer>();

        if (renderer != null)
            renderer.sharedMaterial = MaterialDaFotografia();

        BoxCollider colisor = go.GetComponent<BoxCollider>();

        // O colisor acompanha a escala, e a 2 mm ficava fino de mais para o
        // raycast lhe acertar de frente. Engorda-se só o colisor.
        if (colisor != null)
            colisor.size = new Vector3(1f, 1f, 30f);

        PhotoInteractable interactable = go.AddComponent<PhotoInteractable>();

        SerializedObject so = new SerializedObject(interactable);

        Ligar(so, "inspectionSystem", inspeccao);
        Ligar(so, "photographySystem", fotografia);
        Ligar(so, "fotografia", asset);

        so.FindProperty("stateId").stringValue = nome;

        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"FASE 5: {nome} pousada na cena em {posicao}.");

        mudancas++;
    }

    // ---------------------------------------------------------------
    // Utilitários
    // ---------------------------------------------------------------

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

    // Idempotente: se já existir um filho com este nome, devolve-o em vez de
    // criar um segundo. Correr isto duas vezes não duplica nada.
    private static TMP_Text GarantirTexto(
        GameObject pai,
        string nome,
        Vector2 posicao,
        Vector2 tamanho,
        float corpo,
        TextAlignmentOptions alinhamento,
        Color? cor = null)
    {
        Transform existente = pai.transform.Find(nome);

        if (existente != null)
        {
            TMP_Text jaLa = existente.GetComponent<TMP_Text>();

            if (jaLa != null)
                return jaLa;
        }

        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai.transform, false);

        TextMeshProUGUI texto = go.AddComponent<TextMeshProUGUI>();
        texto.fontSize = corpo;
        texto.alignment = alinhamento;
        texto.color = cor ?? TintaLascada;
        texto.raycastTarget = false;
        texto.text = string.Empty;

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;

        mudancas++;

        return texto;
    }

    private static RawImage GarantirImagem(GameObject pai, string nome, Vector2 posicao)
    {
        Transform existente = pai.transform.Find(nome);

        if (existente != null)
        {
            RawImage jaLa = existente.GetComponent<RawImage>();

            if (jaLa != null)
                return jaLa;
        }

        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai.transform, false);

        RawImage imagem = go.AddComponent<RawImage>();
        imagem.raycastTarget = false;

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(480f, 320f);

        mudancas++;

        return imagem;
    }

    // O clarao do alinhamento. Ecra inteiro, por cima de tudo o que esta no
    // painel, e desligado enquanto nao ha nada a assinalar -- um Image
    // transparente a ecra inteiro continua a apanhar o rato.
    private static Image GarantirFlash(GameObject pai)
    {
        Transform existente = pai.transform.Find("Flash");

        if (existente != null)
        {
            Image jaLa = existente.GetComponent<Image>();

            if (jaLa != null)
                return jaLa;
        }

        GameObject go = new GameObject("Flash", typeof(RectTransform));
        go.transform.SetParent(pai.transform, false);
        go.transform.SetAsLastSibling();

        Image imagem = go.AddComponent<Image>();

        Color clarao = TintaLascada;
        clarao.a = 0f;
        imagem.color = clarao;
        imagem.raycastTarget = false;

        EsticarAoEcra((RectTransform)go.transform);

        go.SetActive(false);

        mudancas++;

        return imagem;
    }

    private static void EsticarAoEcra(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Falhar(string mensagem)
    {
        Debug.LogError("FASE 5: " + mensagem);

        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}
