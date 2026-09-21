using System.Collections.Generic;
using NUnit.Framework;
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
        GameObject go = Novo("Gaveta");
        DrawerInteractable gaveta = go.AddComponent<DrawerInteractable>();

        Assert.IsFalse(gaveta.IsOpen);
        Assert.AreEqual("Abrir", gaveta.GetInteractionText());

        gaveta.Interact();

        Assert.IsTrue(gaveta.IsOpen, "o estado não é legível de fora");
        Assert.AreEqual("Fechar", gaveta.GetInteractionText());

    }

    [Test]
    public void Gaveta_NaoComutaEnquantoSeMove()
    {
        GameObject go = Novo("Gaveta");
        DrawerInteractable gaveta = go.AddComponent<DrawerInteractable>();

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
}
