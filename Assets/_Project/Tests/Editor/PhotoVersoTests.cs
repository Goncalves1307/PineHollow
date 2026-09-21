using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Testes do verso: virar a fotografia no viewer, virá-la na mão, e o
// PhotoInteractable que a apanha do mundo.
//
// «Virar» e «ler verso» são os dois únicos itens do bloco «Inspeção» da
// checklist que não existiam — zoom, rodar e examinar já lá estavam desde a
// FASE 4. O que faltava não eram os verbos, era a segunda face.
public class PhotoVersoTests
{
    private readonly List<Object> criados = new List<Object>();

    private GameObject Novo(string nome)
    {
        GameObject go = new GameObject(nome);
        criados.Add(go);
        return go;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object o in criados)
        {
            if (o != null)
                Object.DestroyImmediate(o);
        }

        criados.Clear();
    }

    private static void Ligar(object alvo, string nome, object valor)
    {
        alvo.GetType()
            .GetField(nome, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(alvo, valor);
    }

    private static PhotoData Foto(string json)
    {
        return JsonUtility.FromJson<PhotoData>(json);
    }

    // ---------------------------------------------------------------
    // Viewer
    // ---------------------------------------------------------------

    private PhotoViewerSystem ViewerMontado(
        out PlayerStateMachine maquina,
        out TMPro.TMP_Text legenda,
        out TMPro.TMP_Text verso)
    {
        GameObject host = Novo("Player");
        maquina = host.AddComponent<PlayerStateMachine>();

        GameObject painel = Novo("PhotoViewer");
        PhotoViewerSystem viewer = painel.AddComponent<PhotoViewerSystem>();

        GameObject linhaLegenda = Novo("Legenda");
        legenda = linhaLegenda.AddComponent<TMPro.TextMeshProUGUI>();

        GameObject linhaVerso = Novo("Verso");
        verso = linhaVerso.AddComponent<TMPro.TextMeshProUGUI>();

        Ligar(viewer, "photoViewer", painel);
        Ligar(viewer, "legenda", legenda);
        Ligar(viewer, "versoTexto", verso);
        Ligar(viewer, "stateMachine", maquina);

        painel.SetActive(false);

        return viewer;
    }

    [Test]
    public void Viewer_AbreSemprePelaFrente()
    {
        PhotoViewerSystem viewer = ViewerMontado(out _, out _, out _);

        PhotoData foto = Foto("{\"id\":\"a\",\"verso\":\"Agora és tu.\"}");

        viewer.Open(foto);
        Assert.IsFalse(viewer.AMostrarVerso);

        viewer.Virar();
        Assert.IsTrue(viewer.AMostrarVerso);

        viewer.Close();
        viewer.Open(foto);

        // Uma fotografia aberta de costas por ter ficado assim da vez
        // anterior é uma surpresa sem motivo.
        Assert.IsFalse(viewer.AMostrarVerso);
    }

    [Test]
    public void Viewer_VirarMostraOTextoDoVersoEEscondeALegenda()
    {
        PhotoViewerSystem viewer = ViewerMontado(
            out _,
            out TMPro.TMP_Text legenda,
            out TMPro.TMP_Text verso
        );

        viewer.Open(Foto(
            "{\"id\":\"a\",\"data\":\"17 de Julho de 1986\"," +
            "\"verso\":\"Ele ainda não chegou.\"}"
        ));

        Assert.IsTrue(legenda.gameObject.activeSelf);
        Assert.IsFalse(verso.gameObject.activeSelf);

        viewer.Virar();

        Assert.IsFalse(legenda.gameObject.activeSelf);
        Assert.IsTrue(verso.gameObject.activeSelf);
        Assert.AreEqual("Ele ainda não chegou.", verso.text);
    }

    [Test]
    public void Viewer_FotografiaSemVerso_NaoSeVira()
    {
        // Não há segunda face: virá-la mostrava um painel vazio.
        PhotoViewerSystem viewer = ViewerMontado(out _, out _, out _);

        viewer.Open(Foto("{\"id\":\"a\",\"data\":\"Hoje\"}"));

        Assert.IsFalse(viewer.Virar());
        Assert.IsFalse(viewer.AMostrarVerso);
    }

    [Test]
    public void Viewer_LegendaJuntaDataLocalEPessoas()
    {
        Assert.AreEqual(
            "17 de Julho de 1986, 23:42\nFábrica\nThomas Hale, Elias Ward",
            PhotoViewerSystem.Legendar(Foto(
                "{\"data\":\"17 de Julho de 1986, 23:42\"," +
                "\"local\":\"Fábrica\"," +
                "\"personagens\":[\"Thomas Hale\",\"Elias Ward\"]}"
            ))
        );
    }

    [Test]
    public void Viewer_LegendaNaoDeixaLinhasVaziasNoQueEstaPorPreencher()
    {
        Assert.AreEqual(
            "Fábrica",
            PhotoViewerSystem.Legendar(Foto("{\"local\":\"Fábrica\"}"))
        );

        Assert.AreEqual(
            string.Empty,
            PhotoViewerSystem.Legendar(Foto("{\"id\":\"a\"}"))
        );
    }

    [Test]
    public void Viewer_FecharTiraOModoDaPilha()
    {
        PhotoViewerSystem viewer = ViewerMontado(out PlayerStateMachine maquina, out _, out _);

        viewer.Open(Foto("{\"id\":\"a\"}"));
        Assert.IsTrue(maquina.IsTopMode(PlayerState.ViewingPhoto));

        viewer.Close();

        Assert.AreEqual(0, maquina.OpenModeCount);
    }

    // ---------------------------------------------------------------
    // Virar na mão
    // ---------------------------------------------------------------

    private InspectionSystem InspeccaoMontada(
        out PlayerStateMachine maquina,
        out GameObject camara,
        out TMPro.TMP_Text texto)
    {
        GameObject host = Novo("Player");
        maquina = host.AddComponent<PlayerStateMachine>();
        host.AddComponent<AudioSource>();

        camara = Novo("Camara");
        camara.AddComponent<Camera>();

        InspectionSystem sistema = host.AddComponent<InspectionSystem>();

        GameObject controlos = Novo("InspectionControls");
        controlos.SetActive(false);

        GameObject linha = Novo("InspectionDescription");
        texto = linha.AddComponent<TMPro.TextMeshProUGUI>();
        linha.SetActive(false);

        Ligar(sistema, "playerCamera", camara.GetComponent<Camera>());
        Ligar(sistema, "stateMachine", maquina);
        Ligar(sistema, "inspectionControls", controlos);
        Ligar(sistema, "descriptionText", texto);
        Ligar(sistema, "inspectionSource", host.GetComponent<AudioSource>());

        return sistema;
    }

    [Test]
    public void Inspeccao_ComVerso_TrocaOTextoAoVirar()
    {
        InspectionSystem sistema = InspeccaoMontada(
            out _,
            out _,
            out TMPro.TMP_Text texto
        );

        sistema.Inspect(Novo("Fotografia"), "Dois homens à porta.", "Agora és tu.");

        Assert.IsTrue(sistema.TemVerso);
        Assert.AreEqual("Dois homens à porta.", texto.text);

        Assert.IsTrue(sistema.Virar());

        Assert.IsTrue(sistema.AMostrarVerso);
        Assert.AreEqual("Agora és tu.", texto.text);

        sistema.Virar();

        Assert.AreEqual("Dois homens à porta.", texto.text);
    }

    [Test]
    public void Inspeccao_ObjectoDeUmaFaceSo_NaoSeVira()
    {
        InspectionSystem sistema = InspeccaoMontada(out _, out _, out _);

        sistema.Inspect(Novo("Cinzeiro"), "Um cinzeiro cheio.");

        Assert.IsFalse(sistema.TemVerso);
        Assert.IsFalse(sistema.Virar());
        Assert.IsFalse(sistema.AMostrarVerso);
    }

    [Test]
    public void Inspeccao_VirarDaMeiaVoltaAoObjecto()
    {
        InspectionSystem sistema = InspeccaoMontada(
            out _,
            out GameObject camara,
            out _
        );

        GameObject foto = Novo("Fotografia");

        sistema.Inspect(foto, "Frente.", "Verso.");

        Quaternion antes = foto.transform.rotation;

        sistema.Virar();

        Assert.AreEqual(
            180f,
            Quaternion.Angle(antes, foto.transform.rotation),
            0.5f,
            "virar não deu meia-volta"
        );
    }

    [Test]
    public void Inspeccao_LargarLimpaOVerso()
    {
        // Sem isto, o objecto seguinte herdava o verso do anterior e um
        // cinzeiro passava a ter o que estava escrito atrás da fotografia.
        InspectionSystem sistema = InspeccaoMontada(
            out PlayerStateMachine maquina,
            out _,
            out _
        );

        sistema.Inspect(Novo("Fotografia"), "Frente.", "Verso.");
        sistema.Virar();

        maquina.BeginFrame();
        maquina.RequestBack();

        // Só o Update deste componente. Um SendMessage ao GameObject corria
        // também o da PlayerStateMachine, que é o primeiro da lista e cujo
        // BeginFrame apaga o pedido de Esc antes de o consumidor o ver — o
        // teste falhava por causa da ordem dos componentes, não do código.
        sistema.GetType()
            .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(sistema, null);

        Assert.IsFalse(sistema.IsInspecting);
        Assert.IsFalse(sistema.TemVerso);
        Assert.IsFalse(sistema.AMostrarVerso);
    }

    // ---------------------------------------------------------------
    // Apanhar do mundo
    // ---------------------------------------------------------------

    [Test]
    public void PhotoInteractable_ApanhaGuardaNoAlbumEPoeNaMao()
    {
        InspectionSystem inspeccao = InspeccaoMontada(out PlayerStateMachine maquina, out _, out _);

        GameObject hostDaFotografia = Novo("PlayerFotografia");
        PhotographySystem fotografia = hostDaFotografia.AddComponent<PhotographySystem>();

        Ligar(fotografia, "photoAlbum", Novo("PhotoAlbum"));
        Ligar(fotografia, "photoCamera", Novo("CameraBody"));
        Ligar(fotografia, "photoPreview", Novo("PhotoPreview"));
        Ligar(fotografia, "stateMachine", maquina);

        PhotoAsset asset = ScriptableObject.CreateInstance<PhotoAsset>();
        asset.name = "estudio-1986";
        criados.Add(asset);

        JsonUtility.FromJsonOverwrite(
            "{\"fotografia\":{\"ano\":1986,\"frente\":\"Dois homens.\"," +
            "\"verso\":\"É aqui que começa.\"}}",
            asset
        );

        GameObject noMundo = Novo("Fotografia_Estudio");
        PhotoInteractable interactable = noMundo.AddComponent<PhotoInteractable>();

        Ligar(interactable, "inspectionSystem", inspeccao);
        Ligar(interactable, "photographySystem", fotografia);
        Ligar(interactable, "fotografia", asset);

        Assert.AreEqual("Examinar", interactable.GetInteractionText());

        interactable.Interact();

        Assert.IsTrue(interactable.Apanhada);
        Assert.AreEqual(1, fotografia.CapturedPhotos.Count, "não foi para o álbum");
        Assert.AreEqual("estudio-1986", fotografia.CapturedPhotos[0].Id);

        Assert.IsTrue(inspeccao.IsInspecting, "não ficou na mão");
        Assert.IsTrue(inspeccao.TemVerso, "a fotografia na mão não tem verso");
    }

    [Test]
    public void PhotoInteractable_SemFotografiaLigada_QueixaSeENaoEstoira()
    {
        GameObject noMundo = Novo("Fotografia_Vazia");
        PhotoInteractable interactable = noMundo.AddComponent<PhotoInteractable>();

        LogAssert.Expect(
            LogType.Error,
            new System.Text.RegularExpressions.Regex("por ligar no")
        );

        interactable.Interact();

        Assert.IsFalse(interactable.Apanhada);
    }
}
