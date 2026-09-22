using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Testes do ciclo da FASE 7: 2026 → Alinhamento → Transição → 1986 → Gameplay
// → Evento → 2026.
//
// Tudo isto é verificável em EditMode porque o SistemaDeFlashback recebe o
// tempo em parâmetro (Avancar(delta)) em vez de ler o Time.deltaTime, que é
// zero fora de play mode. Uma transição que só avançasse em play mode não
// tinha teste nenhum.
public class FlashbackTests
{
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

        EstadoDoMundo.Esquecer();
        RegistoDeConhecimento.Esquecer();
    }

    // ------------------------------------------------------------ montagem

    private PersonagemHistorica Thomas(params Vector3[] pontos)
    {
        GameObject go = Novo("Thomas");

        PersonagemHistorica personagem = go.AddComponent<PersonagemHistorica>();

        List<Transform> percurso = new List<Transform>();

        foreach (Vector3 ponto in pontos)
        {
            GameObject marca = Novo("Ponto");
            marca.transform.position = ponto;
            percurso.Add(marca.transform);
        }

        Campo(personagem, "percurso").SetValue(personagem, percurso.ToArray());
        Campo(personagem, "velocidade").SetValue(personagem, 10f);

        return personagem;
    }

    private SistemaDeFlashback Sistema(
        out PlayerStateMachine maquina,
        out TransicaoDeEpoca transicao,
        out FimAoSairDeVista fim,
        PersonagemHistorica personagem)
    {
        maquina = Novo("Player").AddComponent<PlayerStateMachine>();
        transicao = Novo("Transicao").AddComponent<TransicaoDeEpoca>();

        fim = Novo("Fim").AddComponent<FimAoSairDeVista>();
        Campo(fim, "personagem").SetValue(fim, personagem);

        SistemaDeFlashback sistema =
            Novo("SistemaDeFlashback").AddComponent<SistemaDeFlashback>();

        Campo(sistema, "stateMachine").SetValue(sistema, maquina);
        Campo(sistema, "transicao").SetValue(sistema, transicao);
        Campo(sistema, "fim").SetValue(sistema, fim);

        return sistema;
    }

    // -------------------------------------------------------------- o ciclo

    [Test]
    public void Ciclo_VaiA1986EVolta()
    {
        PersonagemHistorica thomas = Thomas(new Vector3(0f, 0f, 1f));

        SistemaDeFlashback sistema = Sistema(
            out PlayerStateMachine maquina,
            out TransicaoDeEpoca _,
            out FimAoSairDeVista _,
            thomas);

        Assert.AreEqual(FaseDoFlashback.Fora, sistema.Fase);

        sistema.Iniciar();

        Assert.AreEqual(
            FaseDoFlashback.Clarao, sistema.Fase,
            "começa pelo clarão: é a única confirmação de que o jogador alinhou");

        // O clarão respira.
        sistema.Avancar(0.5f);
        Assert.AreEqual(FaseDoFlashback.ACobrir, sistema.Fase);

        // O ecrã tapa e o mundo troca por trás.
        sistema.Avancar(2f);

        Assert.AreEqual(FaseDoFlashback.ADescobrir, sistema.Fase);
        Assert.AreEqual(
            Epoca.Passado1986, EstadoDoMundo.Instancia.EpocaActual,
            "a troca de época tinha de acontecer com o ecrã já tapado");
        Assert.IsTrue(
            maquina.IsModeOpen(PlayerState.Flashback),
            "o modo Flashback não foi empurrado");

        // O véu levanta.
        sistema.Avancar(3f);
        Assert.AreEqual(FaseDoFlashback.Dentro, sistema.Fase);

        // Joga-se. O Thomas anda até ao fim do percurso e sai de vista.
        thomas.Avancar(1f);
        Assert.IsTrue(thomas.SaiuDeVista, "o Thomas não chegou ao fim do caminho");

        sistema.Avancar(0.1f);
        Assert.AreEqual(FaseDoFlashback.ASair, sistema.Fase);

        sistema.Avancar(2f);
        Assert.AreEqual(FaseDoFlashback.AVoltar, sistema.Fase);
        Assert.AreEqual(
            Epoca.Presente2026, EstadoDoMundo.Instancia.EpocaActual,
            "o regresso ao presente não aconteceu");
        Assert.IsFalse(
            maquina.IsModeOpen(PlayerState.Flashback),
            "ficou-se em modo Flashback depois de voltar a 2026");

        sistema.Avancar(3f);
        Assert.AreEqual(FaseDoFlashback.Fora, sistema.Fase);
    }

    [Test]
    public void Iniciar_NaoReentraEnquantoOCicloCorre()
    {
        PersonagemHistorica thomas = Thomas(new Vector3(0f, 0f, 1f));

        SistemaDeFlashback sistema = Sistema(
            out PlayerStateMachine _, out TransicaoDeEpoca _,
            out FimAoSairDeVista _, thomas);

        sistema.Iniciar();
        sistema.Avancar(0.5f);

        Assert.AreEqual(FaseDoFlashback.ACobrir, sistema.Fase);

        // Um segundo alinhamento a meio da transição não pode voltar a começar.
        sistema.Iniciar();

        Assert.AreEqual(
            FaseDoFlashback.ACobrir, sistema.Fase,
            "reentrou a meio do ciclo");
    }

    [Test]
    public void Update_ConsomeOAlinhamentoDaComparacao()
    {
        // O gancho que a FASE 5 deixou escrito à espera de consumidor.
        PhotoComparisonSystem comparacao =
            Novo("Comparacao").AddComponent<PhotoComparisonSystem>();

        Campo(comparacao, "alinhamentoPorConsumir").SetValue(comparacao, true);

        PersonagemHistorica thomas = Thomas(new Vector3(0f, 0f, 1f));

        SistemaDeFlashback sistema = Sistema(
            out PlayerStateMachine _, out TransicaoDeEpoca _,
            out FimAoSairDeVista _, thomas);

        Campo(sistema, "comparacao").SetValue(sistema, comparacao);

        Invocar(sistema, "Update");

        Assert.AreEqual(
            FaseDoFlashback.Clarao, sistema.Fase,
            "o ConsumirAlinhamento não foi consumido");
        Assert.IsFalse(
            comparacao.ConsumirAlinhamento(),
            "o alinhamento tinha de ter sido consumido uma só vez");
    }

    [Test]
    public void Fim_RearmaSeAoEntrar()
    {
        // Rever um flashback com a condição já satisfeita da vez anterior
        // acabava-o no primeiro frame.
        PersonagemHistorica thomas = Thomas(new Vector3(0f, 0f, 1f));

        thomas.Avancar(1f);
        Assert.IsTrue(thomas.SaiuDeVista);

        SistemaDeFlashback sistema = Sistema(
            out PlayerStateMachine _, out TransicaoDeEpoca _,
            out FimAoSairDeVista _, thomas);

        sistema.Iniciar();
        sistema.Avancar(0.5f);
        sistema.Avancar(2f);

        Assert.IsFalse(
            thomas.SaiuDeVista,
            "a condição de fim não foi rearmada à entrada");
    }

    // ------------------------------------------------- a marca no presente

    [Test]
    public void AlteracaoFeitaEm1986_EVisivelEm2026()
    {
        // É a promessa inteira da subtask 869f4yb88, e o caso real que motivou
        // a task. Não é o caminho feliz de nada — é o critério de aceitação.
        EstadoDoMundo estado = Novo("Estado").AddComponent<EstadoDoMundo>();
        estado.Assumir();

        estado.MudarPara(Epoca.Passado1986);

        estado.Registar("porta-fabrica", DoorInteractable.ChaveAberta, true);

        estado.MudarPara(Epoca.Presente2026);

        Assert.IsTrue(
            estado.LerBool("porta-fabrica", DoorInteractable.ChaveAberta),
            "o que se fez em 1986 não chegou a 2026");
    }

    [Test]
    public void PersonagemEmMarcaOPresente_EscreveNoMundo()
    {
        PersonagemHistorica thomas = Thomas(new Vector3(0f, 0f, 1f));

        Campo(thomas, "consciencia").SetValue(
            thomas, NivelDeConsciencia.MarcaOPresente);
        Campo(thomas, "stateIdDaMarca").SetValue(thomas, "porta-fabrica");

        thomas.Avancar(1f);

        Assert.IsTrue(thomas.Marcou);
        Assert.IsTrue(
            EstadoDoMundo.Instancia.LerBool("porta-fabrica", "alterado"),
            "o quarto degrau não deixou marca nenhuma");
    }

    // -------------------------------------------------------- a personagem

    // Um Avancar consome no máximo UM ponto do percurso — é uma chamada por
    // frame, e em jogo o Update dá-lhe sessenta por segundo. Aqui os frames
    // simulam-se, senão o teste media uma coisa que nunca acontece.
    private static void Andar(PersonagemHistorica personagem, int frames, float delta)
    {
        for (int i = 0; i < frames && !personagem.SaiuDeVista; i++)
            personagem.Avancar(delta);
    }

    [Test]
    public void Personagem_AndaOPercursoESaiDeVista()
    {
        PersonagemHistorica thomas = Thomas(
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, 2f));

        Assert.IsFalse(thomas.SaiuDeVista);

        thomas.Avancar(0.05f);

        Assert.IsFalse(thomas.SaiuDeVista, "saiu de vista cedo de mais");
        Assert.Greater(
            thomas.transform.position.z, 0f,
            "não se mexeu");

        Andar(thomas, 10, 1f);

        Assert.IsTrue(thomas.SaiuDeVista, "não chegou ao fim do caminho em dez frames");
    }

    [Test]
    public void Personagem_Inconsciente_NaoRepararNoJogador()
    {
        // O primeiro flashback é observacional: o GDD diz que as pessoas do
        // passado ainda não dão pelo jogador, e que a mudança disso é o jogo.
        PersonagemHistorica thomas = Thomas(new Vector3(0f, 0f, 50f));

        GameObject jogador = Novo("Jogador");
        jogador.transform.position = thomas.transform.position;

        Campo(thomas, "jogador").SetValue(thomas, jogador.transform);

        thomas.Avancar(0.01f);

        Assert.IsFalse(
            thomas.NotouOJogador,
            "o degrau Inconsciente reparou no jogador");
    }

    [Test]
    public void Personagem_QueVe_ParaQuandoOJogadorSeAproxima()
    {
        PersonagemHistorica thomas = Thomas(new Vector3(0f, 0f, 50f));

        Campo(thomas, "consciencia").SetValue(thomas, NivelDeConsciencia.Ve);

        GameObject jogador = Novo("Jogador");
        jogador.transform.position = thomas.transform.position + Vector3.right;

        Campo(thomas, "jogador").SetValue(thomas, jogador.transform);

        float antes = thomas.transform.position.z;

        thomas.Avancar(0.5f);

        Assert.IsTrue(thomas.NotouOJogador);
        Assert.AreEqual(
            antes, thomas.transform.position.z, 0.0001f,
            "devia ter parado por ter dado pelo jogador");
    }

    [Test]
    public void Personagem_Recomecar_VoltaAoPrincipio()
    {
        PersonagemHistorica thomas = Thomas(
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, 2f));

        Andar(thomas, 10, 1f);
        Assert.IsTrue(thomas.SaiuDeVista);

        thomas.Recomecar();

        Assert.IsFalse(thomas.SaiuDeVista);
        Assert.AreEqual(0, thomas.Destino);
    }

    // ------------------------------------------------- não trancar o jogador

    // Os três testes seguintes existem porque a revisão encontrou caminhos em
    // que o flashback nunca acabava. Do modo Flashback não se sai pelo Esc: um
    // ciclo que não termina é a sessão perdida, não um defeito cosmético.

    [Test]
    public void Personagem_ComPercursoPenduradoEmSiPropria_NaoTrancaOFlashback()
    {
        // O defeito real: a montagem criou o `Percurso` como FILHO do Thomas.
        // Os pontos andavam com ele, a distância ficava congelada, e ele dava
        // voltas para sempre. Nenhum teste o viu porque todos constroem o
        // percurso na raiz — este constrói-o como a montagem o construiu.
        GameObject go = Novo("ThomasComPercursoPendurado");

        PersonagemHistorica thomas = go.AddComponent<PersonagemHistorica>();

        GameObject ponto = Novo("PontoPendurado");
        ponto.transform.SetParent(go.transform, false);
        ponto.transform.localPosition = new Vector3(2f, 0f, 6f);

        Campo(thomas, "percurso").SetValue(thomas, new[] { ponto.transform });
        Campo(thomas, "velocidade").SetValue(thomas, 10f);

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
            "descendente da própria"));

        Andar(thomas, 10, 1f);

        Assert.IsTrue(
            thomas.SaiuDeVista,
            "um percurso pendurado na personagem trancava o jogador em 1986");
    }

    [Test]
    public void SemCondicaoDeFim_VoltaA2026EmVezDeTrancar()
    {
        SistemaDeFlashback sistema =
            Novo("SistemaSemFim").AddComponent<SistemaDeFlashback>();

        PlayerStateMachine maquina = Novo("Player").AddComponent<PlayerStateMachine>();
        TransicaoDeEpoca transicao = Novo("Transicao").AddComponent<TransicaoDeEpoca>();

        Campo(sistema, "stateMachine").SetValue(sistema, maquina);
        Campo(sistema, "transicao").SetValue(sistema, transicao);
        // `fim` fica propositadamente por ligar.

        sistema.Iniciar();
        sistema.Avancar(0.5f);
        sistema.Avancar(2f);

        Assert.AreEqual(FaseDoFlashback.ADescobrir, sistema.Fase);

        sistema.Avancar(3f);
        Assert.AreEqual(FaseDoFlashback.Dentro, sistema.Fase);

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
            "não tem FimDeFlashback"));

        sistema.Avancar(0.1f);

        Assert.AreEqual(
            FaseDoFlashback.ASair, sistema.Fase,
            "sem condição de fim o jogador ficava em 1986 sem log e sem saída");
    }

    [Test]
    public void EscDentroDoFlashback_EConsumidoENaoEsvaziaAPilha()
    {
        // Sem consumir, o ResolveUnconsumedBack acusava o dono de não estar a
        // correr o Update — falso — e tirava o Flashback da pilha à força. O
        // mundo ficava em 1986 com a pilha a dizer que não.
        PersonagemHistorica thomas = Thomas(new Vector3(0f, 0f, 1f));

        SistemaDeFlashback sistema = Sistema(
            out PlayerStateMachine maquina, out TransicaoDeEpoca _,
            out FimAoSairDeVista _, thomas);

        sistema.Iniciar();
        sistema.Avancar(0.5f);
        sistema.Avancar(2f);
        sistema.Avancar(3f);

        Assert.AreEqual(FaseDoFlashback.Dentro, sistema.Fase);
        Assert.IsTrue(maquina.IsModeOpen(PlayerState.Flashback));

        maquina.BeginFrame();
        maquina.RequestBack();

        sistema.Avancar(0.016f);

        maquina.ResolveUnconsumedBack();

        Assert.IsTrue(
            maquina.IsModeOpen(PlayerState.Flashback),
            "o Esc esvaziou a pilha e deixou o mundo em 1986 sem camada");
    }

    [Test]
    public void EscNasTransicoes_TambemEConsumido()
    {
        // A primeira correcção só consumia o Esc na fase Dentro, e o Flashback
        // está na pilha desde o fim de ACobrir até ao VoltarA2026 — ou seja
        // durante ADescobrir e ASair também, com o ecrã preto, que é
        // precisamente quando alguém carrega no Esc. Apanhado na segunda ronda.
        PersonagemHistorica thomas = Thomas(new Vector3(0f, 0f, 1f));

        SistemaDeFlashback sistema = Sistema(
            out PlayerStateMachine maquina, out TransicaoDeEpoca _,
            out FimAoSairDeVista _, thomas);

        sistema.Iniciar();
        sistema.Avancar(0.5f);
        sistema.Avancar(2f);

        // ADescobrir: o véu ainda está a levantar e o modo já está na pilha.
        Assert.AreEqual(FaseDoFlashback.ADescobrir, sistema.Fase);
        Assert.IsTrue(maquina.IsModeOpen(PlayerState.Flashback));

        maquina.BeginFrame();
        maquina.RequestBack();
        sistema.Avancar(0.016f);
        maquina.ResolveUnconsumedBack();

        Assert.IsTrue(
            maquina.IsModeOpen(PlayerState.Flashback),
            "o Esc durante ADescobrir esvaziou a pilha");

        // Agora ASair.
        sistema.Avancar(3f);
        Assert.AreEqual(FaseDoFlashback.Dentro, sistema.Fase);

        Andar(thomas, 10, 1f);
        sistema.Avancar(0.016f);

        Assert.AreEqual(FaseDoFlashback.ASair, sistema.Fase);

        maquina.BeginFrame();
        maquina.RequestBack();
        sistema.Avancar(0.016f);
        maquina.ResolveUnconsumedBack();

        Assert.IsTrue(
            maquina.IsModeOpen(PlayerState.Flashback),
            "o Esc durante ASair esvaziou a pilha e o PopMode do regresso " +
            "ficava sem efeito");
    }

    [Test]
    public void AlinhamentoMarcadoDuranteOClarao_NaoArrancaUmSegundoFlashback()
    {
        PhotoComparisonSystem comparacao =
            Novo("Comparacao").AddComponent<PhotoComparisonSystem>();

        PersonagemHistorica thomas = Thomas(new Vector3(0f, 0f, 1f));

        SistemaDeFlashback sistema = Sistema(
            out PlayerStateMachine _, out TransicaoDeEpoca _,
            out FimAoSairDeVista _, thomas);

        Campo(sistema, "comparacao").SetValue(sistema, comparacao);

        sistema.Iniciar();

        // O jogador ainda tem o botão em baixo e volta a entrar na tolerância:
        // o painel continua aberto durante o clarão e marca outro alinhamento.
        Campo(comparacao, "alinhamentoPorConsumir").SetValue(comparacao, true);

        sistema.Avancar(0.5f);
        sistema.Avancar(2f);

        Assert.IsFalse(
            comparacao.ConsumirAlinhamento(),
            "ficou um alinhamento por consumir: o ciclo arrancava outra vez " +
            "sozinho no primeiro frame depois de voltar a 2026");
    }

    // ---------------------------------------------------------- transições

    [Test]
    public void Transicao_CobreEDescobre()
    {
        TransicaoDeEpoca transicao =
            Novo("Transicao").AddComponent<TransicaoDeEpoca>();

        Assert.IsTrue(transicao.EstaLimpo);

        Assert.IsTrue(transicao.Avancar(10f, true), "devia ter coberto");
        Assert.IsTrue(transicao.EstaCoberto);
        Assert.AreEqual(1f, transicao.Cobertura, 0.0001f);

        Assert.IsTrue(transicao.Avancar(10f, false), "devia ter descoberto");
        Assert.IsTrue(transicao.EstaLimpo);
    }

    [Test]
    public void Transicao_NaoChegaAoFimNumPassoPequeno()
    {
        TransicaoDeEpoca transicao =
            Novo("Transicao").AddComponent<TransicaoDeEpoca>();

        Assert.IsFalse(
            transicao.Avancar(0.01f, true),
            "cobriu tudo num centésimo de segundo");
        Assert.Greater(transicao.Cobertura, 0f);
    }

    [Test]
    public void Som_CruzaEntreAsDuasEpocas()
    {
        AmbienteSonoroDeEpoca som =
            Novo("Som").AddComponent<AmbienteSonoroDeEpoca>();

        som.Definir(Epoca.Presente2026);

        Assert.AreEqual(0f, som.Mistura, 0.0001f);
        Assert.AreEqual(1f, som.VolumeDe2026, 0.0001f);
        Assert.AreEqual(0f, som.VolumeDe1986, 0.0001f);

        Assert.IsFalse(
            som.Avancar(0.1f, Epoca.Passado1986),
            "cruzou tudo num décimo de segundo");

        Assert.IsTrue(som.Avancar(10f, Epoca.Passado1986));
        Assert.AreEqual(1f, som.VolumeDe1986, 0.0001f);
        Assert.AreEqual(0f, som.VolumeDe2026, 0.0001f);
    }

    [Test]
    public void Ambiente_MudaATemperaturaDaLuzEntreEpocas()
    {
        // A direcção artística §7: 1986 quente porque há tungsténio aceso,
        // 2026 frio porque só entra o céu. É a regra das duas épocas na
        // prática, e por isso tem teste.
        AmbienteDeEpoca ambiente =
            Novo("Ambiente").AddComponent<AmbienteDeEpoca>();

        Color quente = ambiente.CorDe(Epoca.Passado1986);
        Color frio = ambiente.CorDe(Epoca.Presente2026);

        Assert.Greater(
            quente.r - quente.b, frio.r - frio.b,
            "1986 tem de ser mais quente do que 2026");

        Assert.Greater(
            ambiente.IntensidadeDe(Epoca.Passado1986),
            ambiente.IntensidadeDe(Epoca.Presente2026),
            "a cidade viva de 1986 tem mais luz acesa do que a de 2026");
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
