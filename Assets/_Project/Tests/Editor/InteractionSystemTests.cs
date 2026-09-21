using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Testes do raycast de interação. Cobrem o que motivou a FASE 3: até aqui o
// raio partia sem máscara e sem guarda de estado, e nenhum destes casos se
// reproduzia na cena do protótipo — que tem três colliders e nenhum trigger.
// A partir da FASE 10 reproduzem-se todos.
public class InteractionSystemTests
{
    private const int LayerInteractable = 8;

    private GameObject host;
    private GameObject cameraHost;
    private InteractionSystem interaction;
    private PlayerStateMachine stateMachine;
    private GameObject prompt;

    // A limpeza vive toda aqui e não no fim de cada teste: um assert que falha
    // lança, e o DestroyImmediate que viesse a seguir nunca corria — os
    // colliders sobreviviam e faziam falhar os testes seguintes por arrasto.
    private readonly List<GameObject> criados = new List<GameObject>();

    // Alvo de teste: conta as interacções e diz o verbo, como qualquer
    // interactable real.
    private class FakeInteractable : MonoBehaviour, IInteractable
    {
        public int Interactions;
        public void Interact() { Interactions++; }
        public string GetInteractionText() { return "Abrir"; }
    }

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("InteractionSystemTests");
        stateMachine = host.AddComponent<PlayerStateMachine>();
        interaction = host.AddComponent<InteractionSystem>();

        cameraHost = new GameObject("Camera");
        cameraHost.transform.position = Vector3.zero;
        cameraHost.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        Camera camera = cameraHost.AddComponent<Camera>();

        prompt = new GameObject("InteractionPrompt");

        Set("playerCamera", camera);
        Set("interactionDistance", 3f);
        Set("interactableMask", (LayerMask)(1 << LayerInteractable));
        Set("interactionPrompt", prompt);
        Set("stateMachine", stateMachine);
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

        Object.DestroyImmediate(prompt);
        Object.DestroyImmediate(cameraHost);
        Object.DestroyImmediate(host);
    }

    private void Set(string field, object value)
    {
        typeof(InteractionSystem)
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(interaction, value);
    }

    private void Tick()
    {
        Physics.SyncTransforms();

        typeof(InteractionSystem)
            .GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(interaction, null);
    }

    private GameObject Box(Vector3 at, int layer, bool isTrigger)
    {
        GameObject go = new GameObject("Box");
        criados.Add(go);
        go.layer = layer;
        go.transform.position = at;

        BoxCollider collider = go.AddComponent<BoxCollider>();
        collider.isTrigger = isTrigger;

        return go;
    }

    [Test]
    public void ObjectoNaLayerInteractable_GanhaFocoEPoeOPrompt()
    {
        GameObject alvo = Box(new Vector3(0f, 0f, 2f), LayerInteractable, false);
        alvo.AddComponent<FakeInteractable>();

        Tick();

        Assert.IsTrue(interaction.HasFocus);
        Assert.IsTrue(prompt.activeSelf);

    }

    [Test]
    public void TriggerAFrente_NaoRoubaOFocoAoObjectoAtras()
    {
        // O defeito original: m_QueriesHitTriggers está a 1 e o raio parava no
        // primeiro trigger volume que aparecesse à frente da porta.
        GameObject trigger = Box(new Vector3(0f, 0f, 1f), LayerInteractable, true);

        GameObject alvo = Box(new Vector3(0f, 0f, 2f), LayerInteractable, false);
        alvo.AddComponent<FakeInteractable>();

        Tick();

        Assert.IsTrue(interaction.HasFocus, "o trigger roubou o foco");

    }

    [Test]
    public void ColliderNaoInteractivoAFrente_NaoMataAInteraccao()
    {
        // Uma grade, um vidro, um puxador sem script: fora da máscara, o raio
        // atravessa-os. Antes, o mais próximo ganhava e devolvia null.
        GameObject grade = Box(new Vector3(0f, 0f, 1f), 0, false);

        GameObject alvo = Box(new Vector3(0f, 0f, 2f), LayerInteractable, false);
        alvo.AddComponent<FakeInteractable>();

        Tick();

        Assert.IsTrue(interaction.HasFocus, "a grade matou a interacção");

    }

    [Test]
    public void ColliderNumFilho_ResolveNoPai()
    {
        // A gaveta e o interruptor são objectos compostos: o script está no
        // móvel, o collider que se toca está na frente da gaveta.
        GameObject pai = new GameObject("Movel");
        criados.Add(pai);
        pai.transform.position = new Vector3(0f, 0f, 2f);
        FakeInteractable script = pai.AddComponent<FakeInteractable>();

        GameObject filho = Box(new Vector3(0f, 0f, 2f), LayerInteractable, false);
        filho.transform.SetParent(pai.transform);

        Tick();

        Assert.IsTrue(
            interaction.HasFocus,
            "GetComponent não via o script no pai");

        Assert.AreEqual(
            0,
            script.Interactions,
            "ter foco não é interagir: o Interact() não foi premido");
    }

    [Test]
    public void ForaDoAlcance_NaoHaFoco()
    {
        GameObject alvo = Box(new Vector3(0f, 0f, 5f), LayerInteractable, false);
        alvo.AddComponent<FakeInteractable>();

        Tick();

        Assert.IsFalse(interaction.HasFocus);
        Assert.IsFalse(prompt.activeSelf);

    }

    [Test]
    public void ComCamadaAberta_NaoHaFocoNemPrompt()
    {
        // O bug jogável: em modo fotografia o raycast continuava a correr e o E
        // abria portas por trás da UI.
        GameObject alvo = Box(new Vector3(0f, 0f, 2f), LayerInteractable, false);
        alvo.AddComponent<FakeInteractable>();

        Tick();
        Assert.IsTrue(interaction.HasFocus, "pré-condição: devia ter foco");

        stateMachine.PushMode(PlayerState.Photographing);
        Tick();

        Assert.IsFalse(interaction.HasFocus, "interagiu por trás da UI");
        Assert.IsFalse(prompt.activeSelf, "o prompt ficou visível por trás da UI");

    }

    [Test]
    public void FechadaACamada_OFocoVolta()
    {
        GameObject alvo = Box(new Vector3(0f, 0f, 2f), LayerInteractable, false);
        alvo.AddComponent<FakeInteractable>();

        stateMachine.PushMode(PlayerState.Inspecting);
        Tick();
        Assert.IsFalse(interaction.HasFocus);

        stateMachine.PopMode(PlayerState.Inspecting);
        Tick();

        Assert.IsTrue(interaction.HasFocus);
        Assert.IsTrue(prompt.activeSelf);

    }
}
