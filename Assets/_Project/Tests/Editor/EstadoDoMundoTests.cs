using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Testes do world state da FASE 7. O que se verifica aqui é a promessa da
// subtask 869f4yb88: uma alteração feita em 1986 é visível em 2026, e o
// DoorInteractable lê o seu estado dali.
//
// A arbitragem autorado/automático é decalcada do RegistoDeConhecimento, onde
// ela já esteve INVERTIDA dentro do Awake sem nada a apanhar — por isso o
// Assumir() é público e por isso estes testes existem.
public class EstadoDoMundoTests
{
    private readonly List<GameObject> criados = new List<GameObject>();

    private GameObject Novo(string nome)
    {
        GameObject go = new GameObject(nome);
        criados.Add(go);
        return go;
    }

    private EstadoDoMundo Autorado(string nome = "EstadoAutorado")
    {
        // AddComponent num objecto de teste: o automatico fica false, que é o
        // que distingue um estado posto à mão de um criado de emergência.
        return Novo(nome).AddComponent<EstadoDoMundo>();
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

        // Sem isto um teste herdava o mundo do anterior e passava por razões
        // que não são as suas.
        EstadoDoMundo.Esquecer();
    }

    // ---------------------------------------------------------------- estado

    [Test]
    public void Instancia_CriaDeEmergenciaQuandoNaoHaNenhum()
    {
        EstadoDoMundo estado = EstadoDoMundo.Instancia;

        Assert.IsNotNull(estado);
        Assert.IsTrue(
            estado.Automatico,
            "o estado criado pela Instancia tem de se saber automático");
        Assert.AreEqual(Epoca.Presente2026, estado.EpocaActual,
            "o jogo começa em 2026");
    }

    [Test]
    public void Registar_EscreveELeDeVolta()
    {
        EstadoDoMundo estado = Autorado();

        Assert.IsTrue(estado.Registar("porta-cave", DoorInteractable.ChaveAberta, true));
        Assert.IsTrue(estado.LerBool("porta-cave", DoorInteractable.ChaveAberta));
        Assert.AreEqual(1, estado.Total);
    }

    [Test]
    public void Registar_DevolveFalseQuandoOValorNaoMuda()
    {
        EstadoDoMundo estado = Autorado();

        estado.Registar("porta-cave", "aberta", true);

        Assert.IsFalse(
            estado.Registar("porta-cave", "aberta", true),
            "escrever o mesmo valor não é uma alteração");
        Assert.AreEqual(1, estado.Total, "não pode haver duas entradas para o mesmo par");
    }

    [Test]
    public void Registar_ActualizaEmVezDeAcumular()
    {
        EstadoDoMundo estado = Autorado();

        estado.Registar("porta-cave", "aberta", true);

        // Ao contrário do RegistoDeConhecimento, aqui a segunda vez É uma
        // mudança: uma porta abre e fecha as vezes que o jogador quiser.
        Assert.IsTrue(estado.Registar("porta-cave", "aberta", false));
        Assert.IsFalse(estado.LerBool("porta-cave", "aberta"));
        Assert.AreEqual(1, estado.Total);
    }

    [Test]
    public void Registar_DistingueChavesDoMesmoObjecto()
    {
        EstadoDoMundo estado = Autorado();

        estado.Registar("porta-cave", "aberta", true);
        estado.Registar("porta-cave", "trancada", false);

        Assert.AreEqual(2, estado.Total, "a segunda chave esmagou a primeira");
        Assert.IsTrue(estado.LerBool("porta-cave", "aberta"));
        Assert.IsFalse(estado.LerBool("porta-cave", "trancada"));
    }

