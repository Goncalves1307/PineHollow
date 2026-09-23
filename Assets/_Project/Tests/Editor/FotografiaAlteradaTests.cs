using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Testes da FASE 8, ronda 1: a fotografia que muda quando o jogador volta de
// 1986 (subtask 869f4yb8z).
//
// O caso que motivou a task é o do meio: o jogador JÁ TEM a fotografia no
// álbum antes do flashback, e ao voltar é essa mesma cópia — não uma nova — que
// tem de mostrar a figura atrás do Thomas. O AddPhoto recusa um id repetido, e
// um desenho que trocasse a cópia em vez de a resolver ao ler deixava o álbum
// a mostrar um passado que já não aconteceu.
//
// O último teste abre a cena montada. É a lição da FASE 7: 257 testes verdes
// provaram o modelo mental de quem os escreveu, e a cena que a montagem produziu
// estava errada. Aqui o que pode partir é o stateId que o flashback escreve não
// ser o que a fotografia lê, e só a cena o diz.
public class FotografiaAlteradaTests
{
    private const string Marca = "flashback-teste";

    private readonly List<Object> criados = new List<Object>();

    private GameObject Novo(string nome)
    {
        GameObject go = new GameObject(nome);
        criados.Add(go);
        return go;
    }

    private Texture2D Textura(int largura = 8, int altura = 4)
    {
        Texture2D textura = new Texture2D(largura, altura);
        criados.Add(textura);
        return textura;
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

        EstadoDoMundo.Esquecer();
        RegistoDeConhecimento.Esquecer();
    }

