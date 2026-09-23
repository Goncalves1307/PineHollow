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

    public const string CaminhoDaImagemLago1994 =
        PastaDasImagens + "/Lago_1994.png";

    public const string CaminhoDaImagemCidade1986 =
        PastaDasImagens + "/Cidade_1986.png";

    // As duas do capítulo 1 que substituem as da FASE 5, e os nomes que elas
    // tinham — a montagem migra os antigos para os novos.
    public const string NomeDaCidade1986 = "Fotografia_Cidade_1986";
    public const string NomeDoLago1994 = "Fotografia_Lago_1994";

    private const string AntigoDe1986 = "Fotografia_Estudio_1986";
    private const string AntigoDe1994 = "Fotografia_Estudio_1994";

    // O que o par do lago revela. É também o id que o elo «As fotografias ligam
    // diferentes épocas» da cadeia da FASE 6 exige.
    public const string DescobertaDoLago = "thomas-nao-mudou";

    // O id que o elo «Thomas morreu em 1986» da cadeia da FASE 6 exige desde
    // que foi escrita. Até aqui nenhum documento o tinha.
    public const string StateIdDoObituario = "obituario-thomas-hale";

    // A nota do Ethan que já estava na cena desde a FASE 3.
    public const string StateIdDaNotaDoEthan = "prototipo/documento";

    // Placeholder marcado como tal: o texto definitivo é do Diogo, e um texto
    // provisório que não o diga tem o hábito de sobreviver (AGENTS.md).
    public const string MarcaDeProvisorio = "[PROVISÓRIO — texto do Diogo]";

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

        Texture2D lago1994 = GarantirImagem(CaminhoDaImagemLago1994, Cena.Lago1994);
        Texture2D cidade1986 = GarantirImagem(CaminhoDaImagemCidade1986, Cena.Cidade1986);

        if (lago1994 == null || cidade1986 == null)
        {
            Falhar("As imagens da cidade de 1986 e do lago de 1994 não importaram.");

            return;
        }

        Migrar(AntigoDe1986, NomeDaCidade1986);
        Migrar(AntigoDe1994, NomeDoLago1994);

        PhotoAsset lago = GarantirFotografia(antes, depois);
        PhotoAsset lagoDepois = GarantirLago1994(lago1994);
        PhotoAsset cidade = GarantirCidade1986(cidade1986);

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

        MigrarNaCena(AntigoDe1986, NomeDaCidade1986, cidade);
        MigrarNaCena(AntigoDe1994, NomeDoLago1994, lagoDepois);

        PousarNoMundo(lago);

        EscreverAsNotas();

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
        return GarantirImagem(caminho, comFigura ? Cena.LagoDepois : Cena.LagoAntes);
    }

    private static Texture2D GarantirImagem(string caminho, Cena cena)
    {
        byte[] png = Desenhar(cena);

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

    // As quatro imagens provisórias do slice. Uma só função de desenho por
    // família (o lago, a cidade) e não uma por imagem: o lago de 1994 TEM de
    // ser o mesmo enquadramento que o de 1986, com o Thomas no mesmo píxel, ou
    // o alinhamento declarado em (0,0) deixava de ser o que o jogador vê.
    internal enum Cena
    {
        LagoAntes,
        LagoDepois,
        Lago1994,
        Cidade1986
    }

    internal static byte[] Desenhar(bool comFigura)
    {
        return Desenhar(comFigura ? Cena.LagoDepois : Cena.LagoAntes);
    }

    internal static byte[] Desenhar(Cena cena)
    {
        int largura = PhotoAsset.LarguraAutorada;
        int altura = PhotoAsset.AlturaAutorada;

        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGB24, false);

        Color32[] px = new Color32[largura * altura];

        if (cena == Cena.Cidade1986)
            DesenharCidade(px, largura, altura);
        else
            DesenharLago(px, largura, altura, cena);

        Grao(px, largura, altura);
        Vinheta(px, largura, altura);

        textura.SetPixels32(px);
        textura.Apply(false);

        byte[] png = textura.EncodeToPNG();

        Object.DestroyImmediate(textura);

        return png;
    }

    // Um fim de tarde junto ao lago: céu encoberto, a linha das árvores, a
    // água, a margem e o Thomas de pé nela. A versão «depois» tem uma segunda
    // figura mais atrás, perto das árvores, mais pequena e mais desfocada —
    // está lá, mas não salta à vista. Quem já viu a primeira versão repara;
    // quem não viu pode nem dar por ela.
    //
    // A de 1994 é o mesmo sítio e o mesmo homem, no mesmo lugar e na mesma
    // pose — é isso que o jogador tem de ver ao sobrepô-las: Thomas não mudou
    // (Historia:221). Muda a cor, que é o que oito anos de película mudam.
    //
    // Cores dentro da disciplina de albedo do repo (50–240 sRGB), e as de 1986
    // já com o desbotado de uma cópia antiga: nada aqui é preto nem branco
    // puros. As de 1986 não se tocam — as imagens já estão no git, e um tom
    // mexido regravava-as com o mesmo conteúdo.
    private static void DesenharLago(Color32[] px, int largura, int altura, Cena cena)
    {
        bool de1994 = cena == Cena.Lago1994;

        Color ceuAlto = de1994 ? De(0xA4, 0xB0, 0xB4) : De(0xB8, 0xB4, 0xA6);
        Color ceuBaixo = de1994 ? De(0xC6, 0xCC, 0xC4) : De(0xD2, 0xC9, 0xB2);
        Color arvores = de1994 ? De(0x34, 0x46, 0x3A) : De(0x3E, 0x44, 0x3C);
        Color agua = de1994 ? De(0x5A, 0x70, 0x78) : De(0x6E, 0x78, 0x78);
        Color reflexo = de1994 ? De(0x8C, 0x9C, 0x9C) : De(0x9A, 0x9E, 0x94);
        Color margem = de1994 ? De(0x54, 0x52, 0x44) : De(0x5C, 0x56, 0x48);
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
        // O mesmo sítio nas três versões, ao píxel.
        Pessoa(px, largura, altura, 0.60f, 0.10f, 0.34f, figura, 0f);

        // A figura atrás dele. Mais pequena porque está mais longe, mais clara
        // porque a névoa do lago a come, e esbatida nas bordas porque o foco
        // era o Thomas.
        if (cena == Cena.LagoDepois)
            Pessoa(px, largura, altura, 0.47f, 0.19f, 0.2f, Color.Lerp(figura, arvores, 0.45f), 0.35f);
    }

    // Pine Hollow em 1986: uma fila de casas de madeira, janelas, a rua. Numa
    // das janelas do primeiro andar, um homem (Historia:205-209). É pequeno de
    // propósito — o jogador pode nem reparar à primeira, e o capítulo diz que
    // ainda não sabe quem ele é.
    private static void DesenharCidade(Color32[] px, int largura, int altura)
    {
        Color ceuAlto = De(0xB4, 0xB2, 0xA6);
        Color ceuBaixo = De(0xCE, 0xC6, 0xB0);
        Color rua = De(0x5E, 0x5C, 0x56);
        Color passeio = De(0x86, 0x82, 0x76);
        Color telhado = De(0x4A, 0x44, 0x40);
        Color vidro = De(0x4E, 0x56, 0x58);
        Color caixilho = De(0xC4, 0xBE, 0xAC);
        Color figura = De(0x38, 0x36, 0x34);

        // Três fachadas de ripado, cada uma com a sua cor desbotada.
        Color[] fachadas =
        {
            De(0x9C, 0x8E, 0x78),
            De(0x8A, 0x94, 0x8C),
            De(0xA8, 0x9A, 0x86)
        };

        for (int y = 0; y < altura; y++)
        {
            float v = y / (float)altura;
            float t = Mathf.Clamp01((v - 0.7f) / 0.3f);

            for (int x = 0; x < largura; x++)
                px[y * largura + x] = Color.Lerp(ceuBaixo, ceuAlto, t);
        }

        Rectangulo(px, largura, altura, 0f, 0f, 1f, 0.12f, rua);
        Rectangulo(px, largura, altura, 0f, 0.12f, 1f, 0.16f, passeio);

        for (int i = 0; i < 3; i++)
        {
            float u0 = 0.04f + i * 0.32f;
            float u1 = u0 + 0.29f;
            float topo = 0.66f + 0.03f * i;

            Rectangulo(px, largura, altura, u0, 0.16f, u1, topo, fachadas[i]);

            // O ripado: linhas horizontais mais escuras, de 2 em 2 %.
            for (float v = 0.17f; v < topo; v += 0.02f)
                Rectangulo(px, largura, altura, u0, v, u1, v + 0.003f, Color.Lerp(fachadas[i], telhado, 0.35f));

            Rectangulo(px, largura, altura, u0 - 0.01f, topo, u1 + 0.01f, topo + 0.04f, telhado);

            // Duas janelas por andar.
            for (int andar = 0; andar < 2; andar++)
            {
                float v0 = 0.24f + andar * 0.2f;

                for (int j = 0; j < 2; j++)
                {
                    float w0 = u0 + 0.05f + j * 0.13f;

                    Rectangulo(px, largura, altura, w0 - 0.006f, v0 - 0.01f, w0 + 0.086f, v0 + 0.13f, caixilho);
                    Rectangulo(px, largura, altura, w0, v0, w0 + 0.08f, v0 + 0.12f, vidro);
                }
            }
        }

        // O homem na janela: primeiro andar da casa do meio, janela da
        // direita (u 0.54–0.62, v 0.44–0.56). Recortado ao vidro — só se vê
        // cabeça e tronco, que é o que uma janela mostra.
        Pessoa(px, largura, altura, 0.58f, 0.35f, 0.2f, figura, 0.25f,
               0.54f, 0.44f, 0.62f, 0.56f);

        // O pinázio passa por cima dele: está DENTRO da casa.
        Rectangulo(px, largura, altura, 0.577f, 0.44f, 0.583f, 0.56f, caixilho);
    }

    private static void Rectangulo(
        Color32[] px, int largura, int altura,
        float u0, float v0, float u1, float v1, Color cor)
    {
        int x0 = Mathf.Clamp(Mathf.RoundToInt(u0 * largura), 0, largura);
        int x1 = Mathf.Clamp(Mathf.RoundToInt(u1 * largura), 0, largura);
        int y0 = Mathf.Clamp(Mathf.RoundToInt(v0 * altura), 0, altura);
        int y1 = Mathf.Clamp(Mathf.RoundToInt(v1 * altura), 0, altura);

        Color32 c = cor;

        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
                px[y * largura + x] = c;
        }
    }

    // Um vulto de pé: tronco, pernas e cabeça. Nada de rosto — a 1986 do
    // jogo não identifica ninguém pela cara, identifica pela fotografia.
    private static void Pessoa(
        Color32[] px, int largura, int altura,
        float centroU, float peV, float alturaV,
        Color cor, float suavidade,
        float recorteU0 = 0f, float recorteV0 = 0f,
        float recorteU1 = 1f, float recorteV1 = 1f)
    {
        float meiaLargura = alturaV * 0.13f * altura / largura;

        for (int y = 0; y < altura; y++)
        {
            float v = y / (float)altura;

            if (v < peV || v > peV + alturaV || v < recorteV0 || v >= recorteV1)
                continue;

            float h = (v - peV) / alturaV;

            for (int x = 0; x < largura; x++)
            {
                float u = x / (float)largura;

                if (u < recorteU0 || u >= recorteU1)
                    continue;

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

    // As duas fotografias da FASE 5 eram conteúdo fora do capítulo 1: o verso
    // «Ele ainda não chegou.» é o da fotografia da Câmara (Historia:420-431,
    // capítulo 5), «três pessoas à porta da fábrica» não é fotografia nenhuma
    // do capítulo, e «3 de Outubro de 1994» não está em documento nenhum.
    // Passam a ser as do capítulo 1. O ficheiro muda de nome por RenameAsset,
    // que MANTÉM o GUID — a cena aponta para o asset por GUID e não se parte.
    //
    // Só se muda o nome quando o novo ainda não existe: correr isto duas vezes
    // não pode tentar renomear por cima do que a primeira corrida já fez.
    private static void Migrar(string antigo, string novo)
    {
        string origem = $"{PastaDasFotografias}/{antigo}.asset";
        string destino = $"{PastaDasFotografias}/{novo}.asset";

        if (AssetDatabase.LoadAssetAtPath<PhotoAsset>(origem) == null ||
            AssetDatabase.LoadAssetAtPath<PhotoAsset>(destino) != null)
        {
            return;
        }

        string erro = AssetDatabase.RenameAsset(origem, novo);

        if (!string.IsNullOrEmpty(erro))
        {
            Falhar($"Não consegui renomear {antigo} para {novo}: {erro}");

            return;
        }

        Debug.Log($"FASE 8: {antigo} passou a {novo} (GUID mantido).");

        mudancas++;
    }

    // Cria-o se faltar e, falte ou não, volta a escrever-lhe os campos. É a
    // diferença para o GarantirFotografia da FASE 5, que devolvia o asset que
    // encontrasse tal como estava — e é por isso que lá as fotografias antigas
    // ficaram sem a descoberta quando a montagem mudou.
    private static SerializedObject Abrir(string nome, out PhotoAsset asset)
    {
        string caminho = $"{PastaDasFotografias}/{nome}.asset";

        asset = AssetDatabase.LoadAssetAtPath<PhotoAsset>(caminho);

        if (asset == null)
        {
            if (!AssetDatabase.IsValidFolder(PastaDasFotografias))
                AssetDatabase.CreateFolder("Assets/_Project", "Photography");

            asset = ScriptableObject.CreateInstance<PhotoAsset>();

            AssetDatabase.CreateAsset(asset, caminho);

            Debug.Log($"FASE 8: fotografia autorada criada em {caminho}.");

            mudancas++;
        }

        return new SerializedObject(asset);
    }

    // Os campos de base. A data é só o ano em todas: a Historia não data
    // nenhuma destas fotografias, e um dia inventado é o género de placeholder
    // que sobrevive até ao jogo sair. Sem verso em todas: não há verso
    // canónico para nenhuma das três.
    private static void EscreverBase(
        SerializedObject so, string nome, int ano, string local,
        string frente, Texture2D imagem, params string[] personagens)
    {
        Definir(so, "fotografia.id", nome);
        Definir(so, "fotografia.ano", ano);
        Definir(so, "fotografia.data", ano.ToString());
        Definir(so, "fotografia.local", local);
        DefinirLista(so, "fotografia.personagens", personagens);
        Definir(so, "fotografia.frente", frente);
        Definir(so, "fotografia.verso", string.Empty);
        Ligar(so, "fotografia.imagem", imagem);
        Definir(so, "fotografia.autorada", true);
    }

    private static void Tamanho(SerializedObject so, string campo, int n)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || prop.arraySize == n)
            return;

        prop.arraySize = n;
        mudancas++;
    }

    private static PhotoAsset GarantirFotografia(Texture2D antes, Texture2D depois)
    {
        SerializedObject so = Abrir(NomeDaFotografia, out PhotoAsset asset);

        EscreverBase(so, NomeDaFotografia, 1986, "Lago", "Thomas junto ao lago.", antes, "Thomas Hale");

        // O par que leva ao primeiro flashback: o mesmo homem, no mesmo sítio,
        // oito anos depois (Historia:217-231). Em (0,0), rotação 0 e escala 1
        // porque as duas imagens são o mesmo enquadramento ao píxel.
        Tamanho(so, "fotografia.alinhamentos", 1);

        string a = "fotografia.alinhamentos.Array.data[0].";

        Definir(so, a + "comId", NomeDoLago1994);
        DefinirVector2(so, a + "posicaoAlvo", Vector2.zero);
        DefinirFloat(so, a + "rotacaoAlvo", 0f);
        DefinirFloat(so, a + "escalaAlvo", 1f);
        DefinirFloat(so, a + "toleranciaPosicao", 0.06f);
        DefinirFloat(so, a + "toleranciaRotacao", 6f);
        DefinirFloat(so, a + "toleranciaEscala", 0.1f);

        // O que o par revela, à letra de Historia:221. É também o elo «As
        // fotografias ligam diferentes épocas» da cadeia da FASE 6.
        Tamanho(so, "fotografia.descobertas", 1);

        string d = "fotografia.descobertas.Array.data[0].";

        Definir(so, d + "id", DescobertaDoLago);
        DefinirEnum(so, d + "tipo", (int)TipoDeDescoberta.Pessoa);
        Definir(so, d + "descricao", "Thomas não mudou.");
        DefinirVector2(so, d + "posicaoNaImagem", new Vector2(0.60f, 0.27f));
        Definir(so, d + "revelaAoAlinharCom", NomeDoLago1994);
        Definir(so, d + "revelada", false);

        Tamanho(so, "fotografia.variantes", 1);

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

    // «Outra, tirada décadas depois, mostra o mesmo homem» (Historia:219). É
    // 1994 e não 2011 porque o elo 3 da cadeia do GDD §17 é «Thomas aparece
    // em 1994» — decisão do Diogo, 23/09.
    private static PhotoAsset GarantirLago1994(Texture2D imagem)
    {
        SerializedObject so = Abrir(NomeDoLago1994, out PhotoAsset asset);

        EscreverBase(so, NomeDoLago1994, 1994, "Lago", "Thomas junto ao lago, anos depois.", imagem, "Thomas Hale");

        // O par é declarado de um lado só — o do lago de 1986. Declarado dos
        // dois, os dois podiam divergir e o alinhamento dependia de qual
        // estava por cima.
        Tamanho(so, "fotografia.alinhamentos", 0);
        Tamanho(so, "fotografia.descobertas", 0);
        Tamanho(so, "fotografia.variantes", 0);

        so.ApplyModifiedPropertiesWithoutUndo();

        return asset;
    }

    // «Entre as fotografias existe uma imagem de Pine Hollow tirada em 1986.
    // Numa janela aparece um homem. É Thomas Hale. O jogador ainda não sabe
    // quem ele é.» (Historia:205-211). Por isso não tem personagens: se
    // tivesse, o caderno escrevia «Thomas Hale» no instante em que o jogador a
    // apanha, e o capítulo diz que ele ainda não sabe quem é.
    private static PhotoAsset GarantirCidade1986(Texture2D imagem)
    {
        SerializedObject so = Abrir(NomeDaCidade1986, out PhotoAsset asset);

        EscreverBase(so, NomeDaCidade1986, 1986, "Pine Hollow", "Pine Hollow. Numa janela, um homem.", imagem);

        Tamanho(so, "fotografia.alinhamentos", 0);
        Tamanho(so, "fotografia.descobertas", 0);
        Tamanho(so, "fotografia.variantes", 0);

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
            GameObject molde = GameObject.Find(NomeDoLago1994);

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

    // O objecto da cena com o nome antigo passa a ter o novo, e o stateId com
    // ele — o stateId É o nome nestas fotografias (Fase5Montagem.Apanhavel), e
    // um que ficasse «Estudio» era um id morto no save da FASE 21.
    private static void MigrarNaCena(string antigo, string novo, PhotoAsset asset)
    {
        foreach (PhotoInteractable candidato in Object.FindObjectsByType<PhotoInteractable>(
                     FindObjectsInactive.Include))
        {
            if (candidato == null)
                continue;

            string nome = candidato.gameObject.name;

            if (nome != antigo && nome != novo)
                continue;

            if (nome != novo)
            {
                candidato.gameObject.name = novo;

                Debug.Log($"FASE 8: {antigo} passou a {novo} na cena.");

                mudancas++;
            }

            SerializedObject so = new SerializedObject(candidato);

            Ligar(so, "fotografia", asset);
            Definir(so, "stateId", novo);

            so.ApplyModifiedPropertiesWithoutUndo();

            return;
        }

        Falhar($"Não há na cena a fotografia {novo} (nem {antigo}) — corre primeiro a montagem da FASE 5.");
    }

    // A nota do Ethan e o obituário.
    //
    // A nota que estava na cena usava DOIS versos canónicos de outras
    // fotografias — «Ele ainda não chegou.» e «Não reveles todas.» — como texto
    // de uma nota. Passa a ser a pista do Ethan do slice (decisão 6 do plano):
    // aponta para o lago sem dizer «alinha estas duas», que o GDD proíbe.
    //
    // O obituário é o «Encontra documentos que confirmam que Thomas Hale
    // morreu em 1986» (Historia:225), e o elo 2 da cadeia da FASE 6, que
    // esperava por ele desde que foi escrita. Só o ano: o dia da morte não
    // está escrito em documento nenhum.
    private static void EscreverAsNotas()
    {
        DocumentInteractable nota = null;
        DocumentInteractable obituario = null;

        foreach (DocumentInteractable candidato in Object.FindObjectsByType<DocumentInteractable>(
                     FindObjectsInactive.Include))
        {
            string id = new SerializedObject(candidato).FindProperty("stateId").stringValue;

            if (id == StateIdDaNotaDoEthan)
                nota = candidato;
            else if (id == StateIdDoObituario)
                obituario = candidato;
        }

        if (nota == null)
        {
            Falhar("Não há na cena a nota do Ethan (stateId " + StateIdDaNotaDoEthan + ").");

            return;
        }

        SerializedObject soNota = new SerializedObject(nota);

        Definir(soNota, "title", "Nota de Ethan Cole");
        Definir(soNota, "body",
            MarcaDeProvisorio + "\n\n" +
            "O lago. Outra vez o lago.\n\n" +
            "Tirei-a do mesmo sítio, à mesma hora. Ele estava no mesmo sítio.");

        soNota.ApplyModifiedPropertiesWithoutUndo();

        if (obituario == null)
        {
            // Decalcado da nota: o mesmo volume, colisor, layer e ligação ao
            // ReadingSystem, que já foram acertados com capturas na FASE 3.
            GameObject go = Object.Instantiate(nota.gameObject, nota.transform.parent);

            go.name = "Obituario_Thomas_Hale";
            go.transform.position = nota.transform.position + new Vector3(-0.45f, 0f, 0f);
            go.transform.rotation = nota.transform.rotation;

            obituario = go.GetComponent<DocumentInteractable>();

            Debug.Log($"FASE 8: obituário do Thomas pousado na cena em {go.transform.position}.");

            mudancas++;
        }

        SerializedObject soObituario = new SerializedObject(obituario);

        Definir(soObituario, "stateId", StateIdDoObituario);
        Definir(soObituario, "title", "Obituário — Thomas Hale");
        Definir(soObituario, "body",
            MarcaDeProvisorio + "\n\n" +
            "Thomas Hale, técnico de manutenção da fábrica.\n" +
            "Faleceu em 1986.");

        soObituario.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------
    // Utilitários
    // ---------------------------------------------------------------

    private static void DefinirFloat(SerializedObject so, string campo, float valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || Mathf.Approximately(prop.floatValue, valor))
            return;

        prop.floatValue = valor;

        mudancas++;
    }

    private static void DefinirVector2(SerializedObject so, string campo, Vector2 valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || prop.vector2Value == valor)
            return;

        prop.vector2Value = valor;

        mudancas++;
    }

    private static void DefinirEnum(SerializedObject so, string campo, int valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || prop.enumValueIndex == valor)
            return;

        prop.enumValueIndex = valor;

        mudancas++;
    }

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
