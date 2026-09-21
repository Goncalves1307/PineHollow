using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;

// Testes do InspectionSystem — o sistema grande que estava sem nenhum.
//
// Rotação e zoom não aparecem aqui de propósito: dependem de Mouse.current,
// que em EditMode é null, e os dois handlers saem na primeira linha. Testá-los
// obrigava a InputTestFixture, que nenhum teste deste repositório usa. O que se
// cobre é o que não passa pelo rato — pegar, largar, os limites, e as guardas
// que impedem o modo de ficar preso na pilha.
public class InspectionSystemTests
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
    public void Examinar_PoeOObjectoAFrenteDaCamaraEEmpurraOModo()
    {
        InspectionSystem sistema = SistemaMontado(out PlayerStateMachine maquina,
                                                  out GameObject camara,
                                                  out _);

        GameObject alvo = Novo("Caixa");

        sistema.Inspect(alvo);

        Assert.IsTrue(sistema.IsInspecting);
        Assert.AreSame(
            camara.transform,
            alvo.transform.parent,
            "o objecto não foi parar à frente da câmara");
        Assert.AreEqual(
            1.5f,
            alvo.transform.localPosition.z,
            0.0001f,
            "a distância inicial do inspector não foi respeitada");
        Assert.IsTrue(
            maquina.IsModeOpen(PlayerState.Inspecting),
            "o modo de inspecção não foi empurrado para a pilha");
    }

    // O campo do inspector podia estar fora dos limites do zoom, e o objecto
    // aparecia num sítio de onde o scroll nunca mais o trazia de volta.
    [Test]
    public void Examinar_ComDistanciaInicialForaDosLimites_ApanhaOLimite()
    {
        InspectionSystem sistema = SistemaMontado(out _, out _, out _);

        Campo(sistema, "inspectionDistance").SetValue(sistema, 9f);

        GameObject alvo = Novo("Caixa");

        sistema.Inspect(alvo);

        Assert.AreEqual(
            2.5f,
            alvo.transform.localPosition.z,
            0.0001f,
            "a distância inicial escapou ao limite máximo do zoom");
    }

    [Test]
    public void Examinar_ComOutroJaNaMao_NaoLargaOPrimeiro()
    {
        InspectionSystem sistema = SistemaMontado(out PlayerStateMachine maquina,
                                                  out _, out _);

        GameObject primeiro = Novo("Caixa");
        GameObject segundo = Novo("Moldura");

        sistema.Inspect(primeiro);
        sistema.Inspect(segundo);

        Assert.IsNull(
            segundo.transform.parent,
            "o segundo objecto foi para a mão por cima do primeiro");
        Assert.AreEqual(
            1,
            maquina.OpenModeCount,
            "empurrou o modo de inspecção duas vezes");
    }

    // Sem câmara não há inspecção nenhuma. O que não pode acontecer é ficar o
    // modo na pilha: o jogador ficava trancado, com o objecto ainda no chão.
    [Test]
    public void Examinar_SemCamara_NaoTrancaOJogador()
    {
        InspectionSystem sistema = SistemaMontado(out PlayerStateMachine maquina,
                                                  out _, out _);

        Campo(sistema, "playerCamera").SetValue(sistema, null);

        GameObject alvo = Novo("Caixa");

        LogAssert.Expect(LogType.Error, new Regex("por ligar"));

        sistema.Inspect(alvo);

        Assert.IsFalse(sistema.IsInspecting);
        Assert.AreEqual(
            0,
            maquina.OpenModeCount,
            "trancou o jogador num modo que nunca chegou a abrir");
        Assert.IsNull(
            alvo.transform.parent,
            "mexeu no objecto apesar de não ter câmara para o pôr à frente");
    }

    [Test]
    public void Examinar_ComDescricao_MostraOTexto()
    {
        InspectionSystem sistema = SistemaMontado(out _, out _, out TMPro.TMP_Text texto);

        sistema.Inspect(Novo("Caixa"), "Uma caixa de madeira.");

        Assert.IsTrue(
            texto.gameObject.activeSelf,
            "a linha da descrição ficou escondida com texto para mostrar");
        Assert.AreEqual("Uma caixa de madeira.", texto.text);
    }

    // Um objecto sem nada para dizer não mostra uma caixa vazia no ecrã.
    [Test]
    public void Examinar_SemDescricao_NaoMostraLinhaVazia()
    {
        InspectionSystem sistema = SistemaMontado(out _, out _, out TMPro.TMP_Text texto);

        sistema.Inspect(Novo("Caixa"));

        Assert.IsFalse(
            texto.gameObject.activeSelf,
            "mostrou a linha da descrição sem descrição nenhuma");
    }

    [Test]
    public void Largar_DevolveOObjectoAoSitioELimpaOEcra()
    {
        InspectionSystem sistema = SistemaMontado(out PlayerStateMachine maquina,
                                                  out _,
                                                  out TMPro.TMP_Text texto);

        GameObject movel = Novo("Movel");
        GameObject alvo = Novo("Caixa");
        alvo.transform.SetParent(movel.transform);
        alvo.transform.position = new Vector3(3f, 1f, 7f);
        alvo.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

        Vector3 posicao = alvo.transform.position;
        Quaternion rotacao = alvo.transform.rotation;

        sistema.Inspect(alvo, "Uma caixa de madeira.");

        Invocar(sistema, "ExitInspection");

        Assert.IsFalse(sistema.IsInspecting);
        Assert.AreSame(
            movel.transform,
            alvo.transform.parent,
            "o objecto não voltou para onde estava pendurado");
        Assert.That(
            Vector3.Distance(posicao, alvo.transform.position),
            Is.LessThan(0.0001f),
            "o objecto não voltou ao sítio");
        Assert.That(
            Quaternion.Angle(rotacao, alvo.transform.rotation),
            Is.LessThan(0.01f),
            "o objecto ficou virado ao contrário do que estava");
        Assert.AreEqual(
            0,
            maquina.OpenModeCount,
            "deixou o modo de inspecção preso na pilha");
        Assert.IsFalse(
            texto.gameObject.activeSelf,
            "a descrição ficou no ecrã depois de largar o objecto");
    }

    // O Start não pode confiar no que ficou guardado na cena: um GameObject
    // deixado activo no editor virava HUD permanente, que o GDD proíbe.
    [Test]
    public void AoArrancar_OEcraDaInspeccaoEstaDesligado()
    {
        InspectionSystem sistema = SistemaMontado(out _, out _, out TMPro.TMP_Text texto);

        GameObject controlos =
            (GameObject)Campo(sistema, "inspectionControls").GetValue(sistema);

        controlos.SetActive(true);
        texto.gameObject.SetActive(true);

        Invocar(sistema, "Start");

        Assert.IsFalse(controlos.activeSelf);
        Assert.IsFalse(texto.gameObject.activeSelf);
    }

    // Uma dependência de UI por ligar tem de se queixar. Calada, dá um exame
    // sem controlos nem descrição e ninguém percebe porquê.
    [Test]
    public void AoArrancar_ComOEcraPorLigar_Queixa_se()
    {
        InspectionSystem sistema = SistemaMontado(out _, out _, out _);

        Campo(sistema, "inspectionControls").SetValue(sistema, null);

        LogAssert.Expect(LogType.Error, new Regex("ecrã da inspecção por ligar"));

        Invocar(sistema, "Start");
    }

    // Enquanto não houver clips — e houve meses em que não houve — isto tem de
    // correr em silêncio e não estoirar.
    [Test]
    public void Examinar_SemClips_NaoEstoira()
    {
        InspectionSystem sistema = SistemaMontado(out _, out _, out _);

        Campo(sistema, "pickupClips").SetValue(sistema, new AudioClip[0]);
        Campo(sistema, "putdownClips").SetValue(sistema, null);

        GameObject alvo = Novo("Caixa");

        Assert.DoesNotThrow(() =>
        {
            sistema.Inspect(alvo);
            Invocar(sistema, "ExitInspection");
        });
    }

    private InspectionSystem SistemaMontado(
        out PlayerStateMachine maquina,
        out GameObject camara,
        out TMPro.TMP_Text texto)
    {
        GameObject host = Novo("Player");

        maquina = host.AddComponent<PlayerStateMachine>();

        // O AudioSource tem de existir aqui. Sem ele, `Tocar` saía na primeira
        // cláusula e o teste dos clips nunca chegava ao código que diz testar —
        // era verde com a guarda dos arrays apagada.
        AudioSource fonte = host.AddComponent<AudioSource>();

        camara = Novo("Camara");
        camara.AddComponent<Camera>();

        InspectionSystem sistema = host.AddComponent<InspectionSystem>();

        GameObject controlos = Novo("InspectionControls");
        controlos.SetActive(false);

        GameObject linha = Novo("InspectionDescription");
        texto = linha.AddComponent<TMPro.TextMeshProUGUI>();
        linha.SetActive(false);

        Campo(sistema, "playerCamera").SetValue(sistema, camara.GetComponent<Camera>());
        Campo(sistema, "stateMachine").SetValue(sistema, maquina);
        Campo(sistema, "inspectionControls").SetValue(sistema, controlos);
        Campo(sistema, "descriptionText").SetValue(sistema, texto);
        Campo(sistema, "inspectionSource").SetValue(sistema, fonte);

        return sistema;
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