    private static FieldInfo Campo(object alvo, string nome)
    {
        return alvo.GetType().GetField(nome, BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private static void Ligar(object alvo, string nome, object valor)
    {
        Campo(alvo, nome).SetValue(alvo, valor);
    }

    // Um mundo AUTORADO, como o do Bootstrap. O Existente só o vê depois de
    // Assumir(), que em EditMode ninguém chama por nós.
    private EstadoDoMundo Mundo()
    {
        EstadoDoMundo mundo = Novo("EstadoDoMundo").AddComponent<EstadoDoMundo>();
        mundo.Assumir();
        return mundo;
    }

    private const string JsonDoLago =
        "{\"id\":\"lago\",\"ano\":1986,\"local\":\"Lago\"," +
        "\"personagens\":[\"Thomas Hale\"]," +
        "\"frente\":\"Thomas junto ao lago.\"," +
        "\"verso\":\"Verso da base.\",\"autorada\":true," +
        "\"alinhamentos\":[{\"comId\":\"outra\",\"escalaAlvo\":1," +
        "\"toleranciaPosicao\":0.06,\"toleranciaRotacao\":6,\"toleranciaEscala\":0.1}]," +
        "\"variantes\":[{\"stateId\":\"" + Marca + "\",\"chave\":\"presente\"," +
        "\"valor\":\"true\",\"frente\":\"Atrás dele, uma figura.\"," +
        "\"revelacao\":\"Agora existe uma figura atrás de Thomas.\"}]}";

    private PhotoData Lago(out Texture2D antes, out Texture2D depois, string json = JsonDoLago)
    {
        PhotoData foto = JsonUtility.FromJson<PhotoData>(json);

        antes = Textura();
        depois = Textura();

        Ligar(foto, "imagem", antes);

        foreach (PhotoVariante variante in foto.Variantes)
            Ligar(variante, "imagem", depois);

        return foto;
    }

    // ---------------------------------------------------------------
    // Qual das versões vale
    // ---------------------------------------------------------------

    [Test]
    public void SemMundo_MostraAFotografiaDeOrigem()
    {
        PhotoData foto = Lago(out Texture2D antes, out _);

        Assert.IsNull(EstadoDoMundo.Existente);
        Assert.IsNull(foto.VarianteActiva);
        Assert.AreSame(antes, foto.Imagem);

        Assert.IsNull(
            Object.FindAnyObjectByType<EstadoDoMundo>(),
            "ler uma fotografia criou um EstadoDoMundo na cena — no editor, " +
            "desenhar o inspector de um PhotoAsset punha objectos na cena aberta");
    }

    [Test]
    public void MundoSemAMarca_MostraAFotografiaDeOrigem()
    {
        Mundo();

        PhotoData foto = Lago(out Texture2D antes, out _);

        Assert.AreSame(antes, foto.Imagem);
        Assert.AreEqual("Thomas junto ao lago.", foto.Frente);
    }

    [Test]
    public void ComAMarca_MostraAVarianteEHerdaOQueElaNaoTroca()
    {
        EstadoDoMundo mundo = Mundo();

        PhotoData foto = Lago(out _, out Texture2D depois);

        mundo.Registar(Marca, "presente", true);

        Assert.AreSame(depois, foto.Imagem);
        Assert.AreEqual("Atrás dele, uma figura.", foto.Frente);

        // A variante não traz verso nem pessoas: herda-os da base.
        Assert.AreEqual("Verso da base.", foto.Verso);
        Assert.IsTrue(foto.TemVerso);
        CollectionAssert.AreEqual(new[] { "Thomas Hale" }, foto.Personagens);

        // O protagonista não tem nome, e não ganha uma entrada de pessoa.
        Assert.AreEqual(1, foto.Personagens.Count);
    }

    [Test]
    public void OValorTemDeSerOQueAVarianteEspera()
    {
        EstadoDoMundo mundo = Mundo();

        PhotoData foto = Lago(out Texture2D antes, out _);

        mundo.Registar(Marca, "presente", false);

        Assert.AreSame(antes, foto.Imagem, "«false» não é ter estado lá");
    }

    [Test]
    public void DuasVariantesQueValem_GanhaAUltimaDaLista()
    {
        EstadoDoMundo mundo = Mundo();

        PhotoData foto = JsonUtility.FromJson<PhotoData>(
            "{\"id\":\"x\",\"frente\":\"base\",\"variantes\":[" +
            "{\"stateId\":\"a\",\"chave\":\"k\",\"valor\":\"true\",\"frente\":\"primeira\"}," +
            "{\"stateId\":\"b\",\"chave\":\"k\",\"valor\":\"true\",\"frente\":\"segunda\"}]}");

        mundo.Registar("a", "k", true);
        Assert.AreEqual("primeira", foto.Frente);

        mundo.Registar("b", "k", true);
        Assert.AreEqual("segunda", foto.Frente, "a mais recente é a que o jogador vê");
    }

    [Test]
    public void VarianteSemStateId_NuncaVale()
    {
        EstadoDoMundo mundo = Mundo();

        PhotoData foto = JsonUtility.FromJson<PhotoData>(
            "{\"id\":\"x\",\"frente\":\"base\",\"variantes\":[" +
            "{\"stateId\":\"\",\"chave\":\"presente\",\"valor\":\"\",\"frente\":\"alterada\"}]}");

        // Um Ler com stateId vazio devolve null; um valor vazio autorado não pode
        // bater com isso e fazer a fotografia nascer já alterada.
        mundo.Registar(Marca, "presente", true);

        Assert.AreEqual("base", foto.Frente);
    }

    // ---------------------------------------------------------------
    // O caso da task: a fotografia que o jogador já tinha
    // ---------------------------------------------------------------

    [Test]
    public void AFotografiaQueJaEstavaNoAlbum_MudaSemSerSubstituida()
    {
        EstadoDoMundo mundo = Mundo();

        PhotographySystem album = Novo("Player").AddComponent<PhotographySystem>();

        PhotoData foto = Lago(out Texture2D antes, out Texture2D depois);

        album.AddPhoto(foto);

        Assert.AreSame(antes, album.CapturedPhotos[0].Imagem);

        // O flashback acontece e volta.
        mundo.Registar(Marca, "presente", true);

        Assert.AreEqual(1, album.CapturedPhotos.Count);
        Assert.AreSame(foto, album.CapturedPhotos[0]);
        Assert.AreSame(
            depois, album.CapturedPhotos[0].Imagem,
            "a cópia do álbum ficou a mostrar a fotografia de antes do flashback");
    }

    [Test]
    public void OAlinhamentoSobreviveAVariante()
    {
        EstadoDoMundo mundo = Mundo();

        PhotoData foto = Lago(out _, out _);
        PhotoData outra = JsonUtility.FromJson<PhotoData>("{\"id\":\"outra\"}");

        Assert.IsTrue(foto.AlinhaCom(outra));

        mundo.Registar(Marca, "presente", true);

        Assert.IsTrue(foto.AlinhaCom(outra), "mudar o que mostra não é mudar de fotografia");
        Assert.IsTrue(outra.AlinhaCom(foto));
    }

    [Test]
    public void ACopiaDoAsset_LevaAsVariantesSemPartilharAsDoAsset()
    {
        PhotoAsset asset = ScriptableObject.CreateInstance<PhotoAsset>();
        asset.name = "Fotografia_Lago";
        criados.Add(asset);

        JsonUtility.FromJsonOverwrite("{\"fotografia\":" + JsonDoLago + "}", asset);

        PhotoData primeira = asset.Criar();
        PhotoData segunda = asset.Criar();

        Assert.AreEqual(1, primeira.Variantes.Count);
        Assert.AreEqual(Marca, primeira.Variantes[0].StateId);
        Assert.AreNotSame(primeira.Variantes[0], segunda.Variantes[0]);

        EstadoDoMundo mundo = Mundo();
        mundo.Registar(Marca, "presente", true);

        Assert.AreEqual("Atrás dele, uma figura.", primeira.Frente);
    }

    // ---------------------------------------------------------------
    // O caderno
    // ---------------------------------------------------------------

    [Test]
    public void VerAVersaoAlterada_EntraNoCadernoUmaSoVez()
    {
        EstadoDoMundo mundo = Mundo();
        RegistoDeConhecimento registo = Novo("Registo").AddComponent<RegistoDeConhecimento>();
        registo.Assumir();

        PhotoData foto = Lago(out _, out _);

        FontesDeConhecimento.RegistarFotografia(registo, foto);
        int antes = registo.Total;

        Assert.IsFalse(
            FontesDeConhecimento.RegistarAlteracao(foto),
            "antes do flashback não há nada de novo para registar");

        mundo.Registar(Marca, "presente", true);

        Assert.IsTrue(FontesDeConhecimento.RegistarAlteracao(foto));
        Assert.IsFalse(FontesDeConhecimento.RegistarAlteracao(foto), "ver duas vezes não é aprender duas");

        Assert.AreEqual(antes + 1, registo.Total);

        Conhecimento novo = registo.Conhecidos[registo.Total - 1];

        Assert.AreEqual(CategoriaDeConhecimento.Descoberta, novo.Categoria);
        Assert.AreEqual("Agora existe uma figura atrás de Thomas.", novo.Titulo);

        // A entrada da fotografia não foi reescrita: o jogador lembra-se de
        // como ela era.
        Conhecimento daFotografia = registo.Obter(
            Conhecimento.Identificar(CategoriaDeConhecimento.Fotografia, "lago"));

        Assert.AreEqual("Thomas junto ao lago.", daFotografia.Texto);
    }

    [Test]
    public void UmaFotografiaSemVariante_NaoPoeUmRegistoNaCena()
    {
        PhotoData foto = JsonUtility.FromJson<PhotoData>("{\"id\":\"simples\"}");

        Assert.IsFalse(FontesDeConhecimento.RegistarAlteracao(foto));

        Assert.IsNull(
            Object.FindAnyObjectByType<RegistoDeConhecimento>(),
            "abrir uma fotografia qualquer no viewer criava um registo de emergência");
    }

    [Test]
    public void OViewer_DesenhaAImagemAlteradaERegista()
    {
        EstadoDoMundo mundo = Mundo();
        RegistoDeConhecimento registo = Novo("Registo").AddComponent<RegistoDeConhecimento>();
        registo.Assumir();

        PlayerStateMachine maquina = Novo("Player").AddComponent<PlayerStateMachine>();

        GameObject painel = Novo("PhotoViewer");
        PhotoViewerSystem viewer = painel.AddComponent<PhotoViewerSystem>();

        // A imagem LIGADA: um viewer sem RawImage não desenha, e um teste que
        // não a liga passa com o desenho errado (AGENTS.md, a lição da FASE 5).
        RawImage imagem = Novo("Imagem").AddComponent<RawImage>();

        Ligar(viewer, "photoViewer", painel);
        Ligar(viewer, "photoImage", imagem);
        Ligar(viewer, "stateMachine", maquina);
        painel.SetActive(false);

        PhotoData foto = Lago(out Texture2D antes, out Texture2D depois);

        viewer.Open(foto);
        Assert.AreSame(antes, imagem.texture);
        viewer.Close();

        mundo.Registar(Marca, "presente", true);

        viewer.Open(foto);

        Assert.AreSame(depois, imagem.texture);
        Assert.IsTrue(registo.Sabe(Conhecimento.Identificar(
            CategoriaDeConhecimento.Descoberta, "lago", foto.Variantes[0].Id)));
    }

    // ---------------------------------------------------------------
    // Quem deixa a marca
    // ---------------------------------------------------------------

    private SistemaDeFlashback Flashback(string stateId, out PersonagemHistorica thomas)
    {
        GameObject go = Novo("Thomas");
        thomas = go.AddComponent<PersonagemHistorica>();

        GameObject ponto = Novo("Ponto");
        ponto.transform.position = new Vector3(0f, 0f, 1f);

        Ligar(thomas, "percurso", new[] { ponto.transform });
        Ligar(thomas, "velocidade", 10f);

        FimAoSairDeVista fim = Novo("Fim").AddComponent<FimAoSairDeVista>();
        Ligar(fim, "personagem", thomas);

        SistemaDeFlashback sistema = Novo("Sistema").AddComponent<SistemaDeFlashback>();

        Ligar(sistema, "stateMachine", Novo("Maquina").AddComponent<PlayerStateMachine>());
        Ligar(sistema, "transicao", Novo("Transicao").AddComponent<TransicaoDeEpoca>());
        Ligar(sistema, "fim", fim);
        Ligar(sistema, "stateIdDaPresenca", stateId);

        return sistema;
    }

    private static void Percorrer(SistemaDeFlashback sistema, PersonagemHistorica thomas)
    {
        sistema.Iniciar();
        sistema.Avancar(0.5f);
        sistema.Avancar(2f);
        sistema.Avancar(3f);

        Assert.AreEqual(FaseDoFlashback.Dentro, sistema.Fase);

        thomas.Avancar(1f);
        sistema.Avancar(0.1f);
        sistema.Avancar(2f);
        sistema.Avancar(3f);

        Assert.AreEqual(FaseDoFlashback.Fora, sistema.Fase);
    }

    [Test]
    public void OFlashback_DeixaAMarcaSoAoVoltar()
    {
        EstadoDoMundo mundo = Mundo();

        SistemaDeFlashback sistema = Flashback(Marca, out PersonagemHistorica thomas);

        sistema.Iniciar();
        sistema.Avancar(0.5f);
        sistema.Avancar(2f);

        Assert.AreEqual(Epoca.Passado1986, mundo.EpocaActual);
        Assert.IsFalse(
            mundo.LerBool(Marca, SistemaDeFlashback.ChaveDaPresenca),
            "a fotografia não pode mudar enquanto o jogador ainda está em 1986");

        sistema.Avancar(3f);
        thomas.Avancar(1f);
        sistema.Avancar(0.1f);
        sistema.Avancar(2f);
        sistema.Avancar(3f);

        Assert.IsTrue(mundo.LerBool(Marca, SistemaDeFlashback.ChaveDaPresenca));
    }

    [Test]
    public void OCicloInteiro_MudaAFotografiaQueOJogadorJaTinha()
    {
        Mundo();

        PhotoData foto = Lago(out Texture2D antes, out Texture2D depois);

        Assert.AreSame(antes, foto.Imagem);

        SistemaDeFlashback sistema = Flashback(Marca, out PersonagemHistorica thomas);

        Percorrer(sistema, thomas);

        Assert.AreSame(depois, foto.Imagem);
    }

    [Test]
    public void UmFlashbackSemStateId_NaoEscreveNada()
    {
        EstadoDoMundo mundo = Mundo();

        SistemaDeFlashback sistema = Flashback(string.Empty, out PersonagemHistorica thomas);

        Percorrer(sistema, thomas);

        Assert.AreEqual(0, mundo.Total);
    }

    // ---------------------------------------------------------------
    // A cena que a montagem produziu
    // ---------------------------------------------------------------

    // Deixa uma cena vazia no fim, passe ou falhe. Sem isto a cena do jogo
    // ficava carregada para os testes seguintes — este ficheiro corre antes do
    // HighlightSystemTests e do InteractionSystemTests por ordem alfabética, e
    // os raycasts deles passavam a acertar nas paredes e fotografias da cena:
    // três falhas que não eram deles.
    [Test]
    public void ACenaMontada_OFlashbackEscreveOQueAFotografiaDoLagoLe()
    {
        try
        {
            VerificarACenaMontada();
        }
        finally
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private void VerificarACenaMontada()
    {
        Scene cena = EditorSceneManager.OpenScene(
            "Assets/_Project/Scenes/Prototype_Player.unity",
            OpenSceneMode.Single
        );

        Assert.IsTrue(cena.IsValid());

        SistemaDeFlashback sistema = Object.FindAnyObjectByType<SistemaDeFlashback>(
            FindObjectsInactive.Include);

        Assert.IsNotNull(sistema, "não há SistemaDeFlashback na cena");
        Assert.AreEqual(
            Fase8Montagem.StateIdDoPrimeiroFlashback, sistema.StateIdDaPresenca,
            "o flashback da cena não deixa a marca — corre Fase8Montagem.Correr");

        PhotoInteractable noMundo = null;

        foreach (PhotoInteractable candidato in Object.FindObjectsByType<PhotoInteractable>(
                     FindObjectsInactive.Include))
        {
            if (candidato.name == Fase8Montagem.NomeDaFotografia)
                noMundo = candidato;
        }

        Assert.IsNotNull(noMundo, "a fotografia do lago não está pousada na cena");

        PhotoAsset asset = (PhotoAsset)Campo(noMundo, "fotografia").GetValue(noMundo);

        Assert.IsNotNull(asset, "a fotografia do lago está pousada sem asset");

        // Mundo limpo, e só depois se pede a cópia: o que se prova é a cadeia
        // da cena, não um estado herdado.
        EstadoDoMundo mundo = Mundo();

        PhotoData foto = asset.Criar();

        Texture2D antes = foto.Imagem;

        Assert.IsNotNull(antes, "a fotografia do lago não tem imagem — aparecia em branco");
        Assert.AreEqual(PhotoAsset.LarguraAutorada, antes.width);
        Assert.AreEqual(
            PhotoAsset.AlturaAutorada, antes.height,
            "a imagem foi escalada na importação — perdeu a proporção 3:2");

        // O que o flashback da cena escreve, com as constantes do próprio sistema.
        mundo.Registar(sistema.StateIdDaPresenca, SistemaDeFlashback.ChaveDaPresenca, true);

        Texture2D depois = foto.Imagem;

        Assert.IsNotNull(foto.VarianteActiva, "a marca do flashback não é a que a variante lê");
        Assert.AreNotSame(antes, depois, "a variante vale mas mostra a mesma imagem");
        Assert.AreEqual(antes.width, depois.width);
        Assert.AreEqual(antes.height, depois.height);
        Assert.IsFalse(string.IsNullOrWhiteSpace(foto.VarianteActiva.Revelacao));
    }

    [Test]
    public void AsDuasImagens_SaoDiferentesEDeterministicas()
    {
        byte[] antes = Fase8Montagem.Desenhar(false);
        byte[] depois = Fase8Montagem.Desenhar(true);

        CollectionAssert.AreNotEqual(antes, depois, "a figura não chegou a ser desenhada");
        CollectionAssert.AreEqual(
            antes, Fase8Montagem.Desenhar(false),
            "o desenho não é determinístico — cada montagem sujava o git");

        Assert.IsTrue(
            File.Exists(Fase8Montagem.CaminhoDaImagemDepois),
            "a imagem alterada não está em disco");
    }
}
