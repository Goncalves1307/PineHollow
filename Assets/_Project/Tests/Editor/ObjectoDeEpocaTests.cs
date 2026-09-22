using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Testes dos objectos que só existem numa das épocas.
//
// O teste que interessa é o da varredura incluir os DESLIGADOS: um objecto de
// 1986 está desligado enquanto se joga em 2026, e uma varredura que só visse os
// activos nunca mais o encontrava para o voltar a acender. O flashback entrava
// numa cidade vazia e nada dava erro.
//
// Não se afere o número devolvido pelo AplicarATodos: ele varre a cena inteira,
// e a cena que outro teste deixe aberta entra na conta. Afere-se o estado dos
// objectos deste teste, que é o que se está a verificar.
public class ObjectoDeEpocaTests
{
    private readonly List<GameObject> criados = new List<GameObject>();

    private GameObject Novo(string nome)
    {
        GameObject go = new GameObject(nome);
        criados.Add(go);
        return go;
    }

    private ObjectoDeEpoca Marcador(string nome, bool em2026, bool em1986)
    {
        ObjectoDeEpoca objecto = Novo(nome).AddComponent<ObjectoDeEpoca>();

        Campo(objecto, "existeEm2026").SetValue(objecto, em2026);
        Campo(objecto, "existeEm1986").SetValue(objecto, em1986);

        return objecto;
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

        EstadoDoMundo.Esquecer();
    }

    [Test]
    public void Existe_RespondePorEpoca()
    {
        ObjectoDeEpoca so1986 = Marcador("So1986", false, true);

        Assert.IsTrue(so1986.Existe(Epoca.Passado1986));
        Assert.IsFalse(so1986.Existe(Epoca.Presente2026));
    }

    [Test]
    public void Aplicar_DesligaOQueNaoExisteNaEpoca()
    {
        ObjectoDeEpoca so1986 = Marcador("So1986", false, true);

        Assert.IsTrue(so1986.Aplicar(Epoca.Presente2026), "devia ter mudado");
        Assert.IsFalse(so1986.gameObject.activeSelf);

        Assert.IsTrue(so1986.Aplicar(Epoca.Passado1986), "devia ter voltado a ligar");
        Assert.IsTrue(so1986.gameObject.activeSelf);
    }

    [Test]
    public void Aplicar_DevolveFalseQuandoNaoHaNadaAFazer()
    {
        ObjectoDeEpoca sempre = Marcador("Sempre", true, true);

        Assert.IsFalse(
            sempre.Aplicar(Epoca.Presente2026),
            "quem não muda não conta como mudança — quem monta a cena usa isto " +
            "para decidir se grava");
    }

    [Test]
    public void AplicarATodos_EncontraOsQueEstaoDESLIGADOS()
    {
        // Este é o teste que justifica o FindObjectsInactive.Include. Sem ele,
        // o objecto de 1986 ficava desligado para sempre a partir da primeira
        // vez que se jogasse em 2026.
        ObjectoDeEpoca so1986 = Marcador("So1986", false, true);

        ObjectoDeEpoca.AplicarATodos(Epoca.Presente2026);

        Assert.IsFalse(so1986.gameObject.activeSelf, "devia estar desligado em 2026");

        ObjectoDeEpoca.AplicarATodos(Epoca.Passado1986);

        Assert.IsTrue(
            so1986.gameObject.activeSelf,
            "a varredura não encontrou o objecto desligado — 1986 ficaria vazio");
    }

    [Test]
    public void AplicarATodos_AcertaOsDoisLadosNaMesmaPassagem()
    {
        ObjectoDeEpoca so2026 = Marcador("So2026", true, false);
        ObjectoDeEpoca so1986 = Marcador("So1986", false, true);
        ObjectoDeEpoca sempre = Marcador("Sempre", true, true);

        ObjectoDeEpoca.AplicarATodos(Epoca.Passado1986);

        Assert.IsFalse(so2026.gameObject.activeSelf);
        Assert.IsTrue(so1986.gameObject.activeSelf);
        Assert.IsTrue(sempre.gameObject.activeSelf, "a geometria comum não se apaga");
    }

    private static System.Reflection.FieldInfo Campo(object alvo, string nome)
    {
        return alvo.GetType().GetField(
            nome,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
    }
}
