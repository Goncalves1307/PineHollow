using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

// Testes do caderno.
//
// Os TMP_Text são ligados de propósito, e não deixados a null: o Configurar de
// cada linha sai cedo sem rótulo, e um teste que não os ligue dava por bom um
// caderno que nunca escreveu uma palavra. É o mesmo erro que deixou passar o
// alinhamento na origem errada com 157 testes verdes.
public class JournalSystemTests
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

    private TMP_Text Texto(string nome, GameObject pai)
    {
        return Filho(nome, pai).AddComponent<TextMeshProUGUI>();
    }

    private GameObject Modelo<T>(string nome, GameObject pai) where T : Component
    {
        GameObject modelo = Filho(nome, pai);

        GameObject rotulo = Filho("Rotulo", modelo);
        TMP_Text texto = rotulo.AddComponent<TextMeshProUGUI>();

        T componente = modelo.AddComponent<T>();

        Ligar(componente, "rotulo", texto);

        modelo.SetActive(false);

        return modelo;
    }

    private JournalSystem Montado(
        out PlayerStateMachine maquina,
        out RegistoDeConhecimento registo,
        out GameObject painel,
        out TMP_Text detalhe,
        out TMP_Text cadeiaTexto)
    {
        registo = Novo("RegistoDeConhecimento")
            .AddComponent<RegistoDeConhecimento>();

        GameObject host = Novo("Player");
        maquina = host.AddComponent<PlayerStateMachine>();

        GameObject anfitriao = Novo("Investigacao");
        JournalSystem caderno = anfitriao.AddComponent<JournalSystem>();

        painel = Filho("Journal", anfitriao);

        GameObject separadores = Filho("Separadores", painel);
        GameObject modeloDoSeparador = Modelo<SeparadorDoJournal>(
            "ModeloDoSeparador", separadores);

        GameObject entradas = Filho("Entradas", painel);
        GameObject modeloDaEntrada = Modelo<EntradaDoJournal>(
            "ModeloDaEntrada", entradas);

        detalhe = Texto("Detalhe", painel);
        cadeiaTexto = Texto("Cadeia", painel);

        Ligar(caderno, "painel", painel);
        Ligar(caderno, "listaDosSeparadores", separadores.transform);
        Ligar(caderno, "modeloDoSeparador", modeloDoSeparador);
        Ligar(caderno, "listaDasEntradas", entradas.transform);
        Ligar(caderno, "modeloDaEntrada", modeloDaEntrada);
        Ligar(caderno, "detalhe", detalhe);
        Ligar(caderno, "cadeiaTexto", cadeiaTexto);
        Ligar(caderno, "stateMachine", maquina);

        painel.SetActive(false);

        return caderno;
    }

    private JournalSystem Montado(out PlayerStateMachine maquina)
    {
        return Montado(out maquina, out _, out _, out _, out _);
    }

    // ---------------------------------------------------------------
    // Abrir e fechar
    // ---------------------------------------------------------------

    [Test]
    public void J_AbreOCadernoEEmpurraOModo()
    {
        JournalSystem caderno = Montado(out PlayerStateMachine maquina);

        caderno.Alternar();

        Assert.IsTrue(caderno.IsOpen);
        Assert.AreEqual(PlayerState.ViewingJournal, maquina.Current);
    }

    [Test]
    public void OCadernoLibertaOCursor()
    {
        // «É ecrã de rato: depende do dono único do cursor» — e o dono é a
        // PlayerStateMachine, que é a única classe que lhe toca.
        JournalSystem caderno = Montado(out PlayerStateMachine maquina);

        caderno.Alternar();

        Assert.IsTrue(maquina.IsCursorFree);
        Assert.IsTrue(maquina.IsMovementLocked);
        Assert.IsTrue(maquina.IsLookLocked);
    }

    [Test]
    public void Esc_FechaOCadernoESemDeixarOModoOrfao()
    {
        JournalSystem caderno = Montado(out PlayerStateMachine maquina);

        caderno.Alternar();

        maquina.BeginFrame();
        maquina.RequestBack();

        Assert.IsTrue(maquina.ConsumeBack(PlayerState.ViewingJournal));

        caderno.Fechar();

        Assert.IsFalse(caderno.IsOpen);
        Assert.AreEqual(0, maquina.OpenModeCount);
    }

    [Test]
    public void J_NaoAbreSobreOutraCamada()
    {
        // O caderno solta o cursor; abri-lo por cima de uma inspecção deixava o
        // objecto na mão por trás do painel.
        JournalSystem caderno = Montado(out PlayerStateMachine maquina);

        maquina.PushMode(PlayerState.Inspecting);

        caderno.Alternar();

        Assert.IsFalse(caderno.IsOpen);
        Assert.AreEqual(PlayerState.Inspecting, maquina.Current);
    }

    [Test]
    public void J_NaoFechaOCadernoQueEstaPorBaixoDeOutraCamada()
    {
        JournalSystem caderno = Montado(out PlayerStateMachine maquina);

        caderno.Alternar();

        maquina.PushMode(PlayerState.ViewingPhoto);

        caderno.Alternar();

        Assert.IsTrue(caderno.IsOpen);
    }

    // ---------------------------------------------------------------
    // As seis gavetas
    // ---------------------------------------------------------------

    [Test]
    public void OCadernoAbreComAsSeisGavetas()
    {
        JournalSystem caderno = Montado(out _);

        caderno.Alternar();

        Assert.AreEqual(6, caderno.Separadores.Count);

        // Em pt-PT e no plural, como se lê a lombada de uma gaveta.
        Assert.AreEqual("Pessoas", caderno.Separadores[0].Rotulo);
        Assert.AreEqual("Descobertas", caderno.Separadores[5].Rotulo);
    }

    [Test]
    public void AGavetaMostraSoOQueLhePertence()
    {
        JournalSystem caderno = Montado(
            out _, out RegistoDeConhecimento registo, out _, out _, out _);

        registo.Registar("pessoa:thomas-hale",
            CategoriaDeConhecimento.Pessoa, "Thomas Hale");

        registo.Registar("local:estudio",
            CategoriaDeConhecimento.Local, "Estúdio do Ethan");

        caderno.Alternar();

        Assert.AreEqual(1, caderno.Entradas.Count);
        Assert.AreEqual("Thomas Hale", caderno.Entradas[0].Rotulo);

        caderno.Mostrar(CategoriaDeConhecimento.Local);

        Assert.AreEqual(1, caderno.Entradas.Count);
        Assert.AreEqual("Estúdio do Ethan", caderno.Entradas[0].Rotulo);
    }

    [Test]
    public void TrocarDeGaveta_NaoAcumulaLinhasDaAnterior()
    {
        JournalSystem caderno = Montado(
            out _, out RegistoDeConhecimento registo, out _, out _, out _);

        registo.Registar("pessoa:a", CategoriaDeConhecimento.Pessoa, "A");
        registo.Registar("pessoa:b", CategoriaDeConhecimento.Pessoa, "B");

        caderno.Alternar();

        Assert.AreEqual(2, caderno.Entradas.Count);

        caderno.Mostrar(CategoriaDeConhecimento.Data);

        Assert.AreEqual(0, caderno.Entradas.Count);

        caderno.Mostrar(CategoriaDeConhecimento.Pessoa);

        Assert.AreEqual(2, caderno.Entradas.Count);
    }

    [Test]
    public void EscolherUmaEntrada_MostraOQueSeSabeSobreEla()
    {
        JournalSystem caderno = Montado(
            out _, out RegistoDeConhecimento registo, out _,
            out TMP_Text detalhe, out _);

        registo.Registar(
            "documento:nota",
            CategoriaDeConhecimento.Documento,
            "Nota de Ethan Cole",
            "Ele ainda não chegou."
        );

        caderno.Alternar();
        caderno.Mostrar(CategoriaDeConhecimento.Documento);

        caderno.Seleccionar(caderno.Entradas[0].Conhecimento);

        StringAssert.Contains("Nota de Ethan Cole", detalhe.text);
        StringAssert.Contains("Ele ainda não chegou.", detalhe.text);
    }

    [Test]
    public void GavetaVazia_DizQueNaoHaNadaEmVezDeFicarMuda()
    {
        JournalSystem caderno = Montado(
            out _, out _, out _, out TMP_Text detalhe, out _);

        caderno.Alternar();

        Assert.AreEqual("Nada ainda.", detalhe.text);
    }

    // ---------------------------------------------------------------
    // A cadeia de progressão (GDD §17)
    // ---------------------------------------------------------------

    private CadeiaDeConhecimento Cadeia()
    {
        CadeiaDeConhecimento cadeia =
            ScriptableObject.CreateInstance<CadeiaDeConhecimento>();

        criados.Add(cadeia);

        cadeia.Definir(new[]
        {
            new EloDaCadeia("Thomas", "pessoa:thomas-hale"),
            new EloDaCadeia("Thomas morreu em 1986", "documento:obituario"),
            new EloDaCadeia("Thomas aparece em 1994", "fotografia:1994")
        });

        return cadeia;
    }

    [Test]
    public void ACadeiaEsconde_ONaoSabidoEMostraOSabido()
    {
        JournalSystem caderno = Montado(
            out _, out RegistoDeConhecimento registo, out _, out _,
            out TMP_Text cadeiaTexto);

        CadeiaDeConhecimento cadeia = Cadeia();
        Ligar(caderno, "cadeia", cadeia);

        registo.Registar("pessoa:thomas-hale",
            CategoriaDeConhecimento.Pessoa, "Thomas Hale");

        caderno.Alternar();

        string[] linhas = cadeiaTexto.text.Split('\n');

        Assert.AreEqual(3, linhas.Length);

        // O elo sabido mostra a frase; os outros mostram um traço. Mostrar a
        // frase de um elo por acender era contar a história por antecipação.
        Assert.AreEqual("Thomas", linhas[0]);
        Assert.AreEqual("—", linhas[1]);
        Assert.AreEqual("—", linhas[2]);
    }

    [Test]
    public void ACadeiaNaoLevaContadorDeQuantosFaltam()
    {
        // «Evitar checklist excessivo»: um «1 de 3» é exactamente isso, e diz
        // ao jogador quanta história lhe falta.
        JournalSystem caderno = Montado(
            out _, out RegistoDeConhecimento registo, out _, out _,
            out TMP_Text cadeiaTexto);

        Ligar(caderno, "cadeia", Cadeia());

        registo.Registar("pessoa:thomas-hale",
            CategoriaDeConhecimento.Pessoa, "Thomas Hale");

        caderno.Alternar();

        // Uma linha por elo e nada mais: nenhuma linha de resumo.
        Assert.AreEqual(3, cadeiaTexto.text.Split('\n').Length);

        StringAssert.DoesNotContain("de 3", cadeiaTexto.text);
        StringAssert.DoesNotContain("/3", cadeiaTexto.text);
    }

    [Test]
    public void SemCadeiaLigada_OCadernoAbreNaMesma()
    {
        JournalSystem caderno = Montado(
            out _, out _, out _, out _, out TMP_Text cadeiaTexto);

        caderno.Alternar();

        Assert.IsTrue(caderno.IsOpen);
        Assert.AreEqual(string.Empty, cadeiaTexto.text);
    }

    // ---------------------------------------------------------------
    // O caso real: o caderno tem de ler o que o jogo produziu
    // ---------------------------------------------------------------

    [Test]
    public void OQueVemDeUmaFotografiaApareceNoCaderno()
    {
        JournalSystem caderno = Montado(
            out _, out RegistoDeConhecimento registo, out _, out _, out _);

        FontesDeConhecimento.RegistarFotografia(registo, JsonUtility.FromJson<PhotoData>(
            "{\"id\":\"Fotografia_Estudio_1986\",\"ano\":1986," +
            "\"data\":\"17 de Julho de 1986\",\"local\":\"Estúdio do Ethan\"," +
            "\"personagens\":[\"Thomas Hale\",\"Elias Ward\"]}"));

        caderno.Alternar();

        Assert.AreEqual(2, caderno.Entradas.Count);

        caderno.Mostrar(CategoriaDeConhecimento.Data);
        Assert.AreEqual("17 de Julho de 1986", caderno.Entradas[0].Rotulo);

        caderno.Mostrar(CategoriaDeConhecimento.Local);
        Assert.AreEqual("Estúdio do Ethan", caderno.Entradas[0].Rotulo);
    }
}
