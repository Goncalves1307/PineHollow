using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Testes do alicerce da FASE 5. O que se protege aqui é a razão de o PhotoData
// existir: uma Texture2D não tem data, local, pessoas nem verso, e sem esses
// campos a comparação e o alinhamento não têm sobre o que trabalhar.
//
// Os PhotoData são montados por JsonUtility porque os campos são privados e
// [SerializeField] — é exactamente o caminho que o Unity usa para os
// desserializar de um asset, e evita abrir uma API só para os testes.
public class PhotoDataTests
{
    private readonly List<Object> criados = new List<Object>();

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

    private static PhotoData DeJson(string json)
    {
        return JsonUtility.FromJson<PhotoData>(json);
    }

    private PhotoAsset AssetComJson(string nome, string jsonDaFotografia)
    {
        PhotoAsset asset = ScriptableObject.CreateInstance<PhotoAsset>();
        asset.name = nome;
        criados.Add(asset);

        JsonUtility.FromJsonOverwrite(
            "{\"fotografia\":" + jsonDaFotografia + "}",
            asset
        );

        return asset;
    }

    // ---------------------------------------------------------------
    // Os campos que a Texture2D não tinha
    // ---------------------------------------------------------------

    [Test]
    public void Fotografia_CarregaOsCamposQueAChecklistPede()
    {
        PhotoData foto = DeJson(
            "{\"id\":\"estudio-1986\",\"ano\":1986," +
            "\"data\":\"17 de Julho de 1986, 23:42\"," +
            "\"local\":\"Estúdio do Ethan\"," +
            "\"personagens\":[\"Thomas Hale\",\"Elias Ward\"]," +
            "\"frente\":\"Dois homens à porta da fábrica.\"," +
            "\"verso\":\"Ele ainda não chegou.\"," +
            "\"metadados\":[{\"chave\":\"Película\",\"valor\":\"Kodak Gold\"}]}"
        );

        Assert.AreEqual("estudio-1986", foto.Id);
        Assert.AreEqual(1986, foto.Ano);
        Assert.AreEqual("17 de Julho de 1986, 23:42", foto.Data);
        Assert.AreEqual("Estúdio do Ethan", foto.Local);
        Assert.AreEqual(2, foto.Personagens.Count);
        Assert.AreEqual("Thomas Hale", foto.Personagens[0]);
        Assert.AreEqual("Dois homens à porta da fábrica.", foto.Frente);
        Assert.AreEqual("Ele ainda não chegou.", foto.Verso);
        Assert.AreEqual(1, foto.Metadados.Count);
        Assert.AreEqual("Kodak Gold", foto.Metadados[0].Valor);
    }

    [Test]
    public void Fotografia_SemListasLigadas_DevolveVazioENaoNull()
    {
        // Um array por ligar no inspector chega aqui como null. Quem itera não
        // tem de se defender disso.
        PhotoData foto = DeJson("{\"id\":\"vazia\"}");

        Assert.IsNotNull(foto.Personagens);
        Assert.IsNotNull(foto.Alinhamentos);
        Assert.AreEqual(0, foto.Personagens.Count);
        Assert.AreEqual(0, foto.Alinhamentos.Count);
    }

    [Test]
    public void Fotografia_SoTemVersoSeLaEstiverEscritoAlgumaCoisa()
    {
        Assert.IsTrue(DeJson("{\"verso\":\"Agora és tu.\"}").TemVerso);
        Assert.IsFalse(DeJson("{\"verso\":\"\"}").TemVerso);
        Assert.IsFalse(DeJson("{\"verso\":\"   \"}").TemVerso, "espaços não são verso");
        Assert.IsFalse(DeJson("{\"id\":\"x\"}").TemVerso, "sem campo não é verso");
    }

    // ---------------------------------------------------------------
    // Encontradas vs tiradas — a distinção que a task pede
    // ---------------------------------------------------------------

    [Test]
    public void Captura_NasceNaoAutoradaESemVerso()
    {
        PhotoData foto = PhotoData.DeCaptura(null, 2026, "Hoje");

        Assert.IsFalse(foto.Autorada, "a fotografia tirada pelo jogador não é autorada");
        Assert.IsFalse(foto.TemVerso, "ninguém escreveu nas costas desta");
        Assert.AreEqual(2026, foto.Ano);
        Assert.IsNotEmpty(foto.Id, "sem id não é referenciável pelo save");
    }

