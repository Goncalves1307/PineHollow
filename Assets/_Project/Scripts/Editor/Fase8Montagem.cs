using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Monta na cena a primeira coisa que a FASE 8 acrescenta: a fotografia que muda
// quando o jogador volta de 1986 (subtask 869f4yb8z).
//
// Três peças, e nenhuma serve sem as outras:
//
//   - as duas imagens da fotografia do lago, antes e depois. São desenhadas
//     aqui, píxel a píxel, e não renderizadas por uma câmara: esta montagem
//     corre em batchmode com -nographics, onde uma câmara não desenha nada. E
//     são PROVISÓRIAS — a fotografia a sério é a FASE 18. Sem elas a mudança
//     não se via, porque até aqui nenhuma fotografia do projecto tinha imagem;
//   - o PhotoAsset «Thomas junto ao lago, 1986» (Historia:217), com a variante
//     que vale quando o mundo diz que o jogador esteve lá;
//   - o stateId que o SistemaDeFlashback escreve ao voltar, e que tem de ser
//     EXACTAMENTE o que a variante lê. É por isso que os dois são escritos aqui
//     a partir da mesma constante, e não à mão em dois inspectors.
//
// Decalcado do Fase7Montagem: pergunta antes de abrir cenas, e nenhum Garantir*
// sai cedo por a coisa já existir — cada corrida volta a escrever os campos no
// que encontrar. Correr isto duas vezes não duplica nada e põe tudo em ordem
// das duas.
//
// Corre pelo menu ou em batchmode:
//   Unity -batchmode -nographics -quit -projectPath . \
//         -executeMethod Fase8Montagem.Correr -logFile -
public static class Fase8Montagem
{
    private const string CaminhoDaCena =
        "Assets/_Project/Scenes/Prototype_Player.unity";

    private const string PastaDasImagens = "Assets/_Project/Art/Fotografias";
    private const string PastaDasFotografias = "Assets/_Project/Photography";

    public const string NomeDaFotografia = "Fotografia_Lago_1986";

    // O que o primeiro flashback deixa escrito no mundo, e o que a fotografia
    // do lago lê. Um só sítio para os dois lados.
    public const string StateIdDoPrimeiroFlashback = "flashback-lago-1986";

    public const string CaminhoDaImagemAntes =
        PastaDasImagens + "/Lago_1986_Antes.png";

    public const string CaminhoDaImagemDepois =
        PastaDasImagens + "/Lago_1986_Depois.png";

    // Canon à letra, Pine_Hollow_Historia.md:253. Não diz quem é a figura: o
    // «É o protagonista» da linha seguinte é o que o jogador conclui, não o
    // que o caderno lhe diz — a ambiguidade é uma regra de desenho.
    private const string Revelacao = "Agora existe uma figura atrás de Thomas.";

    private static int mudancas;

    // Fora do batchmode o Falhar não sai do editor — só escreve no log. Sem
    // isto o Correr continuava depois de uma falha, gravava a cena meio montada
    // (o stateId escrito, a fotografia por pousar) e dizia «montada».
    private static bool falhou;

    [MenuItem("Pine Hollow/FASE 8 — montar a fotografia que muda")]
    public static void Correr()
    {
        mudancas = 0;
        falhou = false;

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("FASE 8: montagem cancelada — havia cenas por gravar.");

            return;
        }

        Texture2D antes = GarantirImagem(CaminhoDaImagemAntes, false);
        Texture2D depois = GarantirImagem(CaminhoDaImagemDepois, true);

        if (antes == null || depois == null)
        {
            Falhar("As imagens da fotografia do lago não importaram.");

            return;
        }

        PhotoAsset lago = GarantirFotografia(antes, depois);

        if (falhou)
            return;

        Scene cena = EditorSceneManager.OpenScene(CaminhoDaCena, OpenSceneMode.Single);

        SistemaDeFlashback sistema = Object.FindAnyObjectByType<SistemaDeFlashback>(
            FindObjectsInactive.Include
        );

        if (sistema == null)
        {
            Falhar("Não há SistemaDeFlashback na cena — corre primeiro a " +
                   "montagem da FASE 7. Sem ele ninguém escreve a marca que a " +
                   "fotografia lê.");

            return;
        }

