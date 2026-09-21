using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// A cena foi montada com o editor fechado, editando o YAML à mão. Estes testes
// existem para que isso não volte a passar sem rede: abrem a cena a sério e
// verificam que as ligações do inspector estão todas lá.
public class PrototypePlayerSceneTests
{
    private const string CenaPath =
        "Assets/_Project/Scenes/Prototype_Player.unity";

    // Campos que podem legitimamente estar vazios no inspector.
    private static readonly HashSet<string> VaziosPermitidos = new HashSet<string>
    {
        // Resolvido no Awake do FootstepSystem (AddComponent se preciso).
        "FootstepSystem.footstepSource",
    };

    private Scene cena;

    [SetUp]
    public void AbrirCena()
    {
        cena = EditorSceneManager.OpenScene(CenaPath, OpenSceneMode.Single);
    }

    [Test]
    public void ACenaAbre()
    {
        Assert.IsTrue(cena.IsValid(), "A cena não abriu: " + CenaPath);
        Assert.IsTrue(cena.isLoaded);
    }

    [Test]
    public void OPlayerTemAMaquinaDeEstadosEOsPassos()
    {
        PlayerController player = ProcurarNaCena<PlayerController>();

        Assert.IsNotNull(player, "Não há PlayerController na cena.");

        Assert.IsNotNull(
            player.GetComponent<PlayerStateMachine>(),
            "O Player não tem PlayerStateMachine — sem ela ninguém é dono do " +
            "cursor nem do Esc."
        );

        Assert.IsNotNull(
            player.GetComponent<FootstepSystem>(),
            "O Player não tem FootstepSystem."
        );

        Assert.IsNotNull(
            player.GetComponent<CharacterController>(),
            "O Player não tem CharacterController."
        );
    }

    // Todos os sistemas têm de apontar para a mesma máquina: duas máquinas
    // seriam duas pilhas, e o Esc voltava a ter dois donos.
    [Test]
    public void SoHaUmaMaquinaDeEstados_ETodosApontamParaEla()
    {
        PlayerStateMachine[] maquinas =
            Object.FindObjectsByType<PlayerStateMachine>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        Assert.AreEqual(
            1,
            maquinas.Length,
            "Devia haver exactamente uma PlayerStateMachine na cena."
        );

        PlayerStateMachine unica = maquinas[0];

        foreach (MonoBehaviour comportamento in ComportamentosDoProjecto())
        {
            SerializedObject so = new SerializedObject(comportamento);
            SerializedProperty prop = so.FindProperty("stateMachine");

            if (prop == null)
                continue;

            Assert.AreEqual(
                unica,
                prop.objectReferenceValue,
                comportamento.GetType().Name +
                ".stateMachine não aponta para a máquina da cena."
            );
        }
    }

    // O que mais me preocupava: uma referência perdida na edição do YAML.
    [Test]
    public void NenhumaReferenciaDoInspectorEstaPorLigar()
    {
        List<string> falhas = new List<string>();

        foreach (MonoBehaviour comportamento in ComportamentosDoProjecto())
        {
            string tipo = comportamento.GetType().Name;

            SerializedObject so = new SerializedObject(comportamento);
            SerializedProperty prop = so.GetIterator();

            while (prop.NextVisible(true))
            {
                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                if (prop.name == "m_Script")
                    continue;

                if (prop.objectReferenceValue != null)
                    continue;

                string chave = tipo + "." + prop.name;

                if (VaziosPermitidos.Contains(chave))
                    continue;

                falhas.Add(chave + "  (em '" + comportamento.name + "')");
            }
        }

        Assert.IsEmpty(
            falhas,
            "Referências do inspector por ligar:\n  " +
            string.Join("\n  ", falhas)
        );
    }

    [Test]
    public void NingemForaDaMaquinaEscreveNoCursorOuLeOEsc()
    {
        string[] scripts = System.IO.Directory.GetFiles(
            "Assets/_Project/Scripts",
            "*.cs",
            System.IO.SearchOption.AllDirectories
        );

        List<string> infractores = new List<string>();

        foreach (string caminho in scripts)
        {
            string nome = System.IO.Path.GetFileName(caminho);

            if (nome == "PlayerStateMachine.cs")
                continue;

            string codigo = System.IO.File.ReadAllText(caminho);

            if (codigo.Contains("Cursor.lockState") ||
                codigo.Contains("Cursor.visible"))
            {
                infractores.Add(nome + " escreve no cursor");
            }

            if (codigo.Contains("escapeKey"))
            {
                infractores.Add(nome + " lê o Esc");
            }
        }

        Assert.IsEmpty(
            infractores,
            "O cursor e o Esc têm um dono único (PlayerStateMachine). " +
            "Estes passaram à frente dele:\n  " +
            string.Join("\n  ", infractores)
        );
    }

    private static T ProcurarNaCena<T>() where T : Object
    {
        T[] achados = Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        return achados.Length > 0 ? achados[0] : null;
    }

    // Só os MonoBehaviours escritos neste projecto — nada de TextMeshPro nem
    // de componentes de UI da Unity.
    private static IEnumerable<MonoBehaviour> ComportamentosDoProjecto()
    {
        MonoBehaviour[] todos = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (MonoBehaviour comportamento in todos)
        {
            if (comportamento == null)
                continue;

            MonoScript script = MonoScript.FromMonoBehaviour(comportamento);

            if (script == null)
                continue;

            string caminho = AssetDatabase.GetAssetPath(script);

            if (string.IsNullOrEmpty(caminho))
                continue;

            if (!caminho.StartsWith("Assets/_Project/Scripts/"))
                continue;

            yield return comportamento;
        }
    }
}
