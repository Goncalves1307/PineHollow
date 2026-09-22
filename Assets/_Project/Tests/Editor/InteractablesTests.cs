using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;

// Testes dos interactables da FASE 3. O que interessa aqui não é o movimento
// — é o estado ficar legível de fora: a FASE 7 (world state 2026/1986) e a
// FASE 21 (save) têm de o poder ler, e a porta original enterrou-o em campos
// privados que ninguém alcança.
public class InteractablesTests
{
    // Limpeza no TearDown e não no fim do teste: um assert que falha lança, e o
    // que viesse depois dele nunca corria.
    private readonly List<GameObject> criados = new List<GameObject>();

    private GameObject Novo(string nome)
    {
        GameObject go = new GameObject(nome);
        criados.Add(go);
        return go;
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

        // A FASE 6 pendurou um registo de conhecimento nestas fontes: apanhar
        // uma fotografia, ler um documento e alinhar passaram a escrever nele.
        // Sem isto, cada teste deixava para trás um registo criado de
        // emergência e o teste seguinte encontrava a cena suja.
        RegistoDeConhecimento.Esquecer();

        // A FASE 7 pendurou o world state na porta: abrir passou a escrever
        // nele. Sem isto, a porta de um teste chegava já aberta ao seguinte.
        EstadoDoMundo.Esquecer();
    }

    // ------------------------------------------------------------- a porta

    // A porta não tinha um único teste, apesar de ser o critério de aceitação
    // literal da subtask do world state («o DoorInteractable lê o seu estado
    // dali»). Estes são os primeiros.

    private DoorInteractable PortaMontada(string stateId)
    {
        GameObject go = Novo("Porta");

        DoorInteractable porta = go.AddComponent<DoorInteractable>();

        GameObject pivot = Novo("Pivot");
        pivot.transform.SetParent(go.transform);

        Campo(porta, "doorPivot").SetValue(porta, pivot.transform);
        Campo(porta, "stateId").SetValue(porta, stateId);

        porta.Preparar();

        return porta;
    }

    [Test]
    public void Porta_ComutaOEstadoEOVerbo()
    {
        DoorInteractable porta = PortaMontada("porta-cave");

        Assert.IsFalse(porta.IsOpen);
        Assert.AreEqual("Abrir", porta.GetInteractionText());

        porta.Interact();

        Assert.IsTrue(porta.IsOpen);
        Assert.AreEqual("Fechar", porta.GetInteractionText());
    }

    [Test]
    public void Porta_EscreveOSeuEstadoNoMundo()
    {
        DoorInteractable porta = PortaMontada("porta-cave");

        porta.Interact();

        Assert.IsTrue(
            EstadoDoMundo.Instancia.LerBool("porta-cave", DoorInteractable.ChaveAberta),
            "abrir a porta não chegou ao world state");
    }

    [Test]
    public void Porta_LeOSeuEstadoDoMundoAoPreparar()
    {
        // O caso real da task: a porta foi aberta noutra época, e ao chegar a
        // esta já tem de estar aberta — sem se abrir à frente do jogador.
        EstadoDoMundo.Instancia.Registar(
            "porta-fabrica", DoorInteractable.ChaveAberta, true);

        DoorInteractable porta = PortaMontada("porta-fabrica");

        Assert.IsTrue(
            porta.IsOpen,
            "a porta não leu o seu estado do world state");
        Assert.IsFalse(
            porta.IsMoving,
            "uma porta que já estava aberta não se abre outra vez à frente do jogador");
    }

    [Test]
    public void Porta_AplicarAntesDePreparar_NaoCorrompeAsRotacoes()
    {
        // Regressão apanhada por revisor externo. Uma porta instanciada em
        // runtime — que é o que o world state passa a fazer — pode receber
        // Aplicar(true) antes do Start. O Preparar() recapturava as rotações às
        // cegas e tomava a pose ABERTA como sendo a fechada: a porta passava a
        // abrir 180º e a «fechar» para aberta.
        GameObject go = Novo("PortaInstanciada");

        DoorInteractable porta = go.AddComponent<DoorInteractable>();

        GameObject pivot = Novo("Pivot");
        pivot.transform.SetParent(go.transform);

        Campo(porta, "doorPivot").SetValue(porta, pivot.transform);
        Campo(porta, "stateId").SetValue(porta, "porta-instanciada");

        Quaternion fechada = pivot.transform.localRotation;

        // Alguém abre a porta antes de o Start correr.
        porta.Aplicar(true);

        Assert.AreNotEqual(
            fechada, pivot.transform.localRotation,
            "o Aplicar não chegou a mexer na porta — o teste não prova nada");

        porta.Preparar();

        // E agora fecha-se: tem de voltar exactamente à pose original.
        porta.Aplicar(false);

        Assert.AreEqual(
            0f,
            Quaternion.Angle(fechada, pivot.transform.localRotation),
            0.01f,
            "fechar a porta não a devolveu à pose fechada de origem");
    }