    [Test]
    public void Captura_DaIdDiferenteACadaFotografia()
    {
        PhotoData primeira = PhotoData.DeCaptura(null, 2026, "Hoje");
        PhotoData segunda = PhotoData.DeCaptura(null, 2026, "Hoje");

        Assert.AreNotEqual(primeira.Id, segunda.Id);
    }

    [Test]
    public void Asset_MarcaAFotografiaComoAutorada()
    {
        PhotoAsset asset = AssetComJson(
            "Fotografia_Estudio",
            "{\"ano\":1986,\"verso\":\"É aqui que começa.\"}"
        );

        PhotoData foto = asset.Criar();

        Assert.IsTrue(foto.Autorada);
        Assert.IsTrue(foto.TemVerso);
    }

    [Test]
    public void Asset_SemIdEscrito_UsaONomeDoAsset()
    {
        // Quem autora não devia ter de escrever o nome duas vezes — e uma
        // fotografia sem id não é referenciável pelo alinhamento.
        PhotoAsset asset = AssetComJson("Fotografia_Fabrica_1986", "{\"ano\":1986}");

        Assert.AreEqual("Fotografia_Fabrica_1986", asset.Criar().Id);
    }

    [Test]
    public void Asset_ComIdEscrito_RespeitaOQueLaEsta()
    {
        PhotoAsset asset = AssetComJson(
            "Fotografia_Fabrica_1986",
            "{\"id\":\"fabrica-1986\",\"ano\":1986}"
        );

        Assert.AreEqual("fabrica-1986", asset.Criar().Id);
    }

    // ---------------------------------------------------------------
    // O caso real: o asset é autoria, não é estado
    // ---------------------------------------------------------------

    [Test]
    public void Asset_EntregaCopiaEUmaDescobertaRevelada_NaoSujaOAsset()
    {
        PhotoAsset asset = AssetComJson(
            "Fotografia_Porta",
            "{\"id\":\"porta-1986\",\"ano\":1986," +
            "\"descobertas\":[{\"id\":\"porta\",\"tipo\":2," +
            "\"descricao\":\"Uma porta que em 2026 não está lá.\"}]}"
        );

        PhotoData primeiraSessao = asset.Criar();

        Assert.AreEqual(1, primeiraSessao.Descobertas.Count);
        Assert.IsTrue(primeiraSessao.Descobertas[0].Revelar());
        Assert.IsTrue(primeiraSessao.Descobertas[0].Revelada);

        // Se o asset partilhasse a instância, esta já vinha revelada — dentro
        // do editor a descoberta aparecia feita na sessão seguinte, e no build
        // não persistia de todo.
        PhotoData segundaSessao = asset.Criar();

        Assert.IsFalse(
            segundaSessao.Descobertas[0].Revelada,
            "revelar em jogo escreveu no ScriptableObject"
        );

        Assert.AreNotSame(primeiraSessao, segundaSessao);
        Assert.AreNotSame(
            primeiraSessao.Descobertas[0],
            segundaSessao.Descobertas[0]
        );
    }

    [Test]
    public void Asset_CopiaOsMetadadosEmVezDeOsPartilhar()
    {
        PhotoAsset asset = AssetComJson(
            "Fotografia_Lago",
            "{\"id\":\"lago-1971\",\"ano\":1971," +
            "\"metadados\":[{\"chave\":\"Máquina\",\"valor\":\"Pentax\"}]}"
        );

        PhotoData primeira = asset.Criar();
        PhotoData segunda = asset.Criar();

        Assert.AreEqual("Pentax", primeira.Metadados[0].Valor);
        Assert.AreNotSame(primeira.Metadados[0], segunda.Metadados[0]);
    }

    [Test]
    public void Descoberta_SoSeRevelaUmaVez()
    {
        PhotoAsset asset = AssetComJson(
            "Fotografia_Sombra",
            "{\"id\":\"sombra\",\"descobertas\":[{\"id\":\"s\",\"tipo\":3}]}"
        );

        PhotoDiscovery descoberta = asset.Criar().Descobertas[0];

        Assert.IsTrue(descoberta.Revelar(), "a primeira vez conta");
        Assert.IsFalse(descoberta.Revelar(), "a segunda não volta a avisar");
    }

