using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Valida a fundação técnica da FASE 1 (CU-869f4yfeg).
/// Corre no editor (menu) ou em batchmode:
///   Unity -batchmode -quit -projectPath . -executeMethod Fase1Validacao.Correr -logFile -
/// Em batchmode sai com código 1 se alguma verificação falhar.
/// </summary>
public static class Fase1Validacao
{
    // As layers que a FASE 1 fixa. A FASE 3 (869f4yb37) depende de Interactable existir.
    private static readonly Dictionary<int, string> LayersEsperadas =
        new Dictionary<int, string>
        {
            { 8, "Interactable" },
            { 9, "Player" },
            { 10, "PhotoOnly" },
            { 11, "IgnorePhoto" },
        };

    // UI e PhotoOnly são marcadores de rendering: não participam em física.
    private static readonly int[] LayersSemFisica = { 5, 10 };

    private const string CenaBootstrap = "Assets/_Project/Scenes/Bootstrap.unity";
    private const string CenaJogo = "Assets/_Project/Scenes/Prototype_Player.unity";

    private static List<string> falhas;

    [MenuItem("Pine Hollow/Validar FASE 1")]
    public static void Correr()
    {
        falhas = new List<string>();

        ValidarLayers();
        ValidarTags();
        ValidarMatrizDeColisao();
        ValidarEstruturaDePastas();
        ValidarBuildSettings();
        ValidarQualidade();
        ValidarCena();
        ValidarArranque();

        if (falhas.Count == 0)
        {
            Debug.Log("[FASE 1] Todas as verificações passaram.");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }

            return;
        }

