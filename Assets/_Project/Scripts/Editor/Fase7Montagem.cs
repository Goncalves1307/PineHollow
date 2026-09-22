using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Monta na cena o que a FASE 7 acrescentou: o world state no Bootstrap, o
// condutor do flashback, o véu da transição, a luz das duas épocas e o Thomas
// junto ao lago.
//
// Decalcado do Fase6Montagem e NÃO do Fase5Montagem, de propósito: o da FASE 5
// abre a cena em Single sem perguntar nada, e corrido pelo menu com trabalho
// por gravar deita-o fora numa fracção de segundo. Este pergunta primeiro.
//
// Nenhum Garantir* daqui sai cedo por a coisa já existir: sair cedo é a
// armadilha desta casa — foi assim que o GarantirFotografia da FASE 5 deixou
// assets desactualizados e que a FASE 6 repetiu o erro duas vezes no ficheiro
// que o critica em comentário. Correr isto duas vezes não duplica nada e põe
// tudo em ordem das duas.
//
// Corre pelo menu ou em batchmode:
//   Unity -batchmode -nographics -quit -projectPath . \
//         -executeMethod Fase7Montagem.Correr -logFile -
public static class Fase7Montagem
{
    private const string CaminhoDoBootstrap =
        "Assets/_Project/Scenes/Bootstrap.unity";

    private const string CaminhoDaCena =
        "Assets/_Project/Scenes/Prototype_Player.unity";

    private const string NomeDoAnfitriao = "Flashback";
    private const string NomeDoVeu = "VeuDaTransicao";
    private const string NomeDoThomas = "Thomas_1986";
    private const string NomeDoPercurso = "Percurso";

    // Quantos pontos tem o caminho do Thomas junto ao lago. Três chegam para
    // ele andar, virar e sair de vista — o GDD pede flashbacks «relativamente
    // curtos e significativos», e a duração é o caminho, não um cronómetro.
    private const int PontosDoPercurso = 3;

    private static int mudancas;

    [MenuItem("Pine Hollow/FASE 7 — montar o flashback")]
    public static void Correr()
    {
        mudancas = 0;

        // Isto abre cenas em Single, o que descarrega a que estiver aberta sem
        // perguntar. Em batchmode não há ninguém a quem perguntar e o diálogo
        // não aparece.
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("FASE 7: montagem cancelada — havia cenas por gravar.");

            return;
        }

        MontarOEstadoDoMundo();

        Scene cena = EditorSceneManager.OpenScene(CaminhoDaCena, OpenSceneMode.Single);

        PhotoComparisonSystem comparacao =
            Object.FindAnyObjectByType<PhotoComparisonSystem>(FindObjectsInactive.Include);

        if (comparacao == null)
        {
            Falhar("Não há PhotoComparisonSystem na cena: sem ele não há " +
                   "alinhamento, e sem alinhamento não há flashback.");

            return;
        }

        PlayerStateMachine maquina =
            Object.FindAnyObjectByType<PlayerStateMachine>(FindObjectsInactive.Include);

        if (maquina == null)
        {
            Falhar("Não há PlayerStateMachine na cena: sem ela não há quem " +
                   "empurre o modo Flashback.");

            return;
        }

        PhotoAlbumSystem album =
            Object.FindAnyObjectByType<PhotoAlbumSystem>(FindObjectsInactive.Include);

        Transform canvas = CanvasDe(comparacao);

        if (canvas == null)
            return;

        GameObject anfitriao = GarantirAnfitriao();

        Image veu = GarantirVeu(canvas);

        PersonagemHistorica thomas = GarantirThomas(maquina.transform);

        LigarTudo(anfitriao, veu, thomas, comparacao, album, maquina);