    // ---------------------------------------------------------------
    // Alinhamento declarado
    // ---------------------------------------------------------------

    private static PhotoData ComAlinhamento(
        string id,
        string comId,
        float x = 0f,
        float y = 0f,
        float rotacao = 0f,
        float escala = 1f)
    {
        return DeJson(
            "{\"id\":\"" + id + "\",\"alinhamentos\":[{" +
            "\"comId\":\"" + comId + "\"," +
            "\"posicaoAlvo\":{\"x\":" + x + ",\"y\":" + y + "}," +
            "\"rotacaoAlvo\":" + rotacao + "," +
            "\"escalaAlvo\":" + escala + "," +
            "\"toleranciaPosicao\":0.05,\"toleranciaRotacao\":5," +
            "\"toleranciaEscala\":0.08}]}"
        );
    }

    [Test]
    public void Alinhamento_ValeNosDoisSentidosComUmaSoDeclaracao()
    {
        PhotoData mil986 = ComAlinhamento("a", "b");
        PhotoData mil994 = DeJson("{\"id\":\"b\"}");

        Assert.IsTrue(mil986.AlinhaCom(mil994), "quem declara alinha");

        Assert.IsTrue(
            mil994.AlinhaCom(mil986),
            "e o par também, sem ter de repetir a declaração"
        );
    }

    [Test]
    public void Alinhamento_NaoInventaParesNemConsigoPropria()
    {
        PhotoData a = ComAlinhamento("a", "b");
        PhotoData c = DeJson("{\"id\":\"c\"}");

        Assert.IsFalse(a.AlinhaCom(c));
        Assert.IsFalse(a.AlinhaCom(a), "uma fotografia não alinha consigo");
        Assert.IsFalse(a.AlinhaCom(null));
    }

    [Test]
    public void Alinhamento_FotografiaSemIdNaoEAlvoDeclaravel()
    {
        // Sem id não há como lhe apontar — e um "" a bater com outro "" daria
        // alinhamentos entre todas as fotografias por preencher.
        PhotoData a = ComAlinhamento("a", "");
        PhotoData semId = DeJson("{\"ano\":1986}");

        Assert.IsFalse(a.AlinhaCom(semId));
    }

    [Test]
    public void Alinhamento_LidoDoLadoDeclarado_DaOAlvoTalQual()
    {
        PhotoData fixa = ComAlinhamento("fixa", "movel", x: 0.2f, rotacao: 30f, escala: 2f);
        PhotoData movel = DeJson("{\"id\":\"movel\"}");

        PhotoAlignment alvo = fixa.AlinhamentoPara(movel);

        Assert.IsNotNull(alvo);
        Assert.AreEqual(0.2f, alvo.PosicaoAlvo.x, 1e-4f);
        Assert.AreEqual(30f, alvo.RotacaoAlvo, 1e-4f);
        Assert.AreEqual(2f, alvo.EscalaAlvo, 1e-4f);
    }

    [Test]
    public void Alinhamento_LidoDoOutroLado_VemInvertidoENaoIgual()
    {
        // O mesmo par visto ao contrário: se o alvo viesse tal qual, a
        // fotografia tinha de ser posta no sítio errado para alinhar.
        PhotoData movel = ComAlinhamento("movel", "fixa", x: 0.2f, rotacao: 30f, escala: 2f);
        PhotoData fixa = DeJson("{\"id\":\"fixa\"}");

        PhotoAlignment alvo = fixa.AlinhamentoPara(movel);

        Assert.IsNotNull(alvo);
        Assert.AreEqual(-30f, alvo.RotacaoAlvo, 1e-4f);
        Assert.AreEqual(0.5f, alvo.EscalaAlvo, 1e-4f, "a escala inversa é a recíproca");
        Assert.AreNotEqual(0.2f, alvo.PosicaoAlvo.x, "a translação não se inverte só com o sinal");
    }

