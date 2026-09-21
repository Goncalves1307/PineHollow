using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

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
        GameObject host = Novo("Player");
        maquina = host.AddComponent<PlayerStateMachine>();

        GameObject painel = Novo("PhotoComparison");
        PhotoComparisonSystem comparacao =
            painel.AddComponent<PhotoComparisonSystem>();

        painel.SetActive(false);

        Ligar(comparacao, "painel", painel);
        Ligar(comparacao, "stateMachine", maquina);

        return comparacao;
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
}
