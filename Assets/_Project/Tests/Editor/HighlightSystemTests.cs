using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Testes do realce do objecto focado. O que interessa não é o aspecto — é que
// o realce aconteça, que saia quando o foco sai, e sobretudo que **não toque
// no material partilhado**: o projecto não tem materiais próprios, e pintar
// um material pintaria todos os objectos da cena de uma vez.
public class HighlightSystemTests
{
    private readonly List<GameObject> criados = new List<GameObject>();

    private HighlightSystem realce;

    [SetUp]
    public void SetUp()
    {
        GameObject host = Novo("Host");
        realce = host.AddComponent<HighlightSystem>();
        Invocar("Awake");
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

    private GameObject Cubo(string nome)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nome;
        criados.Add(go);
        return go;
    }

    private void Invocar(string metodo)
    {
        typeof(HighlightSystem)
            .GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(realce, null);
    }

    private void Ticks(int quantos)
    {
        for (int i = 0; i < quantos; i++)
            Invocar("Update");
    }

    [Test]
    public void SemFoco_NaoHaRealce()
    {
        Ticks(30);

        Assert.IsNull(realce.Focado);
        Assert.AreEqual(0f, realce.Intensidade, 0.001f);
    }

    [Test]
    public void ComFoco_ORealceSobe()
    {
        GameObject alvo = Cubo("Porta");

        realce.Focus(alvo);
        Ticks(60);

        Assert.AreSame(alvo, realce.Focado);
        Assert.Greater(
            realce.Intensidade, 0.9f,
            "o objecto focado não chegou a ser realçado");
    }

    [Test]
    public void PerdidoOFoco_ORealceSai()
    {
        GameObject alvo = Cubo("Porta");

        realce.Focus(alvo);
        Ticks(60);

        realce.Clear();

        Assert.IsNull(realce.Focado);
        Assert.AreEqual(
            0f, realce.Intensidade, 0.001f,
            "o objecto ficou realçado depois de sair de mira");
    }

    [Test]
    public void ORealceNaoTocaNoMaterialPartilhado()
    {
        // O defeito que isto existe para impedir: sem MaterialPropertyBlock,
        // realçar uma porta pintava todos os objectos da cena, porque partilham
        // o material por omissão — e instanciava um material a cada foco.
        GameObject alvo = Cubo("Porta");
        GameObject outro = Cubo("Parede");

        Material partilhado = alvo.GetComponent<Renderer>().sharedMaterial;
        Color antes = partilhado.GetColor("_BaseColor");

        Assert.AreSame(
            partilhado,
            outro.GetComponent<Renderer>().sharedMaterial,
            "pré-condição: os dois partilham o material por omissão");

        realce.Focus(alvo);
        Ticks(60);

        Assert.AreEqual(
            antes,
            partilhado.GetColor("_BaseColor"),
            "o material partilhado foi alterado — a cena inteira ficaria pintada");

        Assert.AreSame(
            partilhado,
            alvo.GetComponent<Renderer>().sharedMaterial,
            "o material foi instanciado: fuga a cada foco");
    }

    [Test]
    public void ACurvaVaiDaCorOriginalAteAoRealce()
    {
        Color origem = new Color(0.3f, 0.3f, 0.3f, 1f);

        Assert.AreEqual(origem, realce.Realcar(origem, 0f), "t=0 não é a origem");

        Color cheio = realce.Realcar(origem, 1f);

        Assert.Greater(
            cheio.r + cheio.g + cheio.b,
            origem.r + origem.g + origem.b,
            "o realce não clareia a matéria");
        Assert.Greater(
            cheio.r, cheio.b,
            "o realce devia puxar ao quente, e não ao frio");
        Assert.AreEqual(origem.a, cheio.a, 0.001f, "mexeu na transparência");
    }

    [Test]
    public void FocarOMesmoDuasVezes_NaoReiniciaORealce()
    {
        GameObject alvo = Cubo("Porta");

        realce.Focus(alvo);
        Ticks(60);

        float antes = realce.Intensidade;

        realce.Focus(alvo);

        Assert.AreEqual(
            antes, realce.Intensidade, 0.001f,
            "re-focar o mesmo objecto fez o realce recomeçar do zero");
    }

    [Test]
    public void ORealceAlcancaOsFilhos()
    {
        // A gaveta e o interruptor são compostos: o script está no móvel e a
        // malha que se vê está num filho.
        GameObject movel = Novo("Movel");
        GameObject frente = Cubo("Frente");
        frente.transform.SetParent(movel.transform);

        realce.Focus(movel);
        Ticks(60);

        Assert.AreSame(movel, realce.Focado);
        Assert.Greater(
            realce.Intensidade, 0.9f,
            "um objecto composto não foi realçado");
    }
}