    [Test]
    public void Porta_SemStateId_ContinuaAAbrirEFechar()
    {
        // O revisor externo disse que a falta de `return` depois do LogError
        // era defeito. Não é: sair ali deixava a porta sem rotações capturadas,
        // ou seja imóvel. Uma porta sem stateId não persiste, mas funciona — e
        // ids em branco não partilham estado nenhum, porque o
        // EstadoDoMundo.Obter devolve null para eles.
        GameObject go = Novo("PortaSemIdQueAbre");

        DoorInteractable porta = go.AddComponent<DoorInteractable>();

        GameObject pivot = Novo("Pivot");
        pivot.transform.SetParent(go.transform);

        Campo(porta, "doorPivot").SetValue(porta, pivot.transform);

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
            "não tem stateId"));

        porta.Preparar();

        Quaternion fechada = pivot.transform.localRotation;

        porta.Aplicar(true);

        Assert.IsTrue(porta.IsOpen);
        Assert.AreNotEqual(
            fechada, pivot.transform.localRotation,
            "uma porta sem stateId ficou imóvel — perdeu-se mais do que se ganhou");

        // E não escreveu nada no mundo.
        Assert.AreEqual(
            0, EstadoDoMundo.Instancia.Total,
            "uma porta sem stateId não pode sujar o world state");
    }

    [Test]
    public void Porta_SemPivot_QueixaSeEmVezDeRebentar()
    {
        // Antes isto era um NullReferenceException. A FASE 7 passa a instanciar
        // portas em runtime, onde o estoiro sairia longe de quem o causou.
        GameObject go = Novo("PortaSemPivot");

        DoorInteractable porta = go.AddComponent<DoorInteractable>();
        Campo(porta, "stateId").SetValue(porta, "porta-sem-pivot");

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
            "não tem doorPivot ligado"));

        Assert.DoesNotThrow(() => porta.Preparar());
    }

    [Test]
    public void Porta_SemStateId_QueixaSe()
    {
        GameObject go = Novo("PortaSemId");

        DoorInteractable porta = go.AddComponent<DoorInteractable>();

        GameObject pivot = Novo("Pivot");
        Campo(porta, "doorPivot").SetValue(porta, pivot.transform);

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
            "não tem stateId"));

        porta.Preparar();
    }

    [Test]
    public void Gaveta_ComutaOEstadoEOVerbo()
    {
        DrawerInteractable gaveta = GavetaMontada(0.35f, 4f);

        Assert.IsFalse(gaveta.IsOpen);
        Assert.AreEqual("Abrir", gaveta.GetInteractionText());

        gaveta.Interact();

        Assert.IsTrue(gaveta.IsOpen, "o estado não é legível de fora");
        Assert.AreEqual("Fechar", gaveta.GetInteractionText());

    }

    [Test]
    public void Gaveta_NaoComutaEnquantoSeMove()
    {
        DrawerInteractable gaveta = GavetaMontada(0.35f, 4f);

        gaveta.Interact();
        gaveta.Interact();

        Assert.IsTrue(
            gaveta.IsOpen,
            "a segunda interacção apanhou a gaveta a meio caminho");

    }

    [Test]
    public void Interruptor_AcendeEApagaAsLuzes()
    {
        GameObject go = Novo("Interruptor");

        GameObject lampada = Novo("Lampada");
        Light luz = lampada.AddComponent<Light>();

        SwitchInteractable interruptor = go.AddComponent<SwitchInteractable>();
        typeof(SwitchInteractable)
            .GetField(
                "controlledLights",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
            .SetValue(interruptor, new[] { luz });

        // Start() é quem aplica o estado inicial às luzes.
        typeof(SwitchInteractable)
            .GetMethod(
                "Start",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
            .Invoke(interruptor, null);

        Assert.IsFalse(interruptor.IsOn);
        Assert.IsFalse(luz.enabled, "a luz ficou acesa com o interruptor desligado");
        Assert.AreEqual("Acender", interruptor.GetInteractionText());

        interruptor.Interact();

        Assert.IsTrue(interruptor.IsOn);
        Assert.IsTrue(luz.enabled);
        Assert.AreEqual("Apagar", interruptor.GetInteractionText());

    }

    [Test]
    public void Documento_AbreOEcraEEmpurraOModo()
    {
        GameObject host = Novo("Host");
        PlayerStateMachine maquina = host.AddComponent<PlayerStateMachine>();
        ReadingSystem leitura = host.AddComponent<ReadingSystem>();

        typeof(ReadingSystem)
            .GetField(
                "stateMachine",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
            .SetValue(leitura, maquina);

        Campo(leitura, "readingPanel").SetValue(leitura, Novo("Painel"));

        GameObject go = Novo("Documento");
        DocumentInteractable documento = go.AddComponent<DocumentInteractable>();
        typeof(DocumentInteractable)
            .GetField(
                "readingSystem",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
            .SetValue(documento, leitura);

        Assert.AreEqual("Ler", documento.GetInteractionText());
        Assert.IsFalse(documento.HasBeenRead);
        Assert.AreEqual(0, maquina.OpenModeCount);

        documento.Interact();

        Assert.IsTrue(documento.HasBeenRead, "a investigação não saberá que foi lido");
        Assert.IsTrue(leitura.IsReading);
        Assert.AreEqual(PlayerState.Reading, maquina.Current);
        Assert.AreEqual(1, maquina.OpenModeCount);

        leitura.Close();

        Assert.IsFalse(leitura.IsReading);
        Assert.AreEqual(0, maquina.OpenModeCount, "a camada ficou presa na pilha");

    }

    [Test]
    public void EcraDeLeitura_NaoLibertaOCursor()
    {
        // Lê-se com os olhos, não com o rato: se libertasse o cursor, a câmara
        // deixava de responder e o ecrã pedia um clique que não existe.
        GameObject host = Novo("Host");
        PlayerStateMachine maquina = host.AddComponent<PlayerStateMachine>();

        maquina.PushMode(PlayerState.Reading);

        Assert.IsFalse(maquina.IsCursorFree);
        Assert.IsTrue(maquina.IsMovementLocked);
        Assert.IsTrue(maquina.IsLookLocked);

    }

    // Os dois valores de inspector que matavam a gaveta para sempre. Ambos
    // eram plausíveis: openDistance negativo é a maneira óbvia de a fazer
    // abrir para o outro lado, e openSpeed a zero é um campo por preencher.
    [Test]
    public void Gaveta_ComOpenDistanceNegativo_RecusaSeEMantemViva()
    {
        LogAssert.Expect(
            LogType.Error,
            new System.Text.RegularExpressions.Regex("ambos têm de ser positivos"));

        DrawerInteractable gaveta = GavetaMontada(-0.35f, 4f);

        gaveta.Interact();

        Assert.IsFalse(
            gaveta.IsOpen,
            "comutou o estado com um valor que nunca converge");
        Assert.AreEqual("Abrir", gaveta.GetInteractionText());
    }

    [Test]
    public void Gaveta_ComOpenSpeedZero_RecusaSeEMantemViva()
    {
        LogAssert.Expect(
            LogType.Error,
            new System.Text.RegularExpressions.Regex("ambos têm de ser positivos"));

        DrawerInteractable gaveta = GavetaMontada(0.35f, 0f);

        gaveta.Interact();

        Assert.IsFalse(gaveta.IsOpen, "ficou presa a dizer Fechar sem se mexer");
    }

    [Test]
    public void Gaveta_ComValoresBons_Abre()
    {
        DrawerInteractable gaveta = GavetaMontada(0.35f, 4f);

        gaveta.Interact();

        Assert.IsTrue(gaveta.IsOpen);
    }

    private DrawerInteractable GavetaMontada(float distancia, float velocidade)
    {
        GameObject movel = Novo("Movel");
        GameObject frente = Novo("Frente");
        frente.transform.SetParent(movel.transform);

        DrawerInteractable gaveta = movel.AddComponent<DrawerInteractable>();

        Campo(gaveta, "drawerBody").SetValue(gaveta, frente.transform);
        Campo(gaveta, "openDistance").SetValue(gaveta, distancia);
        Campo(gaveta, "openSpeed").SetValue(gaveta, velocidade);

        Invocar(gaveta, "Start");

        return gaveta;
    }

    // O ecrã de leitura recusa-se a abrir sem painel: empurrar o modo na mesma
    // trancava o jogador num ecrã que não existia, e só o Esc o tirava de lá.
    [Test]
    public void EcraDeLeitura_SemPainel_NaoAbreNemTrancaOPlayer()
    {
        GameObject host = Novo("Host");
        PlayerStateMachine maquina = host.AddComponent<PlayerStateMachine>();
        ReadingSystem leitura = host.AddComponent<ReadingSystem>();
        Campo(leitura, "stateMachine").SetValue(leitura, maquina);

        GameObject go = Novo("Documento");
        DocumentInteractable documento = go.AddComponent<DocumentInteractable>();
        Campo(documento, "readingSystem").SetValue(documento, leitura);

        LogAssert.Expect(
            LogType.Error,
            new System.Text.RegularExpressions.Regex("Ecrã de leitura sem painel"));

        documento.Interact();

        Assert.IsFalse(leitura.IsReading);
        Assert.AreEqual(
            0,
            maquina.OpenModeCount,
            "trancou o jogador num ecrã que não existe");
        Assert.IsFalse(
            documento.HasBeenRead,
            "contou como lido sem o jogador ver uma linha");
    }

    private static System.Reflection.FieldInfo Campo(object alvo, string nome)
    {
        return alvo.GetType().GetField(
            nome,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
    }

    private static void Invocar(object alvo, string metodo)
    {
        alvo.GetType().GetMethod(
            metodo,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance)
            .Invoke(alvo, null);
    }
}
