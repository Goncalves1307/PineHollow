using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Testes da FASE 8, ronda 4: o conteúdo do slice é o do capítulo 1.
//
// Abrem a cena montada e os assets, e não um JSON escrito aqui: o que se
// protege é o que a montagem produziu. A FASE 7 mostrou que o modelo mental de
// quem escreve os testes e a cena podem divergir sem nenhum vermelho.
//
// Cada teste deixa uma cena vazia no fim — corre antes de outros que fazem
// raycasts, e a cena do jogo carregada fazia-os acertar nela.
public class ConteudoCapitulo1Tests
{
    private const string CenaPath = "Assets/_Project/Scenes/Prototype_Player.unity";

    // Os versos canónicos de outras fotografias (Historia:420-431 e seguintes).
    // Nenhum pertence ao capítulo 1, e o slice não os pode usar.
    private static readonly string[] VersosDeOutrosCapitulos =
    {
        "Ele ainda não chegou.",
        "Agora és tu.",
        "Não reveles todas.",
        "Thomas não é o único.",
        "É aqui que começa."
    };

    private readonly List<GameObject> criados = new List<GameObject>();

    [SetUp]
    public void AbrirCena()
    {
        EditorSceneManager.OpenScene(CenaPath, OpenSceneMode.Single);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in criados)
        {
            if (go != null)
                Object.DestroyImmediate(go);
        }

        criados.Clear();

