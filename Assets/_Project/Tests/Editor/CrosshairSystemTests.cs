using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

// Testes da mira. Ela já não reage ao foco — isso é do HighlightSystem, no
// objecto. O que lhe resta, e é o que se testa, é estar lá e sair do ecrã
// quando o player não manda.
public class CrosshairSystemTests
{
    private readonly List<GameObject> criados = new List<GameObject>();

    private CrosshairSystem sistema;
    private PlayerStateMachine maquina;
    private RectTransform mira;
    private Image traco;

    [SetUp]
    public void SetUp()
    {
        GameObject host = Novo("Host");
        maquina = host.AddComponent<PlayerStateMachine>();
        sistema = host.AddComponent<CrosshairSystem>();

        GameObject miraGo = Novo("Crosshair");
        mira = miraGo.AddComponent<RectTransform>();

        GameObject tracoGo = Novo("H");
        tracoGo.transform.SetParent(miraGo.transform);
        traco = tracoGo.AddComponent<Image>();

        Campo("crosshair").SetValue(sistema, mira);
        Campo("strokes").SetValue(sistema, new Graphic[] { traco });
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

    private void Ticks(int quantos)
    {
        for (int i = 0; i < quantos; i++)
            Invocar("Update");
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

    [Test]
    public void EmRepouso_AMiraEstaLaEDiscreta()
    {
        Ticks(5);

        Assert.IsTrue(mira.gameObject.activeSelf, "a mira não está no ecrã");
        Assert.AreEqual(
            0.35f, traco.color.a, 0.01f,
            "a mira devia ser discreta — o GDD proíbe HUD pesado");
    }
}