        if (mudancas > 0)
        {
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            Debug.Log($"FASE 7: flashback montado, {mudancas} alterações gravadas.");
        }
        else
        {
            Debug.Log("FASE 7: já estava montado, nada a fazer.");
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    // ---------------------------------------------------------------
    // O world state, na cena que sobrevive
    // ---------------------------------------------------------------

    // Vai para o Bootstrap pela mesma razão que o registo de conhecimento: é o
    // objecto que atravessa as trocas de cena. Um world state na cena de jogo
    // esquecia, a cada recarregamento, todas as portas que o jogador abriu.
    private static void MontarOEstadoDoMundo()
    {
        Scene bootstrap = EditorSceneManager.OpenScene(
            CaminhoDoBootstrap,
            OpenSceneMode.Single
        );

        BootstrapLoader carregador = Object.FindAnyObjectByType<BootstrapLoader>(
            FindObjectsInactive.Include
        );

        if (carregador == null)
        {
            Debug.LogWarning(
                "FASE 7: não há BootstrapLoader na cena do Bootstrap — o world " +
                "state não foi montado lá. Em jogo ele cria-se sozinho, mas " +
                "não sobrevive à troca de cena."
            );

            return;
        }

        // Este é o único sítio deste ficheiro que sai cedo, e é porque não há
        // nada para reaplicar: o EstadoDoMundo não tem um campo autorado que a
        // montagem escreva. A regra de não sair cedo existe para não deixar
        // desactualizado o que o script configura — aqui o script não configura
        // coisa nenhuma, só garante que o componente existe. Acrescentá-lo
        // outra vez seria duplicá-lo, e dois estados é o mundo partido ao meio.
        if (carregador.GetComponent<EstadoDoMundo>() != null)
            return;

        Undo.AddComponent<EstadoDoMundo>(carregador.gameObject);

        EditorSceneManager.MarkSceneDirty(bootstrap);
        EditorSceneManager.SaveScene(bootstrap);

        Debug.Log(
            "FASE 7: EstadoDoMundo acrescentado ao Bootstrap — as duas épocas " +
            "e o que nelas muda passam a atravessar as trocas de cena."
        );

        mudancas++;
    }

    // ---------------------------------------------------------------
    // O condutor
    // ---------------------------------------------------------------

    // Objecto de raiz e não filho do Canvas: o SistemaDeFlashback tem de correr
    // o Update com todos os painéis desligados — se vivesse dentro de um painel
    // de UI, o alinhamento fechava o painel e o condutor calava-se a meio da
    // transição. É a mesma armadilha que deixou o álbum inalcançável durante
    // semanas na FASE 5.
    private static GameObject GarantirAnfitriao()
    {
        // Procura pelo COMPONENTE e com os inactivos incluídos, não por
        // GameObject.Find: esse só vê os activos, e bastava alguém desligar o
        // objecto na hierarquia para a corrida seguinte criar um segundo
        // anfitrião com um segundo SistemaDeFlashback — a promessa de «correr
        // duas vezes não duplica nada» caía em silêncio.
        SistemaDeFlashback ja = Object.FindAnyObjectByType<SistemaDeFlashback>(
            FindObjectsInactive.Include
        );

        GameObject existente = ja != null ? ja.gameObject : null;

        if (existente == null)
        {
            existente = new GameObject(NomeDoAnfitriao);

            mudancas++;
        }

        // Os componentes garantem-se um a um e não no construtor: um anfitrião
        // de uma corrida anterior pode ter uns e não outros, e recriar o
        // objecto perdia o que estivesse ligado à mão.
        Garantir<SistemaDeFlashback>(existente);
        Garantir<TransicaoDeEpoca>(existente);
        Garantir<AmbienteDeEpoca>(existente);
        Garantir<AmbienteSonoroDeEpoca>(existente);
        Garantir<FimAoSairDeVista>(existente);

        return existente;
    }

    private static T Garantir<T>(GameObject anfitriao) where T : Component
    {
        T componente = anfitriao.GetComponent<T>();

        if (componente != null)
            return componente;

        mudancas++;

        return Undo.AddComponent<T>(anfitriao);
    }

    // ---------------------------------------------------------------
    // O véu
    // ---------------------------------------------------------------

    // Um Image preto a ecrã inteiro, por cima de tudo, transparente e desligado
    // em repouso.
    //
    // Preto e não pós-processamento: a FASE 1 tirou a aberração cromática, a
    // distorção de lente, o panini e o motion blur porque DESLOCAM A IMAGEM, e
    // este jogo pede ao jogador que alinhe uma fotografia com uma divisão. Um
    // rectângulo opaco por cima não move um píxel de sítio.
    private static Image GarantirVeu(Transform canvas)
    {
        Transform existente = canvas.Find(NomeDoVeu);

        GameObject go;

        if (existente != null)
        {
            go = existente.gameObject;
        }
        else
        {
            go = new GameObject(NomeDoVeu, typeof(RectTransform));
            go.transform.SetParent(canvas, false);

            mudancas++;
        }

        Image veu = go.GetComponent<Image>();

        if (veu == null)
        {
            veu = go.AddComponent<Image>();

            mudancas++;
        }

        // Reaplica-se sempre. Um véu de uma corrida anterior com a cor trocada
        // à mão, ou a apanhar cliques, continuaria assim para sempre se isto
        // saísse cedo por «já existe».
        Reaplicar(veu, (RectTransform)go.transform);

        // Último filho do Canvas: a ordem de desenho do UI é a ordem da
        // hierarquia, e um véu a meio da lista era tapado pelo álbum.
        if (go.transform.GetSiblingIndex() != canvas.childCount - 1)
        {
            go.transform.SetAsLastSibling();

            mudancas++;
        }

        if (go.activeSelf)
        {
            // Desligado em repouso: um Image a ecrã inteiro, ainda que
            // transparente, apanha os raycasts do UI e tapa os cliques do álbum.
            go.SetActive(false);

            mudancas++;
        }

        return veu;
    }

    private static void Reaplicar(Image veu, RectTransform rect)
    {
        Color preto = new Color(0f, 0f, 0f, 0f);

        if (veu.color != preto)
        {
            veu.color = preto;

            mudancas++;
        }

        if (veu.raycastTarget)
        {
            veu.raycastTarget = false;

            mudancas++;
        }

        // Âncoras em fracções e nunca em pixéis: a captura do Fase5Capturas
        // corre maximizada, e um véu dimensionado em pixéis passava nela e
        // deixava uma faixa por tapar na janela estreita do editor.
        if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one ||
            rect.offsetMin != Vector2.zero || rect.offsetMax != Vector2.zero)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            mudancas++;
        }
    }

    // ---------------------------------------------------------------
    // O Thomas junto ao lago
    // ---------------------------------------------------------------

    // GDD §15.1 «The Photograph»: o Thomas é visto junto ao lago em 1986 e o
    // jogador percebe que está no passado. Não fala e não espera — anda o seu
    // caminho e sai de vista, e é isso que acaba o flashback.
    //
    // A cápsula é placeholder assumido. O que esta fase entrega é o mecanismo;
    // o Thomas com cara é conteúdo, e o modelo entra quando houver.
    private static PersonagemHistorica GarantirThomas(Transform jogador)
    {
        // Também pelo componente e com inactivos: a cápsula é um placeholder a
        // estorvar quem veste o cenário, e é plausível que a desliguem. Com o
        // GameObject.Find, a corrida seguinte criava um segundo Thomas e o fim
        // do flashback passava a depender do novo.
        PersonagemHistorica ja = Object.FindAnyObjectByType<PersonagemHistorica>(
            FindObjectsInactive.Include
        );

        GameObject go = ja != null ? ja.gameObject : null;

        if (go == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = NomeDoThomas;

            // Sem colisor: o jogador segue-o, não esbarra nele. Uma cápsula
            // sólida no meio do caminho travava o raycast de interacção e o
            // jogador ficava a apontar-lhe sem nada acontecer.
            Object.DestroyImmediate(go.GetComponent<Collider>());

            go.transform.position = jogador.position + jogador.forward * 8f;

            mudancas++;
        }

        PersonagemHistorica personagem = Garantir<PersonagemHistorica>(go);

        // O RequireComponent já o trouxe; o que interessa é marcá-lo como
        // sendo só de 1986. Sem isto o Thomas ficava a passear em 2026.
        ObjectoDeEpoca epoca = Garantir<ObjectoDeEpoca>(go);

        SerializedObject soEpoca = new SerializedObject(epoca);

        Definir(soEpoca, "existeEm2026", false);
        Definir(soEpoca, "existeEm1986", true);

        soEpoca.ApplyModifiedPropertiesWithoutUndo();

        Transform[] percurso = GarantirPercurso(go.transform, jogador);

        SerializedObject so = new SerializedObject(personagem);

        SerializedProperty lista = so.FindProperty("percurso");

        if (lista != null && lista.arraySize != percurso.Length)
        {
            lista.arraySize = percurso.Length;

            mudancas++;
        }

        for (int i = 0; lista != null && i < percurso.Length; i++)
        {
            SerializedProperty elemento = lista.GetArrayElementAtIndex(i);

            if (elemento.objectReferenceValue != percurso[i])
            {
                elemento.objectReferenceValue = percurso[i];

                mudancas++;
            }
        }

        // Inconsciente é o degrau desta fase, e está escrito à mão e não
        // deixado por omissão: o GDD diz que os primeiros flashbacks são
        // observacionais e que a mudança disso é o jogo. Quem subir o degrau
        // um dia há-de vir a esta linha de propósito.
        Definir(so, "consciencia", (int)NivelDeConsciencia.Inconsciente);

        Ligar(so, "jogador", jogador);

        so.ApplyModifiedPropertiesWithoutUndo();

        return personagem;
    }

    // O percurso vive na RAIZ da cena, nunca dentro do Thomas.
    //
    // Pendurado nele, os pontos andavam com ele: a distância ficava congelada,
    // ele nunca chegava a nenhum, e o flashback não acabava — o jogador ficava
    // em 1986 para sempre, porque do modo Flashback não se sai pelo Esc. Foi o
    // que esta montagem gerou à primeira, e os testes não o apanharam porque
    // constroem o percurso na raiz. Apanhado em revisão.
    private static Transform[] GarantirPercurso(Transform thomas, Transform jogador)
    {
        GameObject existentePai = AcharRaizPorNome(NomeDoPercurso);

        Transform pai = existentePai != null ? existentePai.transform : null;

        // Herdado da montagem antiga: se o percurso estiver pendurado no
        // Thomas, tira-se de lá em vez de se criar um segundo. Uma cena já
        // montada tem de sair desta corrida corrigida, não duplicada.
        Transform pendurado = thomas.Find(NomeDoPercurso);

        if (pendurado != null)
        {
            pendurado.SetParent(null, true);

            pai = pendurado;

            mudancas++;

            Debug.LogWarning(
                "FASE 7: o percurso estava pendurado no Thomas e nunca seria " +
                "alcançado — passou para a raiz da cena."
            );
        }

        if (pai == null)
        {
            GameObject go = new GameObject(NomeDoPercurso);

            go.transform.position = Vector3.zero;
            go.transform.localScale = Vector3.one;

            pai = go.transform;

            mudancas++;
        }

        Transform[] pontos = new Transform[PontosDoPercurso];

        for (int i = 0; i < PontosDoPercurso; i++)
        {
            string nome = "P" + i;

            Transform ponto = pai.Find(nome);

            if (ponto == null)
            {
                GameObject go = new GameObject(nome);
                go.transform.SetParent(pai, false);

                // Afasta-se do jogador ao longo do caminho: o Thomas anda para
                // longe e acaba por sair de vista, que é o fim do flashback.
                go.transform.position =
                    thomas.position +
                    jogador.forward * (6f * (i + 1)) +
                    jogador.right * (i % 2 == 0 ? 2f : -2f);

                ponto = go.transform;

                mudancas++;
            }

            pontos[i] = ponto;
        }

        return pontos;
    }

    // ---------------------------------------------------------------
    // As ligações
    // ---------------------------------------------------------------

    private static void LigarTudo(
        GameObject anfitriao,
        Image veu,
        PersonagemHistorica thomas,
        PhotoComparisonSystem comparacao,
        PhotoAlbumSystem album,
        PlayerStateMachine maquina)
    {
        TransicaoDeEpoca transicao = anfitriao.GetComponent<TransicaoDeEpoca>();
        AmbienteDeEpoca ambiente = anfitriao.GetComponent<AmbienteDeEpoca>();
        AmbienteSonoroDeEpoca som = anfitriao.GetComponent<AmbienteSonoroDeEpoca>();
        FimAoSairDeVista fim = anfitriao.GetComponent<FimAoSairDeVista>();
        SistemaDeFlashback sistema = anfitriao.GetComponent<SistemaDeFlashback>();

        SerializedObject soTransicao = new SerializedObject(transicao);
        Ligar(soTransicao, "veu", veu);
        soTransicao.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soAmbiente = new SerializedObject(ambiente);
        Ligar(soAmbiente, "luzDoCeu", LuzDoCeu());
        soAmbiente.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soSom = new SerializedObject(som);
        Ligar(soSom, "somDe2026", GarantirFonte(anfitriao, "Ambiencia_2026"));
        Ligar(soSom, "somDe1986", GarantirFonte(anfitriao, "Ambiencia_1986"));
        soSom.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soFim = new SerializedObject(fim);
        Ligar(soFim, "personagem", thomas);
        soFim.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject so = new SerializedObject(sistema);

        Ligar(so, "comparacao", comparacao);
        Ligar(so, "album", album);
        Ligar(so, "stateMachine", maquina);
        Ligar(so, "transicao", transicao);
        Ligar(so, "ambiente", ambiente);
        Ligar(so, "som", som);
        Ligar(so, "fim", fim);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Uma das duas ambiências, em 2D e sem clip.
    //
    // O clip fica por pôr de propósito e não por esquecimento: o projecto tem
    // três clips de madeira e não tem betão nem metal, e a fábrica de 1986 é
    // 57 menções no GDD. O que esta fase entrega é o mecanismo do cruzamento —
    // as fontes ficam montadas e ligadas, e no dia em que houver som basta
    // arrastar o clip para aqui. Montá-las agora é o que impede que esse dia
    // implique voltar a mexer em código.
    private static AudioSource GarantirFonte(GameObject anfitriao, string nome)
    {
        Transform existente = anfitriao.transform.Find(nome);

        GameObject go;

        if (existente != null)
        {
            go = existente.gameObject;
        }
        else
        {
            go = new GameObject(nome);
            go.transform.SetParent(anfitriao.transform, false);

            mudancas++;
        }

        AudioSource fonte = go.GetComponent<AudioSource>();

        if (fonte == null)
        {
            fonte = Undo.AddComponent<AudioSource>(go);

            mudancas++;
        }

        // Reaplica-se sempre, como tudo neste ficheiro.
        if (fonte.playOnAwake || !fonte.loop ||
            fonte.volume != 0f || fonte.spatialBlend != 0f)
        {
            // Volume a zero: quem manda nele é o AmbienteSonoroDeEpoca, e uma
            // fonte a arrancar a 1 dava um estrondo de 1986 no primeiro frame
            // de 2026.
            fonte.playOnAwake = false;
            fonte.loop = true;
            fonte.volume = 0f;

            // 2D: uma ambiência não tem sítio. Em 3D ela vinha de um ponto da
            // cena e o jogador andava à procura da fábrica dentro de casa.
            fonte.spatialBlend = 0f;

            mudancas++;
        }

        return fonte;
    }

    // A luz que faz a diferença entre as épocas: 1986 quente porque há
    // tungsténio aceso, 2026 frio porque só entra o céu (§7).
    private static Light LuzDoCeu()
    {
        Light[] luzes = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);

        foreach (Light luz in luzes)
        {
            if (luz != null && luz.type == LightType.Directional)
                return luz;
        }

        Debug.LogWarning(
            "FASE 7: não há luz direccional na cena — as duas épocas vão ficar " +
            "com a mesma luz, e a temperatura É a regra que as distingue."
        );

        return null;
    }

    // ---------------------------------------------------------------

    // Um objecto de raiz pelo nome, incluindo os desligados. O GameObject.Find
    // só vê activos, e o percurso não tem componente próprio por onde ser
    // encontrado — sem isto, desligá-lo na hierarquia fazia a corrida seguinte
    // criar um segundo caminho.
    private static GameObject AcharRaizPorNome(string nome)
    {
        Scene cena = SceneManager.GetActiveScene();

        if (!cena.IsValid())
            return null;

        foreach (GameObject raiz in cena.GetRootGameObjects())
        {
            if (raiz != null && raiz.name == nome)
                return raiz;
        }

        return null;
    }

    private static Transform CanvasDe(PhotoComparisonSystem comparacao)
    {
        SerializedObject so = new SerializedObject(comparacao);

        SerializedProperty prop = so.FindProperty("painel");

        GameObject painel = prop != null
            ? prop.objectReferenceValue as GameObject
            : null;

        if (painel == null || painel.transform.parent == null)
        {
            Falhar("O painel da comparação não tem pai — não há Canvas onde " +
                   "pendurar o véu da transição.");

            return null;
        }

        return painel.transform.parent;
    }

    private static void Ligar(SerializedObject so, string campo, Object valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || prop.objectReferenceValue == valor)
            return;

        prop.objectReferenceValue = valor;

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

    private static void Definir(SerializedObject so, string campo, int valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null || prop.enumValueIndex == valor)
            return;

        prop.enumValueIndex = valor;

        mudancas++;
    }

    private static void Falhar(string mensagem)
    {
        Debug.LogError("FASE 7: " + mensagem);

        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}
