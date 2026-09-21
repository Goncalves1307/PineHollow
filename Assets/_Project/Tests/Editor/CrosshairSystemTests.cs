using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// Testes do realce. O que interessa é que o feedback do foco vive na mira e
// mais nada muda: nenhum material, nenhuma luz, nenhum shader — é isso que o
// mantém fora do alinhamento de fotografias.
public class CrosshairSystemTests
{
    private readonly List<GameObject> criados = new List<GameObject>();

    private CrosshairSystem sistema;
    private PlayerStateMachine maquina;
    private InteractionSystem interaccao;
    private RectTransform mira;
    private Image traco;

    [SetUp]
    public void SetUp()
    {
        GameObject host = Novo("Host");
        maquina = host.AddComponent<PlayerStateMachine>();
        interaccao = host.AddComponent<InteractionSystem>();
        sistema = host.AddComponent<CrosshairSystem>();

        GameObject miraGo = Novo("Crosshair");
        mira = miraGo.AddComponent<RectTransform>();

        GameObject tracoGo = Novo("H");
        tracoGo.transform.SetParent(miraGo.transform);
        traco = tracoGo.AddComponent<Image>();

        Campo("crosshair").SetValue(sistema, mira);
        Campo("strokes").SetValue(sistema, new Graphic[] { traco });
        Campo("interactionSystem").SetValue(sistema, interaccao);
        Campo("stateMachine").SetValue(sistema, maquina);

        Invocar("Start");
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

    private GameObject Novo(string nome)
    {
        GameObject go = new GameObject(nome);
        criados.Add(go);
        return go;
    }

    private static FieldInfo Campo(string nome)
    {
        return typeof(CrosshairSystem).GetField(
            nome, BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private void Invocar(string metodo)
    {
        typeof(CrosshairSystem)
            .GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(sistema, null);
    }

    // Muitos frames, para o MoveTowards chegar ao alvo.
    private void Ticks(int quantos)
    {
        for (int i = 0; i < quantos; i++)
            Invocar("Update");
    }

    private void PorFoco(bool tem)
    {
        typeof(InteractionSystem)
            .GetField("currentInteractable",
                BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(interaccao, tem ? new FakeInteractable() : null);
    }

    private class FakeInteractable : IInteractable
    {
        public void Interact() { }
        public string GetInteractionText() { return "Abrir"; }
    }

    [Test]
    public void EmRepouso_AMiraEstaDiscretaEVisivel()
    {
        Ticks(60);

        Assert.IsTrue(mira.gameObject.activeSelf);
        Assert.AreEqual(0.35f, traco.color.a, 0.01f, "repouso não é discreto");
        Assert.AreEqual(1f, mira.localScale.x, 0.01f);
    }

    [Test]
    public void ComFoco_AMiraAbreEGanhaPresenca()
    {
        PorFoco(true);
        Ticks(60);

        Assert.Greater(
            traco.color.a, 0.8f,
            "a mira não reagiu ao foco — o realce não acontece");
        Assert.Greater(
            mira.localScale.x, 1.3f,
            "a mira não abriu");
    }

    [Test]
    public void PerdidoOFoco_AMiraVoltaAoRepouso()
    {
        PorFoco(true);
        Ticks(60);

        PorFoco(false);
        Ticks(60);

        Assert.AreEqual(0.35f, traco.color.a, 0.01f, "ficou presa em destaque");
        Assert.AreEqual(1f, mira.localScale.x, 0.01f);
    }

    [Test]
    public void ComCamadaAberta_AMiraSaiDoEcra()
    {
        maquina.PushMode(PlayerState.Reading);
        Ticks(2);

        Assert.IsFalse(
            mira.gameObject.activeSelf,
            "a mira ficou por cima do ecrã de leitura");
    }

    [Test]
    public void ComOCursorSolto_AMiraSaiDoEcra()
    {
        maquina.PushMode(PlayerState.ViewingAlbum);
        Ticks(2);

        Assert.IsTrue(maquina.IsCursorFree, "pré-condição: cursor livre");
        Assert.IsFalse(mira.gameObject.activeSelf);
    }

    [Test]
    public void FechadaACamada_AMiraVolta()
    {
        maquina.PushMode(PlayerState.Reading);
        Ticks(2);
        Assert.IsFalse(mira.gameObject.activeSelf);

        maquina.PopMode(PlayerState.Reading);
        Ticks(2);

        Assert.IsTrue(mira.gameObject.activeSelf);
    }
}