    [Test]
    public void Registar_RecusaStateIdVazioEQueixaSe()
    {
        EstadoDoMundo estado = Autorado();

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
            "Alteração sem stateId"));

        Assert.IsFalse(estado.Registar("", "aberta", true));
        Assert.AreEqual(0, estado.Total);
    }

    [Test]
    public void Registar_RecusaChaveVaziaEQueixaSe()
    {
        EstadoDoMundo estado = Autorado();

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
            "Alteração sem chave"));

        Assert.IsFalse(estado.Registar("porta-cave", " ", true));
        Assert.AreEqual(0, estado.Total);
    }

    [Test]
    public void LerBool_DevolveAOmissaoQuandoNaoSabe()
    {
        EstadoDoMundo estado = Autorado();

        Assert.IsFalse(estado.LerBool("nao-existe", "aberta"));
        Assert.IsTrue(estado.LerBool("nao-existe", "aberta", true),
            "a omissão pedida não foi respeitada");
    }

    [Test]
    public void EscreverELer_FazemAVoltaCompletaSemDependerDaCultura()
    {
        // Invariante de propósito: o dia em que isto guardar um número, um save
        // escrito numa máquina com vírgula decimal tem de abrir noutra com
        // ponto.
        Assert.AreEqual("true", AlteracaoDeMundo.Escrever(true));
        Assert.AreEqual("false", AlteracaoDeMundo.Escrever(false));

        Assert.IsTrue(AlteracaoDeMundo.Ler(AlteracaoDeMundo.Escrever(true)));
        Assert.IsFalse(AlteracaoDeMundo.Ler(AlteracaoDeMundo.Escrever(false)));
        Assert.IsTrue(AlteracaoDeMundo.Ler(null, true), "a omissão não foi usada");
    }

    // ---------------------------------------------------------------- épocas

    [Test]
    public void MudarPara_TrocaAEpoca()
    {
        EstadoDoMundo estado = Autorado();

        Assert.IsFalse(estado.Em1986);

        estado.MudarPara(Epoca.Passado1986);

        Assert.AreEqual(Epoca.Passado1986, estado.EpocaActual);
        Assert.IsTrue(estado.Em1986);
    }

    [Test]
    public void MudarPara_NaoSaiCedoQuandoAEpocaJaEAPedida()
    {
        // A armadilha desta casa: um Garantir* que sai cedo nunca corrige o que
        // já está montado. Aqui, sair cedo significava um objecto de 1986
        // deixado activo no editor ficar activo em 2026 porque «a época já era
        // 2026».
        EstadoDoMundo estado = Autorado();

        GameObject so1986 = Novo("So1986");
        ObjectoDeEpoca marcador = so1986.AddComponent<ObjectoDeEpoca>();
        Campo(marcador, "existeEm2026").SetValue(marcador, false);
        Campo(marcador, "existeEm1986").SetValue(marcador, true);

        Assert.IsTrue(so1986.activeSelf, "arranca activo, como ficaria no editor");

        estado.MudarPara(Epoca.Presente2026);

        Assert.IsFalse(
            so1986.activeSelf,
            "mudar para a época em que já se estava tem de pôr o mundo em ordem");
    }

    [Test]
    public void Reaplicar_PoeOMundoDeAcordoComAEpocaCorrente()
    {
        EstadoDoMundo estado = Autorado();

        estado.MudarPara(Epoca.Passado1986);

        GameObject so2026 = Novo("So2026");
        ObjectoDeEpoca marcador = so2026.AddComponent<ObjectoDeEpoca>();
        Campo(marcador, "existeEm2026").SetValue(marcador, true);
        Campo(marcador, "existeEm1986").SetValue(marcador, false);

        estado.Reaplicar();

        Assert.IsFalse(
            so2026.activeSelf,
            "um objecto nascido a meio de 1986 tem de se acertar");
    }

    // ----------------------------------------------------------- arbitragem

    [Test]
    public void Autorado_GanhaAoAutomaticoELevaOQueEleSabia()
    {
        // O de emergência nasce primeiro e o jogador abre-lhe uma porta.
        EstadoDoMundo emergencia = EstadoDoMundo.Instancia;

        Assert.IsTrue(emergencia.Automatico);

        emergencia.Registar("porta-cave", "aberta", true);

        // Agora aparece o do Bootstrap.
        EstadoDoMundo autorado = Autorado();

        autorado.Assumir();

        Assert.AreSame(autorado, EstadoDoMundo.Instancia,
            "o estado posto à mão tem de ficar com o lugar");
        Assert.IsTrue(
            autorado.LerBool("porta-cave", "aberta"),
            "a troca custou ao jogador a porta que ele já tinha aberto");
    }

    [Test]
    public void Autorado_LevaTambemAEpocaDoAutomatico()
    {
        // Assumir a meio de um flashback não pode devolver o mundo a 2026 sem
        // ninguém ter saído de 1986.
        EstadoDoMundo emergencia = EstadoDoMundo.Instancia;

        emergencia.MudarPara(Epoca.Passado1986);

        EstadoDoMundo autorado = Autorado();

        autorado.Assumir();

        Assert.AreEqual(
            Epoca.Passado1986,
            EstadoDoMundo.Instancia.EpocaActual,
            "a época ficou para trás e a cidade mudava debaixo dos pés");
    }

    [Test]
    public void SegundoAutorado_DestroiSeAEleProprio()
    {
        EstadoDoMundo primeiro = Autorado("Primeiro");
        primeiro.Assumir();

        GameObject segundoGo = Novo("Segundo");
        EstadoDoMundo segundo = segundoGo.AddComponent<EstadoDoMundo>();

        segundo.Assumir();

        Assert.AreSame(primeiro, EstadoDoMundo.Instancia,
            "dois estados é o mundo partido ao meio");
        Assert.IsTrue(
            segundoGo == null || segundo == null,
            "o segundo tinha de se ter destruído");
    }

    [Test]
    public void Esquecer_LimpaAEstaticaEOAutomatico()
    {
        EstadoDoMundo emergencia = EstadoDoMundo.Instancia;

        Assert.IsTrue(emergencia.Automatico);

        EstadoDoMundo.Esquecer();

        EstadoDoMundo novo = EstadoDoMundo.Instancia;

        Assert.AreNotSame(emergencia, novo,
            "a instância anterior sobreviveu ao Esquecer");
    }

    [Test]
    public void Limpar_EsvaziaOMundoSemMexerNaEpoca()
    {
        EstadoDoMundo estado = Autorado();

        estado.Registar("porta-cave", "aberta", true);
        estado.MudarPara(Epoca.Passado1986);

        estado.Limpar();

        Assert.AreEqual(0, estado.Total);
        Assert.AreEqual(Epoca.Passado1986, estado.EpocaActual,
            "limpar as alterações não é sair da época");
    }

    private static System.Reflection.FieldInfo Campo(object alvo, string nome)
    {
        return alvo.GetType().GetField(
            nome,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
    }
}
