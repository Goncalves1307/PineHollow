using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Testes do registo de conhecimento e das três fontes que o enchem.
//
// O caso real que motivou a FASE 6 não é «faltam dois ecrãs»: é que não havia
// registo nenhum, e as três coisas que o jogo já produzia não desaguavam em
// lado nenhum. A DescobertasReveladas da comparação era limpa a cada
// alinhamento, o HasBeenRead do documento não era lido por ninguém, e apanhar
// uma fotografia só a punha no álbum.
//
// Por isso o teste que aqui importa mais é o do fim: a descoberta tem de
// sobreviver ao alinhamento seguinte.
public class ConhecimentoTests
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

        // Sem isto um teste herdava o registo do anterior e passava por razões
        // que não são as suas.
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

    private RegistoDeConhecimento Registo()
    {
        return Novo("RegistoDeConhecimento").AddComponent<RegistoDeConhecimento>();
    }

    // ---------------------------------------------------------------
    // O identificador
    // ---------------------------------------------------------------

    [Test]
    public void Identificar_JuntaACategoriaEAsPartesEmMinusculas()
    {
        Assert.AreEqual(
            "pessoa:thomas-hale",
            Conhecimento.Identificar(CategoriaDeConhecimento.Pessoa, "Thomas Hale")
        );
    }

    [Test]
    public void Identificar_MesmoNomeEscritoDeDuasManeirasDaAMesmaChave()
    {
        // Duas fotografias autoradas por pessoas diferentes não podem produzir
        // duas entradas para o mesmo Thomas Hale.
        Assert.AreEqual(
            Conhecimento.Identificar(CategoriaDeConhecimento.Pessoa, "Thomas Hale"),
            Conhecimento.Identificar(CategoriaDeConhecimento.Pessoa, "  thomas   hale ")
        );
    }

    [Test]
    public void Identificar_DescobertasHomonimasEmFotografiasDiferentesNaoColidem()
    {
        // O PhotoDiscovery.Id é único DENTRO de uma fotografia e mais nada — o
        // Fase5Montagem escreve "porta-lateral" em todas as que cria. Sem a
        // fotografia na chave, a segunda porta lateral silenciava a primeira.
        string primeira = Conhecimento.Identificar(
            CategoriaDeConhecimento.Descoberta, "Foto_1986", "porta-lateral");

        string segunda = Conhecimento.Identificar(
            CategoriaDeConhecimento.Descoberta, "Foto_2016", "porta-lateral");

        Assert.AreNotEqual(primeira, segunda);
    }

    [Test]
    public void Identificar_ComUmaParteEmBranco_NaoDaIdNenhum()
    {
        // Saltar a parte em branco encurtava o id em silêncio: duas descobertas
        // sem `id` na mesma fotografia davam ambas `descoberta:<foto>` e a
        // segunda era engolida pelo Registar sem uma linha no log.
        Assert.AreEqual(
            string.Empty,
            Conhecimento.Identificar(
                CategoriaDeConhecimento.Descoberta, "Foto_1986", "   ")
        );
    }

    [Test]
    public void Chave_NaoDependeDaFormaUnicodeDoTexto()
    {
        // «Estúdio» escrito no editor vem composto; colado de um documento ou
        // vindo de macOS vem decomposto, e o acento separado não é letra nem
        // algarismo — virava hífen e dava uma segunda entrada para o mesmo
        // sítio.
        string composto = "Estúdio do Ethan";
        string decomposto = "Estúdio do Ethan";

        Assert.AreNotEqual(composto, decomposto, "as duas strings têm de ser diferentes");

        Assert.AreEqual(
            Conhecimento.Identificar(CategoriaDeConhecimento.Local, composto),
            Conhecimento.Identificar(CategoriaDeConhecimento.Local, decomposto)
        );
    }

    // ---------------------------------------------------------------
    // O registo
    // ---------------------------------------------------------------

    [Test]
    public void Registar_DevolveTrueSoAPrimeiraVez()
    {
        RegistoDeConhecimento registo = Registo();

        Assert.IsTrue(registo.Registar(
            "pessoa:thomas-hale", CategoriaDeConhecimento.Pessoa, "Thomas Hale"));

        Assert.IsFalse(registo.Registar(
            "pessoa:thomas-hale", CategoriaDeConhecimento.Pessoa, "Thomas Hale"));

        Assert.AreEqual(1, registo.Total);
    }

    [Test]
    public void Registar_SemId_RecusaEQueixaSe()
    {
        LogAssert.Expect(LogType.Error, new Regex("Conhecimento sem id"));

        RegistoDeConhecimento registo = Registo();

        Assert.IsFalse(registo.Registar(
            string.Empty, CategoriaDeConhecimento.Pessoa, "Ninguém"));

        Assert.AreEqual(0, registo.Total);
    }

    [Test]
    public void Da_DevolveSoAGavetaPedidaEPelaOrdemPorQueSeAprendeu()
    {
        RegistoDeConhecimento registo = Registo();

        registo.Registar("pessoa:b", CategoriaDeConhecimento.Pessoa, "B");
        registo.Registar("local:x", CategoriaDeConhecimento.Local, "X");
        registo.Registar("pessoa:a", CategoriaDeConhecimento.Pessoa, "A");

        List<Conhecimento> pessoas = registo.Da(CategoriaDeConhecimento.Pessoa);

        Assert.AreEqual(2, pessoas.Count);

        // A ordem é a de quem aprendeu, não a alfabética: é ela que conta a
        // progressão.
        Assert.AreEqual("B", pessoas[0].Titulo);
        Assert.AreEqual("A", pessoas[1].Titulo);
    }

    [Test]
    public void Instancia_EncontraORegistoQueEstaNaCena()
    {
        RegistoDeConhecimento registo = Registo();

        Assert.AreSame(registo, RegistoDeConhecimento.Instancia);
    }

    [Test]
    public void ORegistoAutoradoTomaOLugarDoDeEmergencia()
    {
        // A regra que a Instancia promete: um registo posto à mão ganha sempre
        // a um criado de emergência. Estava escrita ao contrário dentro do
        // Awake — o autorado destruía-se a si próprio — e como o Awake não
        // corre em EditMode, nenhum teste lhe tocava.
        RegistoDeConhecimento emergencia = RegistoDeConhecimento.Instancia;

        Assert.IsTrue(emergencia.Automatico);

        emergencia.Registar(
            "pessoa:thomas-hale", CategoriaDeConhecimento.Pessoa, "Thomas Hale");

        RegistoDeConhecimento autorado = Registo();

        autorado.Assumir();

        Assert.AreSame(autorado, RegistoDeConhecimento.Instancia);

        // E leva com ele o que o outro já tinha aprendido: a troca não pode
        // custar ao jogador o que ele descobriu antes dela.
        Assert.IsTrue(autorado.Sabe("pessoa:thomas-hale"));
    }

    [Test]
    public void UmSegundoRegistoAutorado_DesisteEmVezDePartirOConhecimentoAoMeio()
    {
        RegistoDeConhecimento primeiro = Registo();
        primeiro.Assumir();

        RegistoDeConhecimento segundo = Registo();
        segundo.Assumir();

        Assert.AreSame(primeiro, RegistoDeConhecimento.Instancia);
        Assert.IsTrue(segundo == null, "o segundo devia ter-se destruído");
    }

    // ---------------------------------------------------------------
    // Fonte 1: apanhar uma fotografia
    // ---------------------------------------------------------------

    private const string JsonDe1986 =
        "{\"id\":\"Fotografia_Estudio_1986\",\"ano\":1986," +
        "\"data\":\"17 de Julho de 1986\",\"local\":\"Estúdio do Ethan\"," +
        "\"personagens\":[\"Thomas Hale\",\"Elias Ward\"]," +
        "\"frente\":\"Três pessoas à porta da fábrica.\"}";

    [Test]
    public void Fotografia_DaAFotografiaAData0LocalEUmaEntradaPorPessoa()
    {
        RegistoDeConhecimento registo = Registo();

        int novas = FontesDeConhecimento.RegistarFotografia(registo, Foto(JsonDe1986));

        // Uma fotografia, um ano, um sítio e duas pessoas — que é exactamente a
        // checklist da task a sair de campos que a FASE 5 já tinha autorado.
        Assert.AreEqual(5, novas);

        Assert.IsTrue(registo.Sabe(Conhecimento.Identificar(
            CategoriaDeConhecimento.Fotografia, "Fotografia_Estudio_1986")));

        Assert.IsTrue(registo.Sabe(Conhecimento.Identificar(
            CategoriaDeConhecimento.Data, "1986")));

        Assert.IsTrue(registo.Sabe(Conhecimento.Identificar(
            CategoriaDeConhecimento.Local, "Estúdio do Ethan")));

        Assert.AreEqual(2, registo.Da(CategoriaDeConhecimento.Pessoa).Count);
    }

    [Test]
    public void Fotografia_ADataEIndexadaPeloAnoENaoPelaHora()
    {
        RegistoDeConhecimento registo = Registo();

        // Duas fotografias da mesma noite de 17 de Julho de 1986, com horas
        // diferentes escritas no verso. São a mesma data na cabeça do jogador.
        FontesDeConhecimento.RegistarFotografia(registo, Foto(
            "{\"id\":\"a\",\"ano\":1986,\"data\":\"17 de Julho de 1986, 23:31\"}"));

        FontesDeConhecimento.RegistarFotografia(registo, Foto(
            "{\"id\":\"b\",\"ano\":1986,\"data\":\"17 de Julho de 1986, 23:47\"}"));

        Assert.AreEqual(1, registo.Da(CategoriaDeConhecimento.Data).Count);
        Assert.AreEqual(2, registo.Da(CategoriaDeConhecimento.Fotografia).Count);
    }

    [Test]
    public void Fotografia_AMesmaPessoaEmDuasFotografiasNaoDuplica()
    {
        RegistoDeConhecimento registo = Registo();

        FontesDeConhecimento.RegistarFotografia(registo, Foto(JsonDe1986));

        FontesDeConhecimento.RegistarFotografia(registo, Foto(
            "{\"id\":\"Fotografia_Estudio_1994\",\"ano\":1994," +
            "\"personagens\":[\"Thomas Hale\"]}"));

        Assert.AreEqual(2, registo.Da(CategoriaDeConhecimento.Pessoa).Count);
    }

    [Test]
    public void ApanharUmaFotografia_EnchOCaderno()
    {
        // O caminho a sério, e não só o mapeamento: o PhotoInteractable tem de
        // chamar o registo. Escrever o sistema e não o ligar é o defeito que
        // este projecto já teve duas vezes.
        RegistoDeConhecimento registo = Registo();

        GameObject host = Novo("Player");
        PlayerStateMachine maquina = host.AddComponent<PlayerStateMachine>();
        PhotographySystem fotografia = host.AddComponent<PhotographySystem>();

        Ligar(fotografia, "photoAlbum", Novo("PhotoAlbum"));
        Ligar(fotografia, "photoCamera", Novo("CameraBody"));
        Ligar(fotografia, "photoPreview", Novo("PhotoPreview"));
        Ligar(fotografia, "stateMachine", maquina);

        GameObject alvo = Novo("FotografiaNoMundo");
        PhotoInteractable apanhavel = alvo.AddComponent<PhotoInteractable>();

        PhotoAsset asset = ScriptableObject.CreateInstance<PhotoAsset>();
        asset.name = "Fotografia_Estudio_1986";
        criados.Add(asset);
        JsonUtility.FromJsonOverwrite("{\"fotografia\":" + JsonDe1986 + "}", asset);

        GameObject anfitriaoDaInspeccao = Novo("Inspecao");
        InspectionSystem inspeccao =
            anfitriaoDaInspeccao.AddComponent<InspectionSystem>();

        // A câmara vai ligada de propósito: sem ela o Inspect() sai na primeira
        // linha com um erro, e o teste dava por bom um caminho que nunca chegou
        // ao fim.
        GameObject camara = Novo("PlayerCamera");
        Ligar(inspeccao, "playerCamera", camara.AddComponent<Camera>());
        Ligar(inspeccao, "stateMachine", maquina);

        Ligar(apanhavel, "inspectionSystem", inspeccao);
        Ligar(apanhavel, "photographySystem", fotografia);
        Ligar(apanhavel, "fotografia", asset);

        apanhavel.Interact();

        Assert.IsTrue(apanhavel.Apanhada);

        Assert.IsTrue(registo.Sabe(Conhecimento.Identificar(
            CategoriaDeConhecimento.Pessoa, "Thomas Hale")));

        Assert.IsTrue(registo.Sabe(Conhecimento.Identificar(
            CategoriaDeConhecimento.Fotografia, "Fotografia_Estudio_1986")));
    }

    // ---------------------------------------------------------------
    // Fonte 2: ler um documento
    // ---------------------------------------------------------------

    [Test]
    public void LerUmDocumento_PoeOCorpoNoCaderno()
    {
        RegistoDeConhecimento registo = Registo();

        GameObject host = Novo("Player");
        PlayerStateMachine maquina = host.AddComponent<PlayerStateMachine>();

        GameObject anfitriao = Novo("Leitura");
        ReadingSystem leitura = anfitriao.AddComponent<ReadingSystem>();
        Ligar(leitura, "readingPanel", Novo("PainelDeLeitura"));
        Ligar(leitura, "stateMachine", maquina);

        GameObject papel = Novo("Documento");
        DocumentInteractable documento = papel.AddComponent<DocumentInteractable>();
        Ligar(documento, "readingSystem", leitura);
        Ligar(documento, "stateId", "prototipo/documento");
        Ligar(documento, "title", "Nota de Ethan Cole");
        Ligar(documento, "body", "Ele ainda não chegou.");

        documento.Interact();

        Assert.IsTrue(documento.HasBeenRead);

        Conhecimento entrada = registo.Obter(Conhecimento.Identificar(
            CategoriaDeConhecimento.Documento, "prototipo/documento"));

        Assert.IsNotNull(entrada);
        Assert.AreEqual("Nota de Ethan Cole", entrada.Titulo);
        Assert.AreEqual("Ele ainda não chegou.", entrada.Texto);
    }

    [Test]
    public void DocumentoQueNaoAbriu_NaoEntraNoCaderno()
    {
        // O ReadingSystem devolve false sem painel. Marcar como lido aí dava um
        // documento no caderno sem o jogador ter visto uma linha.
        LogAssert.Expect(LogType.Error, new Regex("Ecrã de leitura sem painel"));

        RegistoDeConhecimento registo = Registo();

        GameObject anfitriao = Novo("Leitura");
        ReadingSystem leitura = anfitriao.AddComponent<ReadingSystem>();

        GameObject papel = Novo("Documento");
        DocumentInteractable documento = papel.AddComponent<DocumentInteractable>();
        Ligar(documento, "readingSystem", leitura);
        Ligar(documento, "stateId", "prototipo/documento");

        documento.Interact();

        Assert.IsFalse(documento.HasBeenRead);
        Assert.AreEqual(0, registo.Total);
    }

    [Test]
    public void DocumentoSemStateId_IndexaPeloTituloEAvisa()
    {
        LogAssert.Expect(LogType.Warning, new Regex("Documento sem stateId"));

        RegistoDeConhecimento registo = Registo();

        Assert.IsTrue(FontesDeConhecimento.RegistarDocumento(
            registo, null, "Recorte de jornal", "…"));

        Assert.IsTrue(registo.Sabe(Conhecimento.Identificar(
            CategoriaDeConhecimento.Documento, "Recorte de jornal")));
    }

    // ---------------------------------------------------------------
    // Fonte 3: alinhar — e o caso real desta fase
    // ---------------------------------------------------------------

    private PhotoComparisonSystem Comparacao(out PlayerStateMachine maquina)
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

    private static PhotoData Par1986()
    {
        return Foto(
            "{\"id\":\"Fotografia_Estudio_1986\",\"ano\":1986," +
            "\"alinhamentos\":[{\"comId\":\"Fotografia_Estudio_1994\"," +
            "\"escalaAlvo\":1,\"toleranciaPosicao\":0.05," +
            "\"toleranciaRotacao\":5,\"toleranciaEscala\":0.08}]," +
            "\"descobertas\":[{\"id\":\"porta-lateral\",\"tipo\":2," +
            "\"descricao\":\"A porta lateral da fábrica, que em 1994 está tapada.\"," +
            "\"revelaAoAlinharCom\":\"Fotografia_Estudio_1994\"}]}"
        );
    }

    [Test]
    public void Alinhar_PoeADescobertaNoCaderno()
    {
        RegistoDeConhecimento registo = Registo();

        PhotoComparisonSystem comparacao = Comparacao(out _);

        comparacao.Abrir(Par1986(), Foto("{\"id\":\"Fotografia_Estudio_1994\"}"));
        comparacao.AlternarModo();

        Assert.IsTrue(comparacao.Alinhada);

        Conhecimento descoberta = registo.Obter(Conhecimento.Identificar(
            CategoriaDeConhecimento.Descoberta,
            "Fotografia_Estudio_1986",
            "porta-lateral"));

        Assert.IsNotNull(descoberta);

        Assert.AreEqual(
            "A porta lateral da fábrica, que em 1994 está tapada.",
            descoberta.Titulo
        );

        // A proveniência: sem ela o caderno ficava com uma frase caída do céu.
        StringAssert.Contains("Fotografia_Estudio_1994", descoberta.Texto);
    }

    [Test]
    public void ADescobertaSobreviveAoAlinhamentoSeguinte()
    {
        // ESTE é o caso real da fase. A DescobertasReveladas da comparação é
        // limpa a cada alinhamento e ao abrir — é o registo do ÚLTIMO, não um
        // arquivo. Antes disto, alinhar um segundo par apagava sem vestígio o
        // que o jogador tinha acabado de descobrir.
        RegistoDeConhecimento registo = Registo();

        PhotoComparisonSystem comparacao = Comparacao(out _);

        comparacao.Abrir(Par1986(), Foto("{\"id\":\"Fotografia_Estudio_1994\"}"));
        comparacao.AlternarModo();

        Assert.AreEqual(1, comparacao.DescobertasReveladas.Count);

        // Um par qualquer a seguir, sem descobertas nenhumas.
        comparacao.Abrir(
            Foto("{\"id\":\"outra\"}"),
            Foto("{\"id\":\"terceira\"}")
        );

        Assert.AreEqual(0, comparacao.DescobertasReveladas.Count);

        Assert.IsTrue(registo.Sabe(Conhecimento.Identificar(
            CategoriaDeConhecimento.Descoberta,
            "Fotografia_Estudio_1986",
            "porta-lateral")));
    }

    [Test]
    public void AMesmaDescoberta_NaoEntraDuasVezes()
    {
        RegistoDeConhecimento registo = Registo();

        PhotoComparisonSystem comparacao = Comparacao(out _);

        PhotoData fixa = Par1986();
        PhotoData movel = Foto("{\"id\":\"Fotografia_Estudio_1994\"}");

        comparacao.Abrir(fixa, movel);
        comparacao.AlternarModo();

        comparacao.Abrir(fixa, movel);
        comparacao.AlternarModo();

        Assert.AreEqual(1, registo.Da(CategoriaDeConhecimento.Descoberta).Count);
    }
}
