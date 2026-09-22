using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// Testes da comparação e do alinhamento — o que decide se o jogador
// descobriu ou não.
//
// A manipulação (mover, rodar, escalar) e a detecção são métodos públicos sem
// input lá dentro exactamente para poderem ser testados: em EditMode o
// Mouse.current é null, e uma regra de jogo que só corra em play mode não
// tem verificação nenhuma.
public class PhotoComparisonSystemTests
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

        // A FASE 6 pendurou um registo de conhecimento nestas fontes: apanhar
        // uma fotografia, ler um documento e alinhar passaram a escrever nele.
        // Sem isto, cada teste deixava para trás um registo criado de
        // emergência e o teste seguinte encontrava a cena suja.
        RegistoDeConhecimento.Esquecer();
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

    // Um par que alinha quando a móvel está sobreposta ao centro, sem rodar e
    // à mesma escala — o caso mais simples e o mais comum.
    private static PhotoData Fixa(string comId)
    {
        return Foto(
            "{\"id\":\"fixa\",\"ano\":1986,\"alinhamentos\":[{" +
            "\"comId\":\"" + comId + "\",\"escalaAlvo\":1," +
            "\"toleranciaPosicao\":0.05,\"toleranciaRotacao\":5," +
            "\"toleranciaEscala\":0.08}]}"
        );
    }

    private PhotoComparisonSystem Montado(out PlayerStateMachine maquina)
    {
        return Montado(out maquina, out _, out _, out _);
    }

    // As imagens ligadas de propósito: sem elas o Desenhar e o
    // AplicarTransformacao saíam na primeira linha e nunca corriam em teste —
    // foi por aí que passou o defeito de a fotografia fixa nunca ir ao centro.
    private PhotoComparisonSystem Montado(
        out PlayerStateMachine maquina,
        out RectTransform fixa,
        out RectTransform movel,
        out Image flash)
    {
        GameObject host = Novo("Player");
        maquina = host.AddComponent<PlayerStateMachine>();

        GameObject painel = Novo("PhotoComparison");
        PhotoComparisonSystem comparacao =
            painel.AddComponent<PhotoComparisonSystem>();

        RawImage imagemFixa = NovaImagem("Fixa", painel, new Vector2(-260f, 40f));
        RawImage imagemMovel = NovaImagem("Movel", painel, new Vector2(260f, 40f));

        fixa = imagemFixa.rectTransform;
        movel = imagemMovel.rectTransform;

        GameObject alvoDoFlash = Novo("Flash");
        alvoDoFlash.transform.SetParent(painel.transform, false);
        flash = alvoDoFlash.AddComponent<Image>();
        alvoDoFlash.SetActive(false);

        painel.SetActive(false);

        Ligar(comparacao, "painel", painel);
        Ligar(comparacao, "imagemFixa", imagemFixa);
        Ligar(comparacao, "imagemMovel", imagemMovel);
        Ligar(comparacao, "flash", flash);
        Ligar(comparacao, "stateMachine", maquina);

        return comparacao;
    }

    private RawImage NovaImagem(string nome, GameObject pai, Vector2 posicao)
    {
        GameObject go = Novo(nome);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(pai.transform, false);

        RawImage imagem = go.AddComponent<RawImage>();

        RectTransform rect = imagem.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(480f, 320f);
        rect.anchoredPosition = posicao;

        return imagem;
    }

    // ---------------------------------------------------------------
    // Abrir e fechar
    // ---------------------------------------------------------------

    [Test]
    public void Abrir_ComecaLadoALadoEEmpurraOModo()
    {
        PhotoComparisonSystem comparacao = Montado(out PlayerStateMachine maquina);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\",\"ano\":1994}"));

        // Sobrepor logo escondia uma das duas e dava a ideia errada de que o
        // jogo já tinha feito o trabalho.
        Assert.AreEqual(ModoDeComparacao.LadoALado, comparacao.Modo);
        Assert.IsTrue(maquina.IsTopMode(PlayerState.ComparingPhotos));
        Assert.AreEqual(1f, comparacao.Escala, 1e-4f);
        Assert.AreEqual(Vector2.zero, comparacao.Posicao);
    }

    [Test]
    public void Abrir_ComUmaNull_NaoAbreNemSujaAPilha()
    {
        PhotoComparisonSystem comparacao = Montado(out PlayerStateMachine maquina);

        comparacao.Abrir(Fixa("movel"), null);

        Assert.IsNull(comparacao.Fixa);
        Assert.AreEqual(0, maquina.OpenModeCount);
    }

    [Test]
    public void Fechar_TiraOModoDaPilha()
    {
        PhotoComparisonSystem comparacao = Montado(out PlayerStateMachine maquina);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.Fechar();

        Assert.AreEqual(0, maquina.OpenModeCount);
        Assert.IsNull(comparacao.Fixa);
    }

    [Test]
    public void AComparacaoLibertaOCursor()
    {
        // Alinhar é arrastar com o rato: sem cursor livre não há como lá
        // chegar. E o cursor livre tranca movimento e câmara.
        PhotoComparisonSystem comparacao = Montado(out PlayerStateMachine maquina);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));

        Assert.IsTrue(maquina.IsCursorFree);
        Assert.IsTrue(maquina.IsMovementLocked);
        Assert.IsTrue(maquina.IsLookLocked);
    }

    // ---------------------------------------------------------------
    // O alinhamento
    // ---------------------------------------------------------------

    [Test]
    public void LadoALado_NuncaAlinha()
    {
        // Alinhar é pôr uma sobre a outra. Aceitar em lado a lado dava o
        // gatilho de graça mal se abrisse a comparação de um par declarado.
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));

        Assert.IsFalse(comparacao.VerificarAlinhamento());
        Assert.IsFalse(comparacao.Alinhada);
    }

    [Test]
    public void Sobreposicao_NoSitioCerto_Alinha()
    {
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();

        Assert.IsTrue(comparacao.Alinhada, "sobrepostas e certas, e não alinhou");
    }

    [Test]
    public void Sobreposicao_ForaDaTolerancia_NaoAlinha()
    {
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();
        comparacao.Mover(new Vector2(0.4f, 0f));

        Assert.IsFalse(comparacao.Alinhada);
    }

    [Test]
    public void Sobreposicao_DentroDaTolerancia_AlinhaSemSerExacto()
    {
        // O jogador tem de encontrar a sobreposição com a mão. Exigir
        // coincidência ao pixel era exigir o impossível com um rato.
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();
        comparacao.Mover(new Vector2(0.03f, 0.02f));
        comparacao.Rodar(3f);

        Assert.IsTrue(comparacao.Alinhada);
    }

    [Test]
    public void ParNaoDeclarado_NuncaAlinhaPorMaisCertoQueEsteja()
    {
        // Sem par declarado não há alinhamento, mesmo com as duas fotografias
        // exactamente uma sobre a outra.
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(
            Foto("{\"id\":\"fixa\"}"),
            Foto("{\"id\":\"movel\"}")
        );

        comparacao.AlternarModo();

        Assert.IsFalse(comparacao.Alinhada);
    }

    [Test]
    public void Alinhar_SoDaOGatilhoUmaVez()
    {
        // Sem isto, cada pixel que o rato andasse dentro da tolerância
        // disparava outra vez — e o flashback da FASE 7 com ele.
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();

        Assert.IsFalse(
            comparacao.VerificarAlinhamento(),
            "já tinha alinhado ao mudar de modo"
        );

        comparacao.Mover(new Vector2(0.01f, 0f));

        Assert.IsFalse(
            comparacao.VerificarAlinhamento(),
            "continuar dentro da tolerância não é alinhar outra vez"
        );
    }

    [Test]
    public void Alinhar_VoltaAContarDepoisDeSairEEntrar()
    {
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();

        comparacao.Mover(new Vector2(0.5f, 0f));
        Assert.IsFalse(comparacao.Alinhada);

        comparacao.Mover(new Vector2(-0.5f, 0f));

        Assert.IsTrue(comparacao.Alinhada);
    }

    [Test]
    public void ConsumirAlinhamento_DevolveTrueUmaSoVez()
    {
        // É o gancho da FASE 7, à maneira do ConsumeBack: quem o consumir é
        // quem faz o FLASH e leva o jogador a 1986.
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();

        Assert.IsTrue(comparacao.ConsumirAlinhamento());
        Assert.IsFalse(comparacao.ConsumirAlinhamento());
    }

    [Test]
    public void SemAlinhar_NaoHaGatilhoParaConsumir()
    {
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(
            Foto("{\"id\":\"fixa\"}"),
            Foto("{\"id\":\"movel\"}")
        );

        comparacao.AlternarModo();

        Assert.IsFalse(comparacao.ConsumirAlinhamento());
    }

    // ---------------------------------------------------------------
    // Descobertas
    // ---------------------------------------------------------------

    [Test]
    public void Alinhar_RevelaSoAsDescobertasDaquelePar()
    {
        PhotoData fixa = Foto(
            "{\"id\":\"fixa\",\"alinhamentos\":[{\"comId\":\"movel\"," +
            "\"escalaAlvo\":1,\"toleranciaPosicao\":0.05," +
            "\"toleranciaRotacao\":5,\"toleranciaEscala\":0.08}]," +
            "\"descobertas\":[" +
            "{\"id\":\"porta\",\"tipo\":2,\"revelaAoAlinharCom\":\"movel\"}," +
            "{\"id\":\"outra\",\"tipo\":0,\"revelaAoAlinharCom\":\"terceira\"}," +
            "{\"id\":\"sozinha\",\"tipo\":4}]}"
        );

        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(fixa, Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();

        Assert.AreEqual(1, comparacao.DescobertasReveladas.Count);
        Assert.AreEqual("porta", comparacao.DescobertasReveladas[0].Id);
        Assert.AreEqual(
            TipoDeDescoberta.Porta,
            comparacao.DescobertasReveladas[0].Tipo
        );
    }

    [Test]
    public void Alinhar_RevelaTambemOQueEstaNaFotografiaMovel()
    {
        PhotoData movel = Foto(
            "{\"id\":\"movel\",\"descobertas\":[" +
            "{\"id\":\"sombra\",\"tipo\":3,\"revelaAoAlinharCom\":\"fixa\"}]}"
        );

        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), movel);
        comparacao.AlternarModo();

        Assert.AreEqual(1, comparacao.DescobertasReveladas.Count);
        Assert.AreEqual("sombra", comparacao.DescobertasReveladas[0].Id);
    }

    [Test]
    public void SemAlinhar_NaoRevelaNada()
    {
        PhotoData fixa = Foto(
            "{\"id\":\"fixa\",\"descobertas\":[" +
            "{\"id\":\"porta\",\"revelaAoAlinharCom\":\"movel\"}]}"
        );

        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(fixa, Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();

        Assert.AreEqual(0, comparacao.DescobertasReveladas.Count);
        Assert.IsFalse(fixa.Descobertas[0].Revelada);
    }

    // ---------------------------------------------------------------
    // Limites da manipulação
    // ---------------------------------------------------------------

    [Test]
    public void Escala_NaoPassaDosLimites()
    {
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();

        comparacao.Escalar(100f);
        Assert.AreEqual(4f, comparacao.Escala, 1e-4f);

        comparacao.Escalar(-100f);
        Assert.AreEqual(0.25f, comparacao.Escala, 1e-4f, "escala zero ou negativa vira a imagem");
    }

    [Test]
    public void Posicao_NaoFogeDoEcra()
    {
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();

        comparacao.Mover(new Vector2(50f, -50f));

        Assert.AreEqual(2f, comparacao.Posicao.x, 1e-4f);
        Assert.AreEqual(-2f, comparacao.Posicao.y, 1e-4f);
    }

    [Test]
    public void Rotacao_MantemSeEntreMenos180E180()
    {
        // Sem isto, rodar sempre no mesmo sentido fazia o valor crescer sem
        // fim e a comparação com o alvo deixava de bater.
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();

        comparacao.Rodar(370f);

        Assert.AreEqual(10f, comparacao.Rotacao, 1e-3f);
        Assert.LessOrEqual(Mathf.Abs(comparacao.Rotacao), 180f);

        comparacao.Rodar(-30f);

        Assert.AreEqual(-20f, comparacao.Rotacao, 1e-3f);
    }

    [Test]
    public void AlternarModo_VaiEVoltaSemPerderOQueOJogadorMexeu()
    {
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();
        comparacao.Mover(new Vector2(0.3f, 0.1f));

        comparacao.AlternarModo();
        Assert.AreEqual(ModoDeComparacao.LadoALado, comparacao.Modo);

        comparacao.AlternarModo();

        Assert.AreEqual(ModoDeComparacao.Sobreposicao, comparacao.Modo);
        Assert.AreEqual(0.3f, comparacao.Posicao.x, 1e-4f, "perdeu o que o jogador tinha feito");
    }

    // ---------------------------------------------------------------
    // Onde as fotografias ficam no ecrã — o defeito que os testes
    // antigos não podiam apanhar, porque não ligavam as imagens
    // ---------------------------------------------------------------

    [Test]
    public void Sobrepor_PoeAFixaNoCentro()
    {
        // Sem isto a fixa ficava onde a vista de arrumação a deixou, e a
        // Posicao da móvel passava a ser medida a partir do centro do painel
        // em vez de a partir da fotografia com que se quer alinhar: o jogo
        // dava Alinhada = true com as duas imagens mais de meia largura
        // afastadas.
        PhotoComparisonSystem comparacao = Montado(
            out _,
            out RectTransform fixa,
            out RectTransform movel,
            out _
        );

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));

        Assert.AreNotEqual(Vector2.zero, fixa.anchoredPosition, "lado a lado é ao lado");

        comparacao.AlternarModo();

        Assert.AreEqual(Vector2.zero, fixa.anchoredPosition);
        Assert.AreEqual(
            Vector2.zero,
            movel.anchoredPosition,
            "com Posicao zero as duas têm de coincidir no ecrã"
        );
    }

    [Test]
    public void Sobrepor_ADistanciaNoEcraSegueAPosicaoNormalizada()
    {
        PhotoComparisonSystem comparacao = Montado(
            out _,
            out RectTransform fixa,
            out RectTransform movel,
            out _
        );

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();
        comparacao.Mover(new Vector2(0.5f, 0.25f));

        // Normalizado à LARGURA nos dois eixos: o espaço é isotrópico, senão
        // rodar em normalizado não era rodar em píxeis.
        float largura = fixa.rect.width;

        Assert.AreEqual(largura * 0.5f, movel.anchoredPosition.x, 0.5f);
        Assert.AreEqual(largura * 0.25f, movel.anchoredPosition.y, 0.5f);
    }

    [Test]
    public void LadoALado_NaoAcumulaOQueOJogadorArrasta()
    {
        // Antes o arrasto não mexia nada no ecrã mas os valores acumulavam à
        // mesma, e ao sobrepor a fotografia saltava para uma posição que o
        // jogador nunca viu.
        PhotoComparisonSystem comparacao = Montado(out _);

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));

        comparacao.Mover(new Vector2(0.5f, 0.5f));
        comparacao.Rodar(45f);
        comparacao.Escalar(1f);

        Assert.AreEqual(Vector2.zero, comparacao.Posicao);
        Assert.AreEqual(0f, comparacao.Rotacao, 1e-4f);
        Assert.AreEqual(1f, comparacao.Escala, 1e-4f);
    }

    // ---------------------------------------------------------------
    // O FLASH
    // ---------------------------------------------------------------

    [Test]
    public void Alinhar_AcendeOClarao()
    {
        // O GDD nunca diz ao jogador que duas fotografias alinham — mas
        // quando ele o descobre, o jogo tem de o confirmar.
        PhotoComparisonSystem comparacao = Montado(
            out _,
            out _,
            out _,
            out Image flash
        );

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));

        Assert.AreEqual(0f, comparacao.IntensidadeDoFlash, 1e-4f);

        comparacao.AlternarModo();

        Assert.AreEqual(1f, comparacao.IntensidadeDoFlash, 1e-4f);
        Assert.IsTrue(flash.gameObject.activeSelf);
        Assert.AreEqual(1f, flash.color.a, 1e-4f);
    }

    [Test]
    public void OClarao_ApagaSeEDesligaOObjecto()
    {
        // Um Image transparente a ecrã inteiro continua a apanhar o rato: se
        // ficasse ligado, tapava as miniaturas por baixo.
        PhotoComparisonSystem comparacao = Montado(
            out _,
            out _,
            out _,
            out Image flash
        );

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));
        comparacao.AlternarModo();

        comparacao.AvancarFlash(0.2f);
        Assert.Less(comparacao.IntensidadeDoFlash, 1f);
        Assert.Greater(comparacao.IntensidadeDoFlash, 0f);

        comparacao.AvancarFlash(5f);

        Assert.AreEqual(0f, comparacao.IntensidadeDoFlash, 1e-4f);
        Assert.IsFalse(flash.gameObject.activeSelf);
    }

    [Test]
    public void SemAlinhar_NaoHaClarao()
    {
        PhotoComparisonSystem comparacao = Montado(
            out _,
            out _,
            out _,
            out Image flash
        );

        comparacao.Abrir(
            Foto("{\"id\":\"fixa\"}"),
            Foto("{\"id\":\"movel\"}")
        );

        comparacao.AlternarModo();

        Assert.AreEqual(0f, comparacao.IntensidadeDoFlash, 1e-4f);
        Assert.IsFalse(flash.gameObject.activeSelf);
    }

    [Test]
    public void Abrir_SemPainelLigado_NaoEmpurraOModo()
    {
        // O IsOpen é lido do painel: sem ele o Update sai sempre cedo e
        // ninguém consome o Esc — cursor solto e movimento trancado até a
        // saída de emergência da máquina de estados.
        GameObject host = Novo("Player");
        PlayerStateMachine maquina = host.AddComponent<PlayerStateMachine>();

        PhotoComparisonSystem comparacao =
            Novo("Orfa").AddComponent<PhotoComparisonSystem>();

        Ligar(comparacao, "stateMachine", maquina);

        UnityEngine.TestTools.LogAssert.Expect(
            LogType.Error,
            new System.Text.RegularExpressions.Regex("painel da comparação por ligar")
        );

        comparacao.Abrir(Fixa("movel"), Foto("{\"id\":\"movel\"}"));

        Assert.AreEqual(0, maquina.OpenModeCount);
    }
}
