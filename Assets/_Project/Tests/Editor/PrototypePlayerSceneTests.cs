using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
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

    // Uma LayerMask a zero não é "referência por ligar" — passa despercebida ao
    // teste das referências e deixa o raycast a não acertar em nada. Falhava em
    // silêncio: sem prompt, sem erro, e a porta simplesmente deixava de existir.
    [Test]
    public void ORaycastDeInteraccaoTemMascaraEVeALayerInteractable()
    {
        InteractionSystem interaccao = ProcurarNaCena<InteractionSystem>();

        Assert.IsNotNull(
            interaccao,
            "A cena não tem InteractionSystem: nada do que a FASE 3 fez corre."
        );

        LayerMask mascara = (LayerMask)typeof(InteractionSystem)
            .GetField(
                "raycastMask",
                BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(interaccao);

        Assert.AreNotEqual(
            0,
            mascara.value,
            "raycastMask está a Nothing: o raycast não acerta em nada e " +
            "nenhum objecto da cena é interactivo."
        );

        Assert.AreNotEqual(
            0,
            mascara.value & ~(1 << layerInteractableCedo()),
            "A máscara só vê a layer Interactable: sem o mundo no raio não há " +
            "oclusão, e o foco atravessa paredes."
        );

        int layerInteractable = layerInteractableCedo();

        Assert.AreNotEqual(
            -1,
            layerInteractable,
            "A layer 'Interactable' desapareceu do TagManager (criada na FASE 1)."
        );

        Assert.AreNotEqual(
            0,
            mascara.value & (1 << layerInteractable),
            "A máscara do raycast não inclui a layer 'Interactable'."
        );
    }

    // Quem é interactivo tem de estar na layer que a máscara vê — senão o
    // objecto existe, tem script, e continua sem responder ao E.
    [Test]
    public void TodosOsInteractablesDaCenaEstaoNaLayerInteractable()
    {
        int layerInteractable = LayerMask.NameToLayer("Interactable");

        MonoBehaviour[] todos = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        int encontrados = 0;

        foreach (MonoBehaviour comportamento in todos)
        {
            if (comportamento == null || !(comportamento is IInteractable))
                continue;

            encontrados++;

            // O que tem de estar na layer 8 é o collider que se toca, e num
            // objecto composto (gaveta, interruptor) esse vive num filho —
            // exigi-lo ao GameObject do script contradizia o
            // GetComponentInParent e obrigava a pôr o móvel inteiro na layer.
            Collider[] colliders =
                comportamento.GetComponentsInChildren<Collider>(true);

            bool alcancavel = false;

            foreach (Collider collider in colliders)
            {
                if (collider.gameObject.layer == layerInteractable &&
                    !collider.isTrigger)
                {
                    alcancavel = true;
                    break;
                }
            }

            Assert.IsTrue(
                alcancavel,
                $"'{comportamento.name}' implementa IInteractable mas não tem " +
                "um collider sólido na layer 'Interactable': o raycast não o vê."
            );
        }

        Assert.Greater(
            encontrados,
            0,
            "A cena não tem um único IInteractable."
        );
    }

    // O quinto caso: o prompt tem UM dono. Não basta o InteractionSystem
    // comportar-se bem — é preciso que mais ninguém tenha por onde lhe mexer.
    // Enquanto o InspectionSystem também o referenciava, sair da inspecção
    // fazia-o piscar com o texto do objecto que já não estava em mira.
    [Test]
    public void OPromptDeInteraccaoTemUmSoDono()
    {
        InteractionSystem interaccao = ProcurarNaCena<InteractionSystem>();
        Assert.IsNotNull(interaccao, "A cena não tem InteractionSystem.");

        GameObject prompt = (GameObject)typeof(InteractionSystem)
            .GetField(
                "interactionPrompt",
                BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(interaccao);

        Assert.IsNotNull(prompt, "O InteractionSystem não tem prompt ligado.");

        foreach (MonoBehaviour comportamento in ComportamentosDoProjecto())
        {
            if (comportamento is InteractionSystem)
                continue;

            foreach (FieldInfo campo in comportamento.GetType().GetFields(
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance))
            {
                if (!typeof(Object).IsAssignableFrom(campo.FieldType))
                    continue;

                Object valor = (Object)campo.GetValue(comportamento);

                if (valor == null)
                    continue;

                // Não basta comparar com o GameObject do prompt: um campo a
                // apontar ao TMP_Text lá dentro dá o mesmo piscar, porque
                // SetActive num filho esconde a mesma coisa.
                GameObject alvo = valor as GameObject;

                if (alvo == null && valor is Component componente)
                    alvo = componente.gameObject;

                if (alvo == null)
                    continue;

                Assert.IsFalse(
                    alvo == prompt || alvo.transform.IsChildOf(prompt.transform),
                    $"'{comportamento.GetType().Name}.{campo.Name}' também " +
                    "alcança o prompt de interacção: são dois donos, e foi " +
                    "assim que o prompt piscava com o texto velho."
                );
            }
        }
    }

    // O realce da FASE 3 vive na mira. Se ela não estiver na cena e ligada,
    // não há realce nenhum — e isso não dá erro, só silêncio.
    [Test]
    public void AMiraExisteNaCenaEEstaLigadaAoFoco()
    {
        CrosshairSystem mira = ProcurarNaCena<CrosshairSystem>();

        Assert.IsNotNull(
            mira,
            "A cena não tem CrosshairSystem: o objecto focado não tem realce."
        );

        foreach (string campo in
            new[] { "crosshair", "interactionSystem", "stateMachine" })
        {
            Object valor = (Object)typeof(CrosshairSystem)
                .GetField(campo, BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(mira);

            Assert.IsNotNull(
                valor,
                $"CrosshairSystem.{campo} está por ligar."
            );
        }

        Graphic[] tracos = (Graphic[])typeof(CrosshairSystem)
            .GetField("strokes", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(mira);

        Assert.IsNotNull(tracos);
        Assert.Greater(
            tracos.Length,
            0,
            "A mira não tem traços: não há o que acender."
        );

        foreach (Graphic traco in tracos)
        {
            Assert.IsNotNull(traco, "Um traço da mira está por ligar.");
        }
    }

    // A regra da direcção artística: o realce é interface, nunca luz no mundo.
    // Se alguém puser luz num interactable, isto apanha-o — e a razão não é
    // estética: luz no objecto focado sabota o alinhamento de fotografias.
    [Test]
    public void NenhumInteractableDaCenaTemLuzPropria()
    {
        foreach (MonoBehaviour comportamento in ComportamentosDoProjecto())
        {
            if (!(comportamento is IInteractable))
                continue;

            Light[] luzes =
                comportamento.GetComponentsInChildren<Light>(true);

            Assert.AreEqual(
                0,
                luzes.Length,
                $"'{comportamento.name}' tem Light própria. O realce é a mira, " +
                "não o objecto: «sempre matéria, nunca luz» (Direcção " +
                "Artística §7.2), e luz no objecto focado sabota o alinhamento."
            );
        }
    }

    private static int layerInteractableCedo()
    {
        return LayerMask.NameToLayer("Interactable");
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

    // Os passos estiveram mudos porque não havia um único ficheiro de áudio no
    // projecto. Agora há — e isto falha se alguém os desligar outra vez.
    [Test]
    public void OsPassosTemSom()
    {
        FootstepSystem passos = ProcurarNaCena<FootstepSystem>();

        Assert.IsNotNull(passos, "Não há FootstepSystem na cena.");

        SerializedObject so = new SerializedObject(passos);
        SerializedProperty clips = so.FindProperty("defaultFootstepClips");

        Assert.IsNotNull(clips, "O campo defaultFootstepClips desapareceu.");

        Assert.Greater(
            clips.arraySize,
            0,
            "Sem clips por omissão os passos voltam a ser mudos."
        );

        for (int i = 0; i < clips.arraySize; i++)
        {
            Assert.IsNotNull(
                clips.GetArrayElementAtIndex(i).objectReferenceValue,
                $"defaultFootstepClips[{i}] está vazio."
            );
        }
    }

    // O som por superfície só se prova se houver ao menos uma superfície a
    // sobrepor-se aos clips por omissão.
    [Test]
    public void HaPeloMenosUmaSuperficieComSomProprio()
    {
        SurfaceAudio[] superficies = Object.FindObjectsByType<SurfaceAudio>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Assert.Greater(
            superficies.Length,
            0,
            "Nenhum objecto da cena tem SurfaceAudio: o caminho do som por " +
            "superfície nunca chega a correr em play mode."
        );

        foreach (SurfaceAudio superficie in superficies)
        {
            Assert.IsTrue(
                superficie.HasClips,
                $"O SurfaceAudio de '{superficie.name}' não tem clips — pisá-lo " +
                "cai nos clips por omissão sem ninguém dar por isso."
            );
        }
    }
}
