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
        MontarTextosDoViewer(viewer);
        MontarComparacao(album, viewer);

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

        if (soAlbum.FindProperty("photoComparisonSystem").objectReferenceValue != null)
            return;

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
        fundo.color = new Color(0.04f, 0.04f, 0.05f, 0.96f);

        RawImage fixa = GarantirImagem(painel, "Fixa", new Vector2(-260f, 40f));
        RawImage movel = GarantirImagem(painel, "Movel", new Vector2(260f, 40f));

        TMP_Text legenda = GarantirTexto(
            painel,
            "Legenda",
            new Vector2(0f, -340f),
            new Vector2(1100f, 110f),
            22f,
            TextAlignmentOptions.Top
        );

        PhotoComparisonSystem comparacao = painel.GetComponent<PhotoComparisonSystem>();

        SerializedObject so = new SerializedObject(comparacao);

        Ligar(so, "painel", painel);
        Ligar(so, "imagemFixa", fixa);
        Ligar(so, "imagemMovel", movel);
        Ligar(so, "legenda", legenda);
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
        TextAlignmentOptions alinhamento)
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
        texto.color = new Color(0.90f, 0.89f, 0.85f);
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