        SerializedObject soSistema = new SerializedObject(sistema);
        Definir(soSistema, "stateIdDaPresenca", StateIdDoPrimeiroFlashback);
        soSistema.ApplyModifiedPropertiesWithoutUndo();

        PousarNoMundo(lago);

        if (falhou)
        {
            // A cena fica aberta e suja no editor, para quem correu ver o que
            // ficou por fazer — mas não se grava por cima do que estava bem.
            Debug.LogError("FASE 8: montagem interrompida, a cena NÃO foi gravada.");

            return;
        }

        if (mudancas > 0)
        {
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            AssetDatabase.SaveAssets();

            Debug.Log($"FASE 8: fotografia que muda montada, {mudancas} alterações gravadas.");
        }
        else
        {
            Debug.Log("FASE 8: já estava montado, nada a fazer.");
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    // ---------------------------------------------------------------
    // As imagens
    // ---------------------------------------------------------------

    // Escreve o PNG só se os bytes mudaram. O desenho é determinístico — o grão
    // é um hash da posição, não um Random —, e é isso que deixa correr a
    // montagem sem sujar o git com uma imagem igual mas regravada.
    private static Texture2D GarantirImagem(string caminho, bool comFigura)
    {
        byte[] png = Desenhar(comFigura);

        string noDisco = Path.Combine(
            Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
            caminho
        );

        bool igual = File.Exists(noDisco) && Iguais(File.ReadAllBytes(noDisco), png);

        if (!igual)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(noDisco) ?? string.Empty);
            File.WriteAllBytes(noDisco, png);
            AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);

            Debug.Log($"FASE 8: imagem provisória escrita em {caminho}.");