        Debug.LogError(
            "[FASE 1] " + falhas.Count + " verificação(ões) falharam:\n  - " +
            string.Join("\n  - ", falhas)
        );

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao)
        {
            falhas.Add(mensagem);
        }
    }

    private static void ValidarLayers()
    {
        foreach (KeyValuePair<int, string> par in LayersEsperadas)
        {
            string real = LayerMask.LayerToName(par.Key);

            Exigir(
                real == par.Value,
                "layer " + par.Key + " devia ser '" + par.Value +
                "' e é '" + real + "'"
            );
        }
    }

    private static void ValidarTags()
    {
        // Nada no projecto lê tags personalizadas — a lista vazia é a resposta certa.
        // O que faltava era a tag built-in MainCamera, verificada em ValidarCena().
        string[] personalizadas = UnityEditorInternal.InternalEditorUtility.tags
            .Except(new[]
            {
                "Untagged", "Respawn", "Finish", "EditorOnly", "MainCamera",
                "Player", "GameController",
            })
            .ToArray();

        Exigir(
            personalizadas.Length == 0,
            "há tags personalizadas que nenhum script lê: " +
            string.Join(", ", personalizadas)
        );
    }

    private static void ValidarMatrizDeColisao()
    {
        foreach (int semFisica in LayersSemFisica)
        {
            for (int outra = 0; outra < 32; outra++)
            {
                Exigir(
                    Physics.GetIgnoreLayerCollision(semFisica, outra),
                    "layer " + semFisica + " (" + LayerMask.LayerToName(semFisica) +
                    ") devia ignorar colisões com a layer " + outra + " e não ignora"
                );
            }
        }

        // As layers de jogo continuam a colidir entre si.
        Exigir(
            !Physics.GetIgnoreLayerCollision(8, 9),
            "Interactable e Player deviam colidir e não colidem"
        );

        Exigir(
            !Physics.GetIgnoreLayerCollision(9, 0),
            "Player e Default deviam colidir e não colidem"
        );
    }

    private static void ValidarEstruturaDePastas()
    {
        string[] esperadas =
        {
            "Assets/_Project",
            "Assets/_Project/Art",
            "Assets/_Project/Audio",
            "Assets/_Project/Materials",
            "Assets/_Project/Models",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Scenes",
            "Assets/_Project/Scripts",
            "Assets/_Project/Settings",
            "Assets/_Project/UI",
            "Assets/Environment",
            "Assets/ThirdParty",
        };

        foreach (string pasta in esperadas)
        {
            Exigir(
                AssetDatabase.IsValidFolder(pasta),
                "falta a pasta " + pasta
            );
        }

        Exigir(
            !AssetDatabase.IsValidFolder("Assets/TutorialInfo"),
            "Assets/TutorialInfo ainda existe (restos do template)"
        );
    }

    private static void ValidarBuildSettings()
    {
        EditorBuildSettingsScene[] cenas = EditorBuildSettings.scenes;

        Exigir(
            cenas.Length == 2,
            "as build settings deviam ter 2 cenas e têm " + cenas.Length
        );

        if (cenas.Length >= 1)
        {
            Exigir(
                cenas[0].path == CenaBootstrap && cenas[0].enabled,
                "a primeira cena devia ser " + CenaBootstrap +
                " activa e é '" + cenas[0].path + "'"
            );
        }

        if (cenas.Length >= 2)
        {
            Exigir(
                cenas[1].path == CenaJogo && cenas[1].enabled,
                "a segunda cena devia ser " + CenaJogo +
                " activa e é '" + cenas[1].path + "'"
            );
        }

        Exigir(
            cenas.All(c => !c.path.Contains("SampleScene")),
            "a SampleScene ainda está nas build settings"
        );
    }

    private static void ValidarQualidade()
    {
        string[] niveis = QualitySettings.names;

        Exigir(
            niveis.Length == 1,
            "devia haver um só nível de qualidade e há " + niveis.Length +
            " (" + string.Join(", ", niveis) + ")"
        );

        Exigir(
            !niveis.Contains("Mobile"),
            "o nível Mobile ainda existe (Android e iOS estão fora do âmbito)"
        );

        var rp = QualitySettings.GetRenderPipelineAssetAt(0)
            as UniversalRenderPipelineAsset;

        Exigir(rp != null, "o nível de qualidade 0 não tem um URP asset ligado");

        if (rp == null)
        {
            return;
        }

        Exigir(
            Mathf.Approximately(rp.shadowDistance, 80f),
            "a distância de sombras devia ser 80 e é " + rp.shadowDistance
        );

        Exigir(
            rp.shadowCascadeCount == 4,
            "deviam ser 4 cascades e são " + rp.shadowCascadeCount
        );
    }

    private static void ValidarCena()
    {
        if (!System.IO.File.Exists(CenaJogo))
        {
            falhas.Add("não existe " + CenaJogo);
            return;
        }

        // Abrir em Single fecha o que estiver aberto. No editor isso levaria o
        // trabalho por gravar de quem carregou no menu, por isso perguntamos
        // primeiro e repomos a cena original no fim.
        string cenaOriginal = EditorSceneManager.GetActiveScene().path;

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            falhas.Add("validação cancelada: havia alterações por gravar");
            return;
        }

        var cena = EditorSceneManager.OpenScene(CenaJogo, OpenSceneMode.Single);

        Exigir(cena.IsValid(), "não consegui abrir " + CenaJogo);

        if (!cena.IsValid())
        {
            return;
        }

        Camera[] camaras = UnityEngine.Object
            .FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        Exigir(
            camaras.Length == 2,
            "esperava 2 câmaras na cena e encontrei " + camaras.Length
        );

        Exigir(
            camaras.Any(c => c.CompareTag("MainCamera")),
            "nenhuma câmara tem a tag MainCamera — Camera.main devolve null"
        );

        foreach (Camera camara in camaras)
        {
            var dados = camara.GetUniversalAdditionalCameraData();

            Exigir(
                dados != null && dados.renderPostProcessing,
                "a câmara '" + camara.name +
                "' tem o pós-processamento desligado"
            );
        }

        Camera captura = camaras.FirstOrDefault(
            c => c.name == "PhotoCaptureCamera"
        );

        Exigir(captura != null, "não encontrei a PhotoCaptureCamera");

        if (captura != null)
        {
            Exigir(
                (captura.cullingMask & (1 << 5)) == 0,
                "a PhotoCaptureCamera ainda vê a layer UI"
            );

            Exigir(
                (captura.cullingMask & (1 << 11)) == 0,
                "a PhotoCaptureCamera ainda vê a layer IgnorePhoto " +
                "(a máquina apareceria na própria fotografia)"
            );
        }

        Camera jogador = camaras.FirstOrDefault(
            c => c.CompareTag("MainCamera")
        );

        if (jogador != null)
        {
            Exigir(
                (jogador.cullingMask & (1 << 10)) == 0,
                "a câmara do jogador ainda vê a layer PhotoOnly"
            );
        }

        var volumes = UnityEngine.Object.FindObjectsByType<Volume>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Exigir(
            // sharedProfile, não profile: o getter de .profile instancia um
            // clone do perfil e marca a cena como suja só por a validarmos.
            volumes.Any(v => v.isGlobal && v.sharedProfile != null),
            "a cena não tem um Volume global com perfil — " +
            "nada do que a direcção artística decidiu se vê"
        );

        ReporCena(cenaOriginal);
    }

    /// <summary>
    /// Devolve o editor à cena que lá estava antes de validarmos.
    /// </summary>
    /// <summary>
    /// A Bootstrap e o unico caminho de arranque da build. Se a referencia de
    /// script se perder — um .meta recriado num merge chega — o jogador fica
    /// num ecra preto para sempre e nada mais aqui daria por isso.
    /// </summary>
    private static void ValidarArranque()
    {
        if (!System.IO.File.Exists(CenaBootstrap))
        {
            falhas.Add("não existe " + CenaBootstrap);
            return;
        }

        string cenaOriginal = EditorSceneManager.GetActiveScene().path;

        EditorSceneManager.OpenScene(CenaBootstrap, OpenSceneMode.Single);

        var carregadores = UnityEngine.Object.FindObjectsByType<BootstrapLoader>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Exigir(
            carregadores.Length == 1,
            "a Bootstrap devia ter exactamente um BootstrapLoader e tem " +
            carregadores.Length +
            " — se for 0, a referência de script perdeu-se e a build arranca " +
            "num ecrã preto"
        );

        if (carregadores.Length == 1)
        {
            var serializado = new SerializedObject(carregadores[0]);

            string cena = serializado
                .FindProperty("gameSceneName")
                .stringValue;

            Exigir(
                !string.IsNullOrEmpty(cena),
                "o BootstrapLoader não tem cena de jogo configurada"
            );

            Exigir(
                EditorBuildSettings.scenes.Any(
                    c => c.enabled &&
                         System.IO.Path.GetFileNameWithoutExtension(c.path) == cena
                ),
                "o BootstrapLoader carrega '" + cena + "', que não é nenhuma " +
                "cena activa das build settings — LoadScene rebenta em runtime"
            );
        }

        ReporCena(cenaOriginal);
    }

    private static void ReporCena(string caminhoOriginal)
    {
        if (Application.isBatchMode ||
            string.IsNullOrEmpty(caminhoOriginal) ||
            caminhoOriginal == CenaJogo ||
            caminhoOriginal == CenaBootstrap ||
            !System.IO.File.Exists(caminhoOriginal))
        {
            return;
        }

        EditorSceneManager.OpenScene(caminhoOriginal, OpenSceneMode.Single);
    }
}
