using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Testes do quadro de investigação.
//
// A regra dura do GDD — «o board não deve resolver automaticamente todas as
// teorias» — é uma regra de jogo, e uma regra de jogo sem teste é uma intenção.
// Três testes deste ficheiro existem só para a segurar: o quadro não cria
// ligações, não as avalia, e não conta quantas faltam.
public class InvestigationBoardSystemTests
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

        RegistoDeConhecimento.Esquecer();
    }

    private static void Ligar(object alvo, string nome, object valor)
    {
        alvo.GetType()
            .GetField(nome, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(alvo, valor);
    }

    private GameObject Filho(string nome, GameObject pai)
    {
        GameObject go = Novo(nome);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(pai.transform, false);
        return go;
    }

    private InvestigationBoardSystem Montado(
        out PlayerStateMachine maquina,
        out RegistoDeConhecimento registo,
        out TMP_Text aviso)
    {
        registo = Novo("RegistoDeConhecimento")
            .AddComponent<RegistoDeConhecimento>();

        GameObject host = Novo("Player");
        maquina = host.AddComponent<PlayerStateMachine>();

        GameObject anfitriao = Novo("Investigacao");
        InvestigationBoardSystem quadro =
            anfitriao.AddComponent<InvestigationBoardSystem>();

        GameObject painel = Filho("Board", anfitriao);

        GameObject superficie = Filho("Superficie", painel);

        // O cartão leva Image e rótulo de propósito: sem eles o Configurar sai
        // cedo e o quadro passava nos testes sem escrever uma palavra em cima
        // de um papel.
        GameObject modeloDoCartao = Filho("ModeloDoCartao", superficie);
        modeloDoCartao.AddComponent<Image>();

        GameObject rotulo = Filho("Rotulo", modeloDoCartao);
        TMP_Text texto = rotulo.AddComponent<TextMeshProUGUI>();

        CartaoDoQuadro cartao = modeloDoCartao.AddComponent<CartaoDoQuadro>();
        Ligar(cartao, "rotulo", texto);
        Ligar(cartao, "fundo", modeloDoCartao.GetComponent<Image>());

        modeloDoCartao.SetActive(false);

        GameObject modeloDaLigacao = Filho("ModeloDaLigacao", superficie);
        modeloDaLigacao.AddComponent<Image>();
        modeloDaLigacao.SetActive(false);

        aviso = Filho("Aviso", painel).AddComponent<TextMeshProUGUI>();

        Ligar(quadro, "painel", painel);
        Ligar(quadro, "superficie", (RectTransform)superficie.transform);
        Ligar(quadro, "modeloDoCartao", modeloDoCartao);
        Ligar(quadro, "modeloDaLigacao", modeloDaLigacao);
        Ligar(quadro, "aviso", aviso);
        Ligar(quadro, "stateMachine", maquina);

        painel.SetActive(false);

        return quadro;
    }

    private InvestigationBoardSystem Montado(out RegistoDeConhecimento registo)
    {
        return Montado(out _, out registo, out _);
    }

    private static void Saber(RegistoDeConhecimento registo, params string[] nomes)
    {
        foreach (string nome in nomes)
        {
            registo.Registar(
                Conhecimento.Identificar(CategoriaDeConhecimento.Pessoa, nome),
                CategoriaDeConhecimento.Pessoa,
                nome
            );
        }
    }

    private static string Id(string nome)
    {
        return Conhecimento.Identificar(CategoriaDeConhecimento.Pessoa, nome);
    }

    // ---------------------------------------------------------------
    // Abrir e fechar
    // ---------------------------------------------------------------

    [Test]
    public void B_AbreOQuadroELibertaOCursor()
    {
        InvestigationBoardSystem quadro = Montado(out PlayerStateMachine maquina, out _, out _);

        quadro.Alternar();

        Assert.IsTrue(quadro.IsOpen);
        Assert.AreEqual(PlayerState.ViewingBoard, maquina.Current);
        Assert.IsTrue(maquina.IsCursorFree);
    }

    [Test]
    public void Esc_FechaOQuadroSemDeixarOModoOrfao()
    {
        InvestigationBoardSystem quadro = Montado(out PlayerStateMachine maquina, out _, out _);

        quadro.Alternar();

        maquina.BeginFrame();
        maquina.RequestBack();

        Assert.IsTrue(maquina.ConsumeBack(PlayerState.ViewingBoard));

        quadro.Fechar();

        Assert.IsFalse(quadro.IsOpen);
        Assert.AreEqual(0, maquina.OpenModeCount);
    }

    [Test]
    public void B_NaoAbreSobreOutraCamada()
    {
        InvestigationBoardSystem quadro = Montado(out PlayerStateMachine maquina, out _, out _);

        maquina.PushMode(PlayerState.ViewingAlbum);

        quadro.Alternar();

        Assert.IsFalse(quadro.IsOpen);
    }

    // ---------------------------------------------------------------
    // Os cartões
    // ---------------------------------------------------------------

    [Test]
    public void OQuadroFazUmCartaoPorCadaCoisaQueSeSabe()
    {
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale", "Elias Ward");

        quadro.Alternar();

        Assert.AreEqual(2, quadro.Cartoes.Count);
        StringAssert.Contains("Thomas Hale", quadro.Cartoes[0].Rotulo);
    }

    [Test]
    public void DoisCartoesComOMesmoTitulo_DistinguemSePelaCategoria()
    {
        // O caso real: a fotografia de 1986 e a data de 1986 chamam-se ambas
        // «17 de Julho de 1986». No caderno distinguem-se pela gaveta; o quadro
        // mistura as seis categorias e mostrava dois papéis iguais.
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        registo.Registar("fotografia:f", CategoriaDeConhecimento.Fotografia,
            "17 de Julho de 1986");

        registo.Registar("data:1986", CategoriaDeConhecimento.Data,
            "17 de Julho de 1986");

        quadro.Alternar();

        Assert.AreNotEqual(quadro.Cartoes[0].Rotulo, quadro.Cartoes[1].Rotulo);

        StringAssert.Contains("FOTOGRAFIA", quadro.Cartoes[0].Rotulo);
        StringAssert.Contains("DATA", quadro.Cartoes[1].Rotulo);
    }

    [Test]
    public void ReabrirNaoDuplicaOsCartoes()
    {
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale");

        quadro.Alternar();
        quadro.Fechar();
        quadro.Alternar();

        Assert.AreEqual(1, quadro.Cartoes.Count);
    }

    [Test]
    public void OsCartoesNascemEmSitiosDiferentes()
    {
        // Todos na mesma posição davam uma pilha de papéis indistinguível.
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale", "Elias Ward", "Ethan Cole");

        quadro.Alternar();

        Assert.AreNotEqual(quadro.Cartoes[0].Posicao, quadro.Cartoes[1].Posicao);
        Assert.AreNotEqual(quadro.Cartoes[1].Posicao, quadro.Cartoes[2].Posicao);
    }

    [Test]
    public void ArrumarUmCartao_GuardaOndeEleFicou()
    {
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale");

        quadro.Alternar();

        quadro.Mover(Id("Thomas Hale"), new Vector2(120f, -80f));

        Assert.AreEqual(new Vector2(120f, -80f), quadro.Cartao(Id("Thomas Hale")).Posicao);

        // Arrumar é metade do que um quadro destes faz: fechá-lo não pode
        // desfazer o trabalho.
        quadro.Fechar();
        quadro.Alternar();

        Assert.AreEqual(new Vector2(120f, -80f), quadro.Cartao(Id("Thomas Hale")).Posicao);
    }

    // ---------------------------------------------------------------
    // As ligações — e as três invariantes da regra do GDD
    // ---------------------------------------------------------------

    [Test]
    public void OQuadroNaoCriaLigacaoNenhumaSozinho()
    {
        // Invariante 1. Saber cinco coisas relacionadas não faz aparecer uma
        // única linha: as ligações são do jogador.
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale", "Elias Ward", "Ethan Cole");

        registo.Registar("data:1986", CategoriaDeConhecimento.Data, "1986");
        registo.Registar("data:1994", CategoriaDeConhecimento.Data, "1994");

        quadro.Alternar();

        Assert.AreEqual(5, quadro.Cartoes.Count);
        Assert.AreEqual(0, quadro.Ligacoes.Count);
    }

    [Test]
    public void UmaLigacaoNaoTemOndeGuardarUmVeredicto()
    {
        // Invariante 2. O GDD proíbe o quadro de validar a hipótese certa. Não
        // é uma questão de não o fazer: não há onde o escrever. Este teste parte
        // no instante em que alguém acrescentar um bool «correcta».
        foreach (FieldInfo campo in typeof(LigacaoDoQuadro).GetFields(
                     BindingFlags.Instance | BindingFlags.Public |
                     BindingFlags.NonPublic))
        {
            Assert.AreEqual(
                typeof(string),
                campo.FieldType,
                "LigacaoDoQuadro.{0} não é uma string: o quadro passou a ter " +
                "onde guardar um juízo sobre uma ligação, e o GDD proíbe-o.",
                campo.Name
            );
        }
    }

    [Test]
    public void OAvisoNaoDizQuantasLigacoesFaltam()
    {
        // Invariante 3. Um «3 de 8» é o checklist excessivo que a task manda
        // evitar — e é a conclusão por outras palavras.
        InvestigationBoardSystem quadro = Montado(out _, out RegistoDeConhecimento registo, out TMP_Text aviso);

        quadro.Alternar();

        Assert.IsFalse(TemAlgarismo(aviso.text), aviso.text);

        Saber(registo, "Thomas Hale", "Elias Ward");

        quadro.Reconstruir();

        quadro.AlternarLigacao(Id("Thomas Hale"), Id("Elias Ward"));

        Assert.IsFalse(TemAlgarismo(aviso.text), aviso.text);
    }

    private static bool TemAlgarismo(string texto)
    {
        foreach (char c in texto)
        {
            if (char.IsDigit(c))
                return true;
        }

        return false;
    }

    [Test]
    public void LigarDuasVezes_DesfazALigacao()
    {
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale", "Elias Ward");

        quadro.Alternar();

        Assert.IsTrue(quadro.AlternarLigacao(Id("Thomas Hale"), Id("Elias Ward")));
        Assert.AreEqual(1, quadro.Ligacoes.Count);

        Assert.IsFalse(quadro.AlternarLigacao(Id("Thomas Hale"), Id("Elias Ward")));
        Assert.AreEqual(0, quadro.Ligacoes.Count);
    }

    [Test]
    public void ALigacaoNaoTemSentido()
    {
        // Ligar A a B é o mesmo que ligar B a A. Uma seta dizia ao jogador qual
        // das duas causa a outra — que é a conclusão que ele tem de tirar.
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale", "Elias Ward");

        quadro.Alternar();

        quadro.AlternarLigacao(Id("Thomas Hale"), Id("Elias Ward"));

        Assert.IsTrue(quadro.EstaLigado(Id("Elias Ward"), Id("Thomas Hale")));
    }

    [Test]
    public void UmCartaoNaoSeLigaASiProprio()
    {
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale");

        quadro.Alternar();

        Assert.IsFalse(quadro.AlternarLigacao(Id("Thomas Hale"), Id("Thomas Hale")));
        Assert.AreEqual(0, quadro.Ligacoes.Count);
    }

    [Test]
    public void MarcarDoisCartoes_FazALigacao()
    {
        // O gesto é o do álbum: marcar a segunda miniatura abre a comparação,
        // marcar o segundo cartão faz a ligação. Não se inventou um botão.
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale", "Elias Ward");

        quadro.Alternar();

        quadro.AlternarMarcacao(Id("Thomas Hale"));

        Assert.AreEqual(1, quadro.Marcados.Count);
        Assert.AreEqual(0, quadro.Ligacoes.Count);

        quadro.AlternarMarcacao(Id("Elias Ward"));

        Assert.AreEqual(0, quadro.Marcados.Count);
        Assert.IsTrue(quadro.EstaLigado(Id("Thomas Hale"), Id("Elias Ward")));
    }

    [Test]
    public void MarcarOMesmoCartaoDuasVezes_Desmarca()
    {
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale");

        quadro.Alternar();

        quadro.AlternarMarcacao(Id("Thomas Hale"));
        quadro.AlternarMarcacao(Id("Thomas Hale"));

        Assert.AreEqual(0, quadro.Marcados.Count);
        Assert.AreEqual(0, quadro.Ligacoes.Count);
    }

    [Test]
    public void AsLigacoesSobrevivemAFecharOQuadro()
    {
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale", "Elias Ward");

        quadro.Alternar();
        quadro.AlternarLigacao(Id("Thomas Hale"), Id("Elias Ward"));

        quadro.Fechar();
        quadro.Alternar();

        Assert.IsTrue(quadro.EstaLigado(Id("Thomas Hale"), Id("Elias Ward")));
    }

    [Test]
    public void AbrirOQuadro_NaoDeixaMarcacoesDaVezAnterior()
    {
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale", "Elias Ward");

        quadro.Alternar();
        quadro.AlternarMarcacao(Id("Thomas Hale"));

        quadro.Fechar();
        quadro.Alternar();

        // Sem isto, marcar um cartão na sessão seguinte ligava-o ao que tinha
        // ficado marcado da anterior — é o mesmo erro que a selecção do álbum
        // teve de corrigir.
        Assert.AreEqual(0, quadro.Marcados.Count);
    }

    // ---------------------------------------------------------------
    // O desenho das linhas
    // ---------------------------------------------------------------

    [Test]
    public void ColocarLinha_UneOsDoisPontos()
    {
        // Aritmética pura, sem rato: uma linha que só se verifica em play mode
        // é uma linha por verificar.
        GameObject go = Novo("Linha");
        RectTransform linha = go.AddComponent<RectTransform>();

        InvestigationBoardSystem.ColocarLinha(
            linha,
            new Vector2(-100f, 0f),
            new Vector2(100f, 0f),
            3f
        );

        Assert.AreEqual(Vector2.zero, linha.anchoredPosition);
        Assert.AreEqual(200f, linha.sizeDelta.x, 0.001f);
        Assert.AreEqual(3f, linha.sizeDelta.y, 0.001f);
        Assert.AreEqual(0f, linha.localRotation.eulerAngles.z, 0.001f);
    }

    [Test]
    public void ColocarLinha_RodaParaOAlvo()
    {
        GameObject go = Novo("Linha");
        RectTransform linha = go.AddComponent<RectTransform>();

        InvestigationBoardSystem.ColocarLinha(
            linha,
            Vector2.zero,
            new Vector2(0f, 100f),
            3f
        );

        Assert.AreEqual(new Vector2(0f, 50f), linha.anchoredPosition);
        Assert.AreEqual(100f, linha.sizeDelta.x, 0.001f);
        Assert.AreEqual(90f, linha.localRotation.eulerAngles.z, 0.001f);
    }

    [Test]
    public void UmaLigacaoDesenhaUmaLinha()
    {
        InvestigationBoardSystem quadro = Montado(out RegistoDeConhecimento registo);

        Saber(registo, "Thomas Hale", "Elias Ward");

        quadro.Alternar();

        int antes = Linhas(quadro);

        quadro.AlternarLigacao(Id("Thomas Hale"), Id("Elias Ward"));

        Assert.AreEqual(antes + 1, Linhas(quadro));
    }

    private static int Linhas(InvestigationBoardSystem quadro)
    {
        List<RectTransform> linhas = (List<RectTransform>)typeof(InvestigationBoardSystem)
            .GetField("linhas", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(quadro);

        return linhas.Count;
    }
}
