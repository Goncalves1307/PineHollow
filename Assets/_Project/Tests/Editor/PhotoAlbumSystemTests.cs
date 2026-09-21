using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Testes do álbum e da comparação.
//
// O caso real que motivou isto não é o caminho feliz: é que o álbum estava
// escrito, ligado na cena e completamente inalcançável. O OpenAlbum() não
// tinha um único chamador, o painel nascia desligado e o Tab só tinha o ramo
// de fechar — todo o caminho álbum → miniatura → viewer estava morto em play
// mode, e nada o denunciava porque nenhum teste lhe tocava.
//
// Por isso o primeiro teste aqui é «o Tab abre mesmo», e o segundo é «o Esc
// fecha sem deixar o modo órfão na pilha», que era a outra metade da avaria.
public class PhotoAlbumSystemTests
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

    private static FieldInfo Campo(object alvo, string nome)
    {
        return alvo.GetType().GetField(
            nome,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
    }

    private static void Ligar(object alvo, string nome, object valor)
    {
        Campo(alvo, nome).SetValue(alvo, valor);
    }

    private static PhotoData Foto(string json)
    {
        return JsonUtility.FromJson<PhotoData>(json);
    }

    // ---------------------------------------------------------------
    // Montagem
    // ---------------------------------------------------------------

    private PhotographySystem SistemaMontado(
        out PlayerStateMachine maquina,
        out PhotoAlbumSystem album,
        out GameObject painelDoAlbum,
        out PhotoComparisonSystem comparacao)
    {
        GameObject host = Novo("Player");
        maquina = host.AddComponent<PlayerStateMachine>();

        PhotographySystem fotografia = host.AddComponent<PhotographySystem>();

        painelDoAlbum = Novo("PhotoAlbum");
        album = painelDoAlbum.AddComponent<PhotoAlbumSystem>();

        GameObject lista = Novo("PhotoList");
        lista.transform.SetParent(painelDoAlbum.transform);

        GameObject prefab = Novo("PhotoThumbnail");
        prefab.AddComponent<PhotoThumbnail>();

        GameObject painelDaComparacao = Novo("PhotoComparison");
        comparacao = painelDaComparacao.AddComponent<PhotoComparisonSystem>();
        painelDaComparacao.SetActive(false);

        Ligar(comparacao, "painel", painelDaComparacao);
        Ligar(comparacao, "stateMachine", maquina);

        Ligar(album, "photographySystem", fotografia);
        Ligar(album, "photoList", lista.transform);
        Ligar(album, "photoThumbnailPrefab", prefab);
        Ligar(album, "stateMachine", maquina);
        Ligar(album, "photoComparisonSystem", comparacao);

        Ligar(fotografia, "photoAlbum", painelDoAlbum);
        Ligar(fotografia, "photoAlbumSystem", album);
        Ligar(fotografia, "photoCamera", Novo("CameraBody"));
        Ligar(fotografia, "photoPreview", Novo("PhotoPreview"));
        Ligar(fotografia, "stateMachine", maquina);

        painelDoAlbum.SetActive(false);

        return fotografia;
    }

    // ---------------------------------------------------------------
    // O caso real: o Tab estava morto
    // ---------------------------------------------------------------

    [Test]
    public void Tab_AbreOAlbumEEmpurraOModo()
    {
        PhotographySystem fotografia = SistemaMontado(
            out PlayerStateMachine maquina,
            out PhotoAlbumSystem album,
            out GameObject painel,
            out _
        );

        Assert.IsFalse(painel.activeSelf, "o painel começa desligado, como na cena");

        fotografia.TogglePhotoAlbum();

        Assert.IsTrue(painel.activeSelf, "o Tab não ligou o painel");
        Assert.IsTrue(album.IsOpen, "o álbum não abriu");
        Assert.IsTrue(
            maquina.IsTopMode(PlayerState.ViewingAlbum),
            "o ViewingAlbum não foi empurrado"
        );
    }

    [Test]
    public void Tab_AbreOPainelAntesDeChamarOpenAlbum()
    {
        // O PhotoAlbumSystem vive dentro do painel: se o OpenAlbum corresse
        // com o GameObject ainda desligado, o Update dele não corria e o
        // ViewingAlbum ficava na pilha sem ninguém a consumir o Esc. O jogo
        // só saía disso pela saída de emergência da máquina de estados.
        PhotographySystem fotografia = SistemaMontado(
            out PlayerStateMachine maquina,
            out PhotoAlbumSystem album,
            out GameObject painel,
            out _
        );

        fotografia.TogglePhotoAlbum();

        Assert.IsTrue(
            painel.activeInHierarchy,
            "o painel tem de estar activo para o Update do álbum correr"
        );

        // A prova de que o Esc chega a alguém: com o painel activo, o Update
        // do álbum corre e consome.
        maquina.BeginFrame();
        maquina.RequestBack();

        Assert.IsTrue(
            maquina.ConsumeBack(PlayerState.ViewingAlbum),
            "o modo no topo não é o do álbum — ninguém fecharia isto"
        );
    }

    [Test]
    public void Tab_ComOAlbumAberto_Fecha()
    {
        PhotographySystem fotografia = SistemaMontado(
            out PlayerStateMachine maquina,
            out PhotoAlbumSystem album,
            out GameObject painel,
            out _
        );

        fotografia.TogglePhotoAlbum();
        fotografia.TogglePhotoAlbum();

        Assert.IsFalse(painel.activeSelf);
        Assert.IsFalse(album.IsOpen);
        Assert.AreEqual(
            0,
            maquina.OpenModeCount,
            "fechar o álbum deixou um modo na pilha"
        );
    }

    [Test]
    public void Tab_NaoAbreOAlbumPorCimaDeOutraCamada()
    {
        // O álbum solta o cursor. Aberto por cima de uma inspecção, o objecto
        // ficava na mão por trás do painel.
        PhotographySystem fotografia = SistemaMontado(
            out PlayerStateMachine maquina,
            out PhotoAlbumSystem album,
            out GameObject painel,
            out _
        );

        maquina.PushMode(PlayerState.Inspecting);

        fotografia.TogglePhotoAlbum();

        Assert.IsFalse(painel.activeSelf, "o álbum abriu por cima da inspecção");
        Assert.IsFalse(album.IsOpen);
        Assert.IsTrue(maquina.IsTopMode(PlayerState.Inspecting));
    }

    // ---------------------------------------------------------------
    // O álbum fala PhotoData
    // ---------------------------------------------------------------

    [Test]
    public void Album_MostraUmaMiniaturaPorFotografia()
    {
        PhotographySystem fotografia = SistemaMontado(
            out _,
            out PhotoAlbumSystem album,
            out _,
            out _
        );

        fotografia.AddPhoto(Foto("{\"id\":\"a\",\"ano\":1986}"));
        fotografia.AddPhoto(Foto("{\"id\":\"b\",\"ano\":1994}"));

        fotografia.TogglePhotoAlbum();

        Transform lista = (Transform)Campo(album, "photoList").GetValue(album);

        Assert.AreEqual(2, lista.childCount);
    }

    [Test]
    public void AddPhoto_NaoGuardaAMesmaFotografiaAutoradaDuasVezes()
    {
        // Apanhar a mesma fotografia outra vez não são duas fotografias — e o
        // PhotoInteractable deixa-a no mundo depois de examinada.
        PhotographySystem fotografia = SistemaMontado(out _, out _, out _, out _);

        fotografia.AddPhoto(AutoradaComId("estudio-1986"));
        fotografia.AddPhoto(AutoradaComId("estudio-1986"));

        Assert.AreEqual(1, fotografia.CapturedPhotos.Count);
    }

    [Test]
    public void AddPhoto_GuardaCadaCapturaMesmoSendoIguais()
    {
        // As capturas têm id próprio cada uma: duas fotografias tiradas da
        // mesma parede continuam a ser duas fotografias.
        PhotographySystem fotografia = SistemaMontado(out _, out _, out _, out _);

        fotografia.AddPhoto(PhotoData.DeCaptura(null, 2026, "Agora"));
        fotografia.AddPhoto(PhotoData.DeCaptura(null, 2026, "Agora"));

        Assert.AreEqual(2, fotografia.CapturedPhotos.Count);
    }

    [Test]
    public void AddPhoto_IgnoraNull()
    {
        PhotographySystem fotografia = SistemaMontado(out _, out _, out _, out _);

        fotografia.AddPhoto(null);

        Assert.AreEqual(0, fotografia.CapturedPhotos.Count);
    }

    private static PhotoData AutoradaComId(string id)
    {
        PhotoAsset asset = ScriptableObject.CreateInstance<PhotoAsset>();
        asset.name = id;

        try
        {
            return asset.Criar();
        }
        finally
        {
            Object.DestroyImmediate(asset);
        }
    }

    // ---------------------------------------------------------------
    // Selecção e comparação
    // ---------------------------------------------------------------

    [Test]
    public void Seleccionar_DuasFotografias_AbreAComparacao()
    {
        PhotographySystem fotografia = SistemaMontado(
            out PlayerStateMachine maquina,
            out PhotoAlbumSystem album,
            out _,
            out PhotoComparisonSystem comparacao
        );

        PhotoData a = Foto("{\"id\":\"a\",\"ano\":1986}");
        PhotoData b = Foto("{\"id\":\"b\",\"ano\":1994}");

        fotografia.AddPhoto(a);
        fotografia.AddPhoto(b);
        fotografia.TogglePhotoAlbum();

        album.AlternarSeleccao(a);

        Assert.AreSame(null, comparacao.Fixa, "uma só não chega para comparar");

        album.AlternarSeleccao(b);

        Assert.AreSame(a, comparacao.Fixa);
        Assert.AreSame(b, comparacao.Movel);
        Assert.IsTrue(maquina.IsTopMode(PlayerState.ComparingPhotos));
    }

    [Test]
    public void Seleccionar_AMesmaDuasVezes_Desmarca()
    {
        PhotographySystem fotografia = SistemaMontado(
            out _,
            out PhotoAlbumSystem album,
            out _,
            out PhotoComparisonSystem comparacao
        );

        PhotoData a = Foto("{\"id\":\"a\"}");

        fotografia.AddPhoto(a);
        fotografia.TogglePhotoAlbum();

        album.AlternarSeleccao(a);
        album.AlternarSeleccao(a);

        Assert.AreEqual(0, album.Seleccionadas.Count);
        Assert.IsNull(comparacao.Fixa);
    }

    [Test]
    public void Seleccionar_UmaTerceira_TrocaAMaisAntigaEmVezDeIgnorar()
    {
        PhotographySystem fotografia = SistemaMontado(
            out _,
            out PhotoAlbumSystem album,
            out _,
            out PhotoComparisonSystem comparacao
        );

        PhotoData a = Foto("{\"id\":\"a\"}");
        PhotoData b = Foto("{\"id\":\"b\"}");
        PhotoData c = Foto("{\"id\":\"c\"}");

        fotografia.AddPhoto(a);
        fotografia.AddPhoto(b);
        fotografia.AddPhoto(c);
        fotografia.TogglePhotoAlbum();

        album.AlternarSeleccao(a);
        album.AlternarSeleccao(b);
        album.AlternarSeleccao(c);

        Assert.AreEqual(2, album.Seleccionadas.Count);
        Assert.AreSame(b, album.Seleccionadas[0]);
        Assert.AreSame(c, album.Seleccionadas[1]);
    }

    [Test]
    public void FecharOAlbum_LimpaASeleccao()
    {
        PhotographySystem fotografia = SistemaMontado(
            out _,
            out PhotoAlbumSystem album,
            out _,
            out _
        );

        PhotoData a = Foto("{\"id\":\"a\"}");

        fotografia.AddPhoto(a);
        fotografia.TogglePhotoAlbum();

        album.AlternarSeleccao(a);
        fotografia.TogglePhotoAlbum();

        Assert.AreEqual(0, album.Seleccionadas.Count);
    }
}
