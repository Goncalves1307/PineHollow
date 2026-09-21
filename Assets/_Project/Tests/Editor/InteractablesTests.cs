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