            mudancas++;
        }

        AcertarImportacao(caminho);

        return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
    }

    // As fotografias autoradas são 2048×1368, que NÃO é potência de dois. Com a
    // importação por omissão o Unity escala-as para 2048×1024 — a proporção 3:2
    // perdia-se, o PhotoAsset queixava-se do tamanho, e a comparação sobrepunha
    // uma imagem esmagada a outra que não o estava.
    private static void AcertarImportacao(string caminho)
    {
        TextureImporter importador = AssetImporter.GetAtPath(caminho) as TextureImporter;

        if (importador == null)
            return;

        bool mudou = false;

        if (importador.npotScale != TextureImporterNPOTScale.None)
        {
            importador.npotScale = TextureImporterNPOTScale.None;
            mudou = true;
        }

        if (importador.mipmapEnabled)
        {
            // É UI: desenha-se num RawImage ao tamanho do ecrã, nunca ao longe.
            importador.mipmapEnabled = false;
            mudou = true;
        }

        if (importador.maxTextureSize < PhotoAsset.LarguraAutorada)
        {
            importador.maxTextureSize = PhotoAsset.LarguraAutorada;
            mudou = true;
        }

        if (!mudou)
            return;

        importador.SaveAndReimport();

        mudancas++;
    }

    private static bool Iguais(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    // Um fim de tarde de 1986 junto ao lago: céu encoberto, a linha das
    // árvores, a água, a margem e o Thomas de pé nela. A versão «depois» tem uma
    // segunda figura mais atrás, perto das árvores, mais pequena e mais
    // desfocada — está lá, mas não salta à vista. Quem já viu a primeira versão
    // repara; quem não viu pode nem dar por ela.
    //
    // Cores dentro da disciplina de albedo do repo (50–240 sRGB) e já com o
    // desbotado de uma cópia de 1986: nada aqui é preto nem branco puros.
    internal static byte[] Desenhar(bool comFigura)
    {
        int largura = PhotoAsset.LarguraAutorada;
        int altura = PhotoAsset.AlturaAutorada;

        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGB24, false);

        Color32[] px = new Color32[largura * altura];

        Color ceuAlto = De(0xB8, 0xB4, 0xA6);
        Color ceuBaixo = De(0xD2, 0xC9, 0xB2);
        Color arvores = De(0x3E, 0x44, 0x3C);
        Color agua = De(0x6E, 0x78, 0x78);
        Color reflexo = De(0x9A, 0x9E, 0x94);
        Color margem = De(0x5C, 0x56, 0x48);
        Color figura = De(0x36, 0x34, 0x32);

        // Em fracção da altura, contada de baixo para cima — é como o Texture2D
        // endereça as linhas.
        const float Margem = 0.22f;
        const float Agua = 0.46f;
        const float Arvores = 0.62f;

        for (int y = 0; y < altura; y++)
        {
            float v = y / (float)altura;

            for (int x = 0; x < largura; x++)
            {
                float u = x / (float)largura;

                // A linha das árvores não é recta: pinheiros, uma serra irregular.
                float topoDasArvores = Arvores +
                    0.035f * Mathf.Abs(Mathf.Sin(u * 61f)) +
                    0.02f * Mathf.Sin(u * 17f + 1.3f);

                Color c;

                if (v < Margem)
                {
                    c = margem;
                }
                else if (v < Agua)
                {
                    // A água é mais clara ao longe e tem uma faixa de reflexo.
                    float t = (v - Margem) / (Agua - Margem);
                    c = Color.Lerp(agua, reflexo, t * 0.6f);

                    if (Mathf.Abs(v - 0.40f) < 0.006f + 0.004f * Mathf.Sin(u * 90f))
                        c = Color.Lerp(c, ceuBaixo, 0.5f);
                }
                else if (v < topoDasArvores)
                {
                    c = arvores;
                }
                else
                {
                    float t = (v - Arvores) / (1f - Arvores);
                    c = Color.Lerp(ceuBaixo, ceuAlto, t);
                }

                px[y * largura + x] = c;
            }
        }

        // O Thomas: de pé na margem, à direita do centro, a olhar para a água.
        Pessoa(px, largura, altura, 0.60f, 0.10f, 0.34f, figura, 0f);

        // A figura atrás dele. Mais pequena porque está mais longe, mais clara
        // porque a névoa do lago a come, e esbatida nas bordas porque o foco
        // era o Thomas.
        if (comFigura)
            Pessoa(px, largura, altura, 0.47f, 0.19f, 0.2f, Color.Lerp(figura, arvores, 0.45f), 0.35f);

        Grao(px, largura, altura);
        Vinheta(px, largura, altura);

        textura.SetPixels32(px);
        textura.Apply(false);

        byte[] png = textura.EncodeToPNG();

        Object.DestroyImmediate(textura);

        return png;
    }

    // Um vulto de pé: tronco, pernas e cabeça. Nada de rosto — a 1986 do
    // jogo não identifica ninguém pela cara, identifica pela fotografia.
    private static void Pessoa(
        Color32[] px, int largura, int altura,
        float centroU, float peV, float alturaV,
        Color cor, float suavidade)
    {
        float meiaLargura = alturaV * 0.13f * altura / largura;

        for (int y = 0; y < altura; y++)
        {
            float v = y / (float)altura;

            if (v < peV || v > peV + alturaV)
                continue;

            float h = (v - peV) / alturaV;

            for (int x = 0; x < largura; x++)
            {
                float u = x / (float)largura;
                float du = (u - centroU) / meiaLargura;

                float dentro;

                if (h > 0.84f)
                {
                    // Cabeça: um círculo em espaço de píxeis, não de UV.
                    float dy = (h - 0.92f) * alturaV * altura;
                    float dx = (u - centroU) * largura;
                    float raio = 0.08f * alturaV * altura;
                    dentro = raio - Mathf.Sqrt(dx * dx + dy * dy);
                    dentro /= raio;
                }
                else if (h > 0.42f)
                {
                    // Tronco, a alargar para os ombros.
                    float ombro = Mathf.Lerp(0.8f, 1f, (h - 0.42f) / 0.42f);
                    dentro = ombro - Mathf.Abs(du);
                }
                else
                {
                    // Duas pernas.
                    dentro = 0.34f - Mathf.Abs(Mathf.Abs(du) - 0.38f);
                }

                if (dentro <= 0f)
                    continue;

                float alfa = suavidade <= 0f
                    ? 1f
                    : Mathf.Clamp01(dentro / suavidade);

                int i = y * largura + x;
                px[i] = Color.Lerp(px[i], cor, alfa);
            }
        }
    }

    // O grão de película. Um hash da posição e não um Random, para a imagem
    // sair igual byte a byte em todas as corridas.
    private static void Grao(Color32[] px, int largura, int altura)
    {
        for (int i = 0; i < px.Length; i++)
        {
            uint h = (uint)i * 2654435761u;
            h ^= h >> 13;
            h *= 0x5bd1e995u;
            h ^= h >> 15;

            int ruido = (int)(h % 17u) - 8;

            Color32 c = px[i];
            c.r = (byte)Mathf.Clamp(c.r + ruido, 50, 240);
            c.g = (byte)Mathf.Clamp(c.g + ruido, 50, 240);
            c.b = (byte)Mathf.Clamp(c.b + ruido, 50, 240);
            px[i] = c;
        }
    }

    private static void Vinheta(Color32[] px, int largura, int altura)
    {
        for (int y = 0; y < altura; y++)
        {
            float v = y / (float)altura - 0.5f;

            for (int x = 0; x < largura; x++)
            {
                float u = x / (float)largura - 0.5f;
                float d = Mathf.Sqrt(u * u + v * v) / 0.707f;
                float f = 1f - 0.28f * d * d;

                int i = y * largura + x;
                Color32 c = px[i];
                c.r = (byte)Mathf.Max(50, c.r * f);
                c.g = (byte)Mathf.Max(50, c.g * f);
                c.b = (byte)Mathf.Max(50, c.b * f);
                px[i] = c;
            }
        }
    }

    private static Color De(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    // ---------------------------------------------------------------
    // O asset
    // ---------------------------------------------------------------

    // Cria-o se faltar e, falte ou não, volta a escrever-lhe os campos. É a
    // diferença para o GarantirFotografia da FASE 5, que devolvia o asset que
    // encontrasse tal como estava — e é por isso que lá as fotografias antigas
    // ficaram sem a descoberta quando a montagem mudou.
    private static PhotoAsset GarantirFotografia(Texture2D antes, Texture2D depois)
    {
        string caminho = $"{PastaDasFotografias}/{NomeDaFotografia}.asset";

        PhotoAsset asset = AssetDatabase.LoadAssetAtPath<PhotoAsset>(caminho);

        if (asset == null)
        {
            if (!AssetDatabase.IsValidFolder(PastaDasFotografias))
                AssetDatabase.CreateFolder("Assets/_Project", "Photography");

            asset = ScriptableObject.CreateInstance<PhotoAsset>();

            AssetDatabase.CreateAsset(asset, caminho);

            Debug.Log($"FASE 8: fotografia autorada criada em {caminho}.");

            mudancas++;
        }

        SerializedObject so = new SerializedObject(asset);

        Definir(so, "fotografia.id", NomeDaFotografia);
        Definir(so, "fotografia.ano", 1986);

        // Só o ano. A Historia não data esta fotografia, e um dia inventado é
        // o género de placeholder que sobrevive até ao jogo sair.
        Definir(so, "fotografia.data", "1986");
        Definir(so, "fotografia.local", "Lago");
        DefinirLista(so, "fotografia.personagens", "Thomas Hale");
        Definir(so, "fotografia.frente", "Thomas junto ao lago.");

        // Sem verso: não há verso canónico para esta fotografia.
        Definir(so, "fotografia.verso", string.Empty);
        Ligar(so, "fotografia.imagem", antes);
        Definir(so, "fotografia.autorada", true);

        SerializedProperty variantes = so.FindProperty("fotografia.variantes");

        if (variantes.arraySize != 1)
        {
            variantes.arraySize = 1;
            mudancas++;
        }

        string v = "fotografia.variantes.Array.data[0].";

        Definir(so, v + "stateId", StateIdDoPrimeiroFlashback);
        Definir(so, v + "chave", SistemaDeFlashback.ChaveDaPresenca);
        Definir(so, v + "valor", AlteracaoDeMundo.Escrever(true));
        Ligar(so, v + "imagem", depois);
        Definir(so, v + "frente", "Thomas junto ao lago. Atrás dele, perto das árvores, uma figura.");
        Definir(so, v + "verso", string.Empty);
        DefinirLista(so, v + "personagens");
        Definir(so, v + "revelacao", Revelacao);

        so.ApplyModifiedPropertiesWithoutUndo();

        return asset;
    }

    // ---------------------------------------------------------------
    // No mundo
    // ---------------------------------------------------------------

    // Ao lado das duas da FASE 5, que é onde o jogador já está a olhar. O sítio
    // definitivo — o estúdio do Ethan — é a ronda 2, e chega com a casa.
    private static void PousarNoMundo(PhotoAsset asset)
    {
        InspectionSystem inspeccao = Object.FindAnyObjectByType<InspectionSystem>(
            FindObjectsInactive.Include
        );

        PhotographySystem fotografia = Object.FindAnyObjectByType<PhotographySystem>(
            FindObjectsInactive.Include
        );

        if (inspeccao == null || fotografia == null)
        {
            Falhar("Falta o InspectionSystem ou o PhotographySystem na cena.");

            return;
        }

        PhotoInteractable interactable = null;

        foreach (PhotoInteractable candidato in Object.FindObjectsByType<PhotoInteractable>(
                     FindObjectsInactive.Include))
        {
            if (candidato != null && candidato.gameObject.name == NomeDaFotografia)
                interactable = candidato;
        }

        if (interactable == null)
        {
            // Decalca o objecto de uma das fotografias da FASE 5 em vez de o
            // desenhar outra vez: o volume fino, o colisor engordado, a layer
            // e o material provisório já foram acertados lá, com capturas.
            GameObject molde = GameObject.Find("Fotografia_Estudio_1994");

            GameObject go;

            if (molde != null)
            {
                go = Object.Instantiate(molde, molde.transform.parent);
                go.transform.position = molde.transform.position + new Vector3(0.35f, 0f, 0f);
                go.transform.rotation = molde.transform.rotation;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = new Vector3(0.7f, 1f, 2f);
                go.transform.localScale = new Vector3(0.18f, 0.12f, 0.002f);
                go.layer = LayerMask.NameToLayer("Interactable");

                BoxCollider colisor = go.GetComponent<BoxCollider>();

                if (colisor != null)
                    colisor.size = new Vector3(1f, 1f, 30f);

                go.AddComponent<PhotoInteractable>();
            }

            go.name = NomeDaFotografia;

            interactable = go.GetComponent<PhotoInteractable>();

            Debug.Log($"FASE 8: {NomeDaFotografia} pousada na cena em {go.transform.position}.");

            mudancas++;
        }

        SerializedObject so = new SerializedObject(interactable);

        Ligar(so, "inspectionSystem", inspeccao);
        Ligar(so, "photographySystem", fotografia);
        Ligar(so, "fotografia", asset);
        Definir(so, "stateId", NomeDaFotografia);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------
    // Utilitários
    // ---------------------------------------------------------------

    private static void Definir(SerializedObject so, string campo, string valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null)
        {
            Falhar($"O campo '{campo}' não existe em {so.targetObject.name}.");

            return;
        }

        if (prop.stringValue == valor)
            return;

        prop.stringValue = valor;

        mudancas++;
    }

    private static void Definir(SerializedObject so, string campo, int valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || prop.intValue == valor)
            return;

        prop.intValue = valor;

        mudancas++;
    }

    private static void Definir(SerializedObject so, string campo, bool valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || prop.boolValue == valor)
            return;

        prop.boolValue = valor;

        mudancas++;
    }

    private static void DefinirLista(SerializedObject so, string campo, params string[] valores)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null)
            return;

        if (prop.arraySize != valores.Length)
        {
            prop.arraySize = valores.Length;
            mudancas++;
        }

        for (int i = 0; i < valores.Length; i++)
        {
            SerializedProperty item = prop.GetArrayElementAtIndex(i);

            if (item.stringValue == valores[i])
                continue;

            item.stringValue = valores[i];
            mudancas++;
        }
    }

    private static void Ligar(SerializedObject so, string campo, Object valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || prop.objectReferenceValue == valor)
            return;

        prop.objectReferenceValue = valor;

        mudancas++;
    }

    private static void Falhar(string mensagem)
    {
        falhou = true;

        Debug.LogError("FASE 8: " + mensagem);

        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}