    [Test]
    public void Alinhamento_InverterDuasVezes_VoltaAoOriginal()
    {
        // A garantia de que a inversa está certa: aplicá-la duas vezes tem de
        // devolver o alvo de partida.
        PhotoData fixa = ComAlinhamento("fixa", "movel", x: 0.2f, y: -0.1f, rotacao: 30f, escala: 2f);
        PhotoData movel = DeJson("{\"id\":\"movel\"}");

        PhotoAlignment original = fixa.AlinhamentoPara(movel);
        PhotoAlignment voltaEMeia = original.Inverter("x").Inverter("movel");

        Assert.AreEqual(original.PosicaoAlvo.x, voltaEMeia.PosicaoAlvo.x, 1e-3f);
        Assert.AreEqual(original.PosicaoAlvo.y, voltaEMeia.PosicaoAlvo.y, 1e-3f);
        Assert.AreEqual(original.RotacaoAlvo, voltaEMeia.RotacaoAlvo, 1e-3f);
        Assert.AreEqual(original.EscalaAlvo, voltaEMeia.EscalaAlvo, 1e-3f);
    }

    [Test]
    public void Coincide_AceitaDentroDaToleranciaERecusaForaDela()
    {
        PhotoData fixa = ComAlinhamento("fixa", "movel", x: 0.2f, rotacao: 30f, escala: 1f);
        PhotoAlignment alvo = fixa.AlinhamentoPara(DeJson("{\"id\":\"movel\"}"));

        Assert.IsTrue(alvo.Coincide(new Vector2(0.22f, 0.02f), 32f, 1.03f), "dentro da tolerância");
        Assert.IsFalse(alvo.Coincide(new Vector2(0.4f, 0f), 30f, 1f), "posição fora");
        Assert.IsFalse(alvo.Coincide(new Vector2(0.2f, 0f), 50f, 1f), "rotação fora");
        Assert.IsFalse(alvo.Coincide(new Vector2(0.2f, 0f), 30f, 1.5f), "escala fora");
    }

    [Test]
    public void Coincide_TrataOAnguloComoCirculoENaoComoRecta()
    {
        // 359° e 1° estão a 2° um do outro. Sem isto, uma fotografia rodada
        // quase até dar a volta nunca alinhava.
        PhotoData fixa = ComAlinhamento("fixa", "movel", rotacao: 1f);
        PhotoAlignment alvo = fixa.AlinhamentoPara(DeJson("{\"id\":\"movel\"}"));

        Assert.IsTrue(alvo.Coincide(Vector2.zero, -3f, 1f));
        Assert.AreEqual(2f, PhotoAlignment.DiferencaAngular(359f, 1f), 1e-4f);
    }

    [Test]
    public void Descoberta_SaiDoAlinhamentoQueForDeclarado()
    {
        PhotoAsset asset = AssetComJson(
            "Fotografia_Fabrica",
            "{\"id\":\"fabrica\",\"descobertas\":[" +
            "{\"id\":\"d1\",\"revelaAoAlinharCom\":\"lago\"}," +
            "{\"id\":\"d2\"}]}"
        );

        PhotoData foto = asset.Criar();
        PhotoData lago = DeJson("{\"id\":\"lago\"}");
        PhotoData outra = DeJson("{\"id\":\"outra\"}");

        Assert.IsTrue(foto.Descobertas[0].SaiDoAlinhamentoCom(lago));
        Assert.IsFalse(foto.Descobertas[0].SaiDoAlinhamentoCom(outra));

        Assert.IsFalse(
            foto.Descobertas[1].SaiDoAlinhamentoCom(lago),
            "sem par declarado, esta sai de examinar e não de alinhar"
        );
    }

    [Test]
    public void Asset_CopiaOsAlinhamentosEmVezDeOsPartilhar()
    {
        PhotoAsset asset = AssetComJson(
            "Fotografia_Par",
            "{\"id\":\"par\",\"alinhamentos\":[{\"comId\":\"outro\",\"escalaAlvo\":1}]}"
        );

        PhotoData primeira = asset.Criar();
        PhotoData segunda = asset.Criar();

        Assert.AreEqual(1, primeira.Alinhamentos.Count);
        Assert.AreEqual("outro", primeira.Alinhamentos[0].ComId);
        Assert.AreNotSame(primeira.Alinhamentos[0], segunda.Alinhamentos[0]);
    }

    // ---------------------------------------------------------------
    // Canon
    // ---------------------------------------------------------------

    [Test]
    public void AnosCanonicos_SaoOsNoveDoGdd()
    {
        CollectionAssert.AreEqual(
            new[] { 1946, 1958, 1971, 1986, 1994, 2001, 2011, 2016, 2026 },
            PhotoData.AnosCanonicos
        );
    }
}