        EstadoDoMundo.Esquecer();
        RegistoDeConhecimento.Esquecer();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static object Ler(object alvo, string campo)
    {
        return alvo.GetType()
            .GetField(campo, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(alvo);
    }

    private static Dictionary<string, PhotoData> FotografiasDaCena()
    {
        Dictionary<string, PhotoData> todas = new Dictionary<string, PhotoData>();

        foreach (PhotoInteractable noMundo in Object.FindObjectsByType<PhotoInteractable>(
                     FindObjectsInactive.Include))
        {
            PhotoAsset asset = (PhotoAsset)Ler(noMundo, "fotografia");

            Assert.IsNotNull(asset, noMundo.name + " está pousada sem asset");

            // Um duplicado colapsava na mesma chave e o teste ficava verde com
            // duas fotografias sobrepostas na cena — que é o que uma migração
            // corrida pela ordem errada produz.
            Assert.IsFalse(todas.ContainsKey(noMundo.name), "há duas " + noMundo.name + " na cena");

            todas[noMundo.name] = asset.Criar();
        }

        return todas;
    }

    private static Dictionary<string, DocumentInteractable> DocumentosDaCena()
    {
        Dictionary<string, DocumentInteractable> todos = new Dictionary<string, DocumentInteractable>();

        foreach (DocumentInteractable documento in Object.FindObjectsByType<DocumentInteractable>(
                     FindObjectsInactive.Include))
        {
            string id = (string)Ler(documento, "stateId");

            // Um Instantiate repetido dava dois obituários com o mesmo id.
            Assert.IsFalse(todos.ContainsKey(id), "há dois documentos com o stateId " + id);

            todos[id] = documento;
        }

        return todos;
    }

    [Test]
    public void ACena_TemAsTresFotografiasDoCapitulo1()
    {
        Dictionary<string, PhotoData> fotos = FotografiasDaCena();

        CollectionAssert.AreEquivalent(
            new[]
            {
                Fase8Montagem.NomeDaCidade1986,
                Fase8Montagem.NomeDaFotografia,
                Fase8Montagem.NomeDoLago1994
            },
            fotos.Keys,
            "a cena tem fotografias a mais ou a menos — sobrou alguma «Estudio»?");

        foreach (KeyValuePair<string, PhotoData> par in fotos)
        {
            Assert.AreEqual(
                par.Key, par.Value.Id,
                "o objecto e o id da fotografia divergem: o stateId do save ficava a apontar a outra");
        }
    }

    [Test]
    public void NenhumaFotografia_TemVersoOuDataForaDoCanone()
    {
        foreach (PhotoData foto in FotografiasDaCena().Values)
        {
            CollectionAssert.Contains(PhotoData.AnosCanonicos, foto.Ano, foto.Id);

            // Só o ano: nenhuma destas está datada nos documentos.
            Assert.AreEqual(foto.Ano.ToString(), foto.Data, foto.Id + " tem uma data inventada");

            Assert.IsFalse(foto.TemVerso, foto.Id + " tem verso — nenhuma do capítulo 1 tem");

            Assert.IsNotNull(foto.ImagemBase, foto.Id + " não tem imagem — aparecia em branco");
            Assert.AreEqual(PhotoAsset.LarguraAutorada, foto.ImagemBase.width, foto.Id);
            Assert.AreEqual(PhotoAsset.AlturaAutorada, foto.ImagemBase.height, foto.Id);
        }
    }

    [Test]
    public void AFotografiaDaCidade_NaoDizQuemEOHomem()
    {
        PhotoData cidade = FotografiasDaCena()[Fase8Montagem.NomeDaCidade1986];

        // «O jogador ainda não sabe quem ele é.» (Historia:211). Com o Thomas em
        // personagens, o caderno escrevia-lhe o nome no instante em que a apanha.
        Assert.AreEqual(0, cidade.Personagens.Count);
        Assert.AreEqual(1986, cidade.Ano);
    }

    [Test]
    public void OParDoLago_AlinhaEmZeroERevelaQueThomasNaoMudou()
    {
        Dictionary<string, PhotoData> fotos = FotografiasDaCena();

        PhotoData de1986 = fotos[Fase8Montagem.NomeDaFotografia];
        PhotoData de1994 = fotos[Fase8Montagem.NomeDoLago1994];

        PhotoAlignment alinhamento = de1986.AlinhamentoPara(de1994);

        Assert.IsNotNull(alinhamento, "o par do lago não alinha — não há como chegar a 1986");
        Assert.IsTrue(
            alinhamento.Coincide(Vector2.zero, 0f, 1f),
            "as duas imagens são o mesmo enquadramento: sobrepostas sem mexer têm de alinhar");

        // Lido do outro lado também, que é como o jogador o faz metade das vezes.
        Assert.IsNotNull(de1994.AlinhamentoPara(de1986));

        // A cidade não alinha com nada: não é ela que leva ao flashback.
        Assert.IsFalse(fotos[Fase8Montagem.NomeDaCidade1986].AlinhaCom(de1994));
        Assert.IsFalse(fotos[Fase8Montagem.NomeDaCidade1986].AlinhaCom(de1986));

        PhotoDiscovery descoberta = null;

        foreach (PhotoDiscovery candidata in de1986.Descobertas)
        {
            if (candidata.SaiDoAlinhamentoCom(de1994))
                descoberta = candidata;
        }

        Assert.IsNotNull(descoberta, "alinhar o par não revela nada");
        Assert.AreEqual(Fase8Montagem.DescobertaDoLago, descoberta.Id);
        Assert.AreEqual("Thomas não mudou.", descoberta.Descricao, "canon à letra, Historia:221");
    }

    [Test]
    public void AFotografiaQueMuda_EUmaDoParQueLevaAoFlashback()
    {
        Dictionary<string, PhotoData> fotos = FotografiasDaCena();

        PhotoData de1986 = fotos[Fase8Montagem.NomeDaFotografia];

        Assert.IsTrue(de1986.AlinhaCom(fotos[Fase8Montagem.NomeDoLago1994]));
        Assert.AreEqual(1, de1986.Variantes.Count);
        Assert.AreEqual(Fase8Montagem.StateIdDoPrimeiroFlashback, de1986.Variantes[0].StateId);
    }

    [Test]
    public void AsImagensDoLago_TemOThomasNoMesmoPixel()
    {
        // Se o Thomas mudasse de sítio entre 1986 e 1994, o alinhamento
        // declarado em (0,0) deixava de ser o que o jogador vê ao sobrepor.
        Texture2D a = new Texture2D(2, 2);
        Texture2D b = new Texture2D(2, 2);

        try
        {
            a.LoadImage(Fase8Montagem.Desenhar(Fase8Montagem.Cena.LagoAntes));
            b.LoadImage(Fase8Montagem.Desenhar(Fase8Montagem.Cena.Lago1994));

            // O tronco do Thomas: centro u=0.60, a meio da altura dele.
            int x = Mathf.RoundToInt(0.60f * a.width);
            int y = Mathf.RoundToInt((0.10f + 0.34f * 0.6f) * a.height);

            Color noThomas1986 = a.GetPixel(x, y);
            Color noThomas1994 = b.GetPixel(x, y);

            Assert.Less(noThomas1986.grayscale, 0.3f, "o Thomas não está onde devia em 1986");
            Assert.Less(noThomas1994.grayscale, 0.3f, "o Thomas não está no mesmo sítio em 1994");

            // Mas não é a mesma imagem: a água mudou de cor.
            int xa = Mathf.RoundToInt(0.2f * a.width);
            int ya = Mathf.RoundToInt(0.3f * a.height);

            Assert.AreNotEqual(a.GetPixel(xa, ya), b.GetPixel(xa, ya), "1994 saiu igual a 1986");
        }
        finally
        {
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }

    [Test]
    public void ANotaDoEthan_NaoUsaVersosDeOutrasFotografias()
    {
        Dictionary<string, DocumentInteractable> documentos = DocumentosDaCena();

        Assert.IsTrue(documentos.ContainsKey(Fase8Montagem.StateIdDaNotaDoEthan));

        string corpo = (string)Ler(documentos[Fase8Montagem.StateIdDaNotaDoEthan], "body");

        foreach (string verso in VersosDeOutrosCapitulos)
            StringAssert.DoesNotContain(verso, corpo);

        StringAssert.StartsWith(
            Fase8Montagem.MarcaDeProvisorio, corpo,
            "texto provisório sem a marca sobrevive até ao jogo sair");
    }

    [Test]
    public void OObituario_EstaNaCenaComOIdQueACadeiaEspera()
    {
        Dictionary<string, DocumentInteractable> documentos = DocumentosDaCena();

        Assert.IsTrue(
            documentos.ContainsKey(Fase8Montagem.StateIdDoObituario),
            "o elo «Thomas morreu em 1986» nunca acende sem este documento");

        string corpo = (string)Ler(documentos[Fase8Montagem.StateIdDoObituario], "body");

        StringAssert.Contains("1986", corpo);
        StringAssert.StartsWith(Fase8Montagem.MarcaDeProvisorio, corpo);
    }

    // O contrato entre a cadeia da FASE 6 e o conteúdo: os elos que o slice
    // promete acender têm de apontar para ids que a cena produz. Mudar um nome
    // de fotografia sem mudar a cadeia apagava um elo sem vermelho nenhum.
    [Test]
    public void ACadeia_AcendeComOQueACenaTem()
    {
        CadeiaDeConhecimento cadeia = AssetDatabase.LoadAssetAtPath<CadeiaDeConhecimento>(
            "Assets/_Project/Investigation/CadeiaDeConhecimento.asset");

        Assert.IsNotNull(cadeia);

        HashSet<string> exigidos = new HashSet<string>();

        foreach (EloDaCadeia elo in cadeia.Elos)
            exigidos.Add(elo.ExigeId);

        string[] doSlice =
        {
            Conhecimento.Identificar(CategoriaDeConhecimento.Pessoa, "Thomas Hale"),
            Conhecimento.Identificar(CategoriaDeConhecimento.Documento, Fase8Montagem.StateIdDoObituario),
            Conhecimento.Identificar(CategoriaDeConhecimento.Fotografia, Fase8Montagem.NomeDoLago1994),
            Conhecimento.Identificar(
                CategoriaDeConhecimento.Descoberta,
                Fase8Montagem.NomeDaFotografia,
                Fase8Montagem.DescobertaDoLago)
        };

        foreach (string id in doSlice)
            Assert.IsTrue(exigidos.Contains(id), "a cadeia não espera por " + id + " — corre Fase6Montagem.Correr");
    }
}
