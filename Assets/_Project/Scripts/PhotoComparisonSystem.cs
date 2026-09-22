using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum ModoDeComparacao
{
    LadoALado,
    Sobreposicao
}

// Duas fotografias lado a lado ou uma sobre a outra, com escala, posição e
// rotação no rato — e a detecção de quando ficam alinhadas.
//
// A manipulação e a detecção são métodos públicos sem input nenhum lá dentro,
// e o Update só lhes chama. É de propósito: em EditMode o Mouse.current é
// null, e uma verificação que só corra em play mode não é verificação. Assim
// o que decide se o jogador alinhou ou não tem testes.
public class PhotoComparisonSystem : MonoBehaviour
{
    [Header("Painel")]
    [SerializeField] private GameObject painel;
    [SerializeField] private RawImage imagemFixa;
    [SerializeField] private RawImage imagemMovel;
    [SerializeField] private TMP_Text legenda;

    [Header("Limites")]
    [SerializeField] private float escalaMinima = 0.25f;
    [SerializeField] private float escalaMaxima = 4f;
    [SerializeField] private float alcanceDaPosicao = 2f;

    [Header("Sensibilidade")]
    [SerializeField] private float velocidadeDeArrasto = 0.002f;
    [SerializeField] private float velocidadeDeRotacao = 0.2f;
    [SerializeField] private float velocidadeDeZoom = 0.001f;

    [Header("Sobreposição")]
    [Range(0f, 1f)]
    [SerializeField] private float opacidadeSobreposta = 0.55f;

    [Header("Alinhamento")]
    // O FLASH. O GDD nunca diz ao jogador que duas fotografias alinham — mas
    // quando ele o descobre, o jogo tem de o confirmar, ou a descoberta passa
    // despercebida. É o mesmo clarão que a FASE 7 vai usar para entrar no
    // flashback, e por isso vive aqui e não lá.
    [SerializeField] private Image flash;
    [SerializeField] private float duracaoDoFlash = 0.45f;

    [Header("State")]
    [SerializeField] private PlayerStateMachine stateMachine;

    public bool IsOpen { get; private set; }

    public PhotoData Fixa { get; private set; }
    public PhotoData Movel { get; private set; }

    public ModoDeComparacao Modo { get; private set; }

    public Vector2 Posicao { get; private set; }
    public float Rotacao { get; private set; }
    public float Escala { get; private set; } = 1f;

    public bool Alinhada { get; private set; }

    // O que se descobriu no alinhamento mais recente. Lista e não um sim/não
    // porque uma fotografia pode esconder mais do que uma coisa.
    //
    // É limpa a cada alinhamento: isto é o registo do ÚLTIMO, não um arquivo.
    // O arquivo é o RegistoDeConhecimento, para onde cada descoberta é copiada
    // no momento em que sai — sem isso, o alinhamento seguinte apagava o que o
    // jogador tinha acabado de descobrir e não sobrava vestígio nenhum.
    private readonly List<PhotoDiscovery> descobertasReveladas =
        new List<PhotoDiscovery>();

    public IReadOnlyList<PhotoDiscovery> DescobertasReveladas =>
        descobertasReveladas;

    // Fica a true quando o alinhamento acontece e é consumido uma só vez, à
    // maneira do ConsumeBack. É o gancho da FASE 7: o sistema de flashbacks
    // pergunta por ele no seu Update em vez de haver um evento — este projecto
    // não usa eventos nem coroutines, tudo é polled.
    private bool alinhamentoPorConsumir;

    private float flashRestante;

    // 0 quando não há clarão, 1 no instante do alinhamento. Legível de fora
    // para ter teste: o Time.deltaTime é zero em EditMode.
    public float IntensidadeDoFlash =>
        duracaoDoFlash <= 0f ? 0f : Mathf.Clamp01(flashRestante / duracaoDoFlash);

    public void Abrir(PhotoData fixa, PhotoData movel)
    {
        if (fixa == null || movel == null)
            return;

        // Sem painel não há Update que corra — o IsOpen é lido dele. Empurrar
        // o modo aqui deixava o ComparingPhotos na pilha sem ninguém a
        // consumir o Esc: cursor solto, movimento trancado, e só a saída de
        // emergência da máquina de estados o resgatava.
        if (painel == null)
        {
            Debug.LogError(
                $"{name}: painel da comparação por ligar no inspector — " +
                "comparar não faz nada.",
                this
            );

            return;
        }

        Fixa = fixa;
        Movel = movel;

        // Começa lado a lado. Sobrepor primeiro escondia uma das duas e dava a
        // ideia errada de que o jogo já tinha feito o trabalho.
        Modo = ModoDeComparacao.LadoALado;

        Posicao = Vector2.zero;
        Rotacao = 0f;
        Escala = 1f;

        Alinhada = false;
        alinhamentoPorConsumir = false;
        flashRestante = 0f;
        descobertasReveladas.Clear();

        Desenhar();

        if (painel != null)
            painel.SetActive(true);

        if (stateMachine != null)
            stateMachine.PushMode(PlayerState.ComparingPhotos);
    }

    public void Fechar()
    {
        if (painel != null)
            painel.SetActive(false);

        IsOpen = false;

        Fixa = null;
        Movel = null;
        flashRestante = 0f;

        DesenharFlash();

        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.ComparingPhotos);
    }

    private void Update()
    {
        IsOpen = painel != null && painel.activeSelf;

        if (!IsOpen)
            return;

        AvancarFlash(Time.deltaTime);

        if (stateMachine != null &&
            stateMachine.IsTopMode(PlayerState.ComparingPhotos))
        {
            LerInput();
        }

        if (stateMachine != null &&
            stateMachine.ConsumeBack(PlayerState.ComparingPhotos))
        {
            Fechar();
        }
    }

    private void LerInput()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            AlternarModo();
        }

        if (Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        if (Mouse.current.leftButton.isPressed)
            Mover(delta * velocidadeDeArrasto);

        if (Mouse.current.rightButton.isPressed)
            Rodar(-delta.x * velocidadeDeRotacao);

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
            Escalar(scroll * velocidadeDeZoom);
    }

    public void AlternarModo()
    {
        Modo = Modo == ModoDeComparacao.LadoALado
            ? ModoDeComparacao.Sobreposicao
            : ModoDeComparacao.LadoALado;

        Desenhar();

        VerificarAlinhamento();
    }

    // Mover, rodar e escalar só fazem sentido com uma fotografia por cima da
    // outra. Lado a lado, o arrasto não mexia nada no ecrã mas os valores
    // acumulavam à mesma — e ao sobrepor, a fotografia saltava para uma
    // posição que o jogador nunca viu. A guarda está aqui e não no leitor do
    // rato para valer a qualquer chamador, e para ter teste.
    public void Mover(Vector2 delta)
    {
        if (Modo != ModoDeComparacao.Sobreposicao)
            return;

        Posicao = new Vector2(
            Mathf.Clamp(Posicao.x + delta.x, -alcanceDaPosicao, alcanceDaPosicao),
            Mathf.Clamp(Posicao.y + delta.y, -alcanceDaPosicao, alcanceDaPosicao)
        );

        Desenhar();

        VerificarAlinhamento();
    }

    public void Rodar(float graus)
    {
        if (Modo != ModoDeComparacao.Sobreposicao)
            return;

        // Entre -180 e 180: sem isto, rodar sempre no mesmo sentido fazia o
        // valor crescer sem fim e a comparação com o alvo deixava de bater.
        Rotacao = Mathf.Repeat(Rotacao + graus + 180f, 360f) - 180f;

        Desenhar();

        VerificarAlinhamento();
    }

    public void Escalar(float delta)
    {
        if (Modo != ModoDeComparacao.Sobreposicao)
            return;

        Escala = Mathf.Clamp(Escala + delta, escalaMinima, escalaMaxima);

        Desenhar();

        VerificarAlinhamento();
    }

    // O coração da fase. Devolve true no frame em que o alinhamento passa a
    // existir — e só nesse, para não revelar a mesma coisa a cada pixel que o
    // rato ande dentro da tolerância.
    public bool VerificarAlinhamento()
    {
        bool antes = Alinhada;

        Alinhada = EstaAlinhada();

        if (!Alinhada || antes)
            return false;

        Revelar();

        alinhamentoPorConsumir = true;

        flashRestante = duracaoDoFlash;
        DesenharFlash();

        return true;
    }

    private bool EstaAlinhada()
    {
        // Lado a lado não alinha nada: alinhar é pôr uma sobre a outra, e
        // aceitar aqui daria o gatilho de graça mal se abrisse a comparação
        // de um par declarado.
        if (Modo != ModoDeComparacao.Sobreposicao)
            return false;

        if (Fixa == null || Movel == null)
            return false;

        PhotoAlignment alinhamento = Fixa.AlinhamentoPara(Movel);

        if (alinhamento == null)
            return false;

        return alinhamento.Coincide(Posicao, Rotacao, Escala);
    }

    private void Revelar()
    {
        descobertasReveladas.Clear();

        Revelar(Fixa, Movel);
        Revelar(Movel, Fixa);
    }

    private void Revelar(PhotoData fotografia, PhotoData par)
    {
        if (fotografia == null)
            return;

        foreach (PhotoDiscovery descoberta in fotografia.Descobertas)
        {
            if (descoberta == null || !descoberta.SaiDoAlinhamentoCom(par))
                continue;

            if (!descoberta.Revelar())
                continue;

            descobertasReveladas.Add(descoberta);

            FontesDeConhecimento.RegistarDescoberta(
                RegistoDeConhecimento.Instancia,
                fotografia,
                descoberta,
                par
            );
        }
    }

    // O gancho da FASE 7. Devolve true uma só vez por alinhamento: quem o
    // consumir é quem faz o FLASH e leva o jogador a 1986. Enquanto esse
    // sistema não existir, ninguém chama isto e o alinhamento fica só
    // registado — é o limite honesto desta fase.
    public bool ConsumirAlinhamento()
    {
        if (!alinhamentoPorConsumir)
            return false;

        alinhamentoPorConsumir = false;

        return true;
    }

    private void Desenhar()
    {
        if (imagemFixa != null)
            imagemFixa.texture = Fixa != null ? Fixa.Imagem : null;

        if (imagemMovel != null)
        {
            imagemMovel.texture = Movel != null ? Movel.Imagem : null;

            // Em sobreposição a de cima fica translúcida, senão tapava a de
            // baixo e não havia nada para alinhar.
            Color cor = imagemMovel.color;

            cor.a = Modo == ModoDeComparacao.Sobreposicao
                ? opacidadeSobreposta
                : 1f;

            imagemMovel.color = cor;

            AplicarTransformacao();
        }

        if (legenda != null)
            legenda.text = Legendar();
    }

    private void AplicarTransformacao()
    {
        RectTransform movel = imagemMovel != null ? imagemMovel.rectTransform : null;
        RectTransform fixa = imagemFixa != null ? imagemFixa.rectTransform : null;

        if (movel == null)
            return;

        float largura = fixa != null ? fixa.rect.width : movel.rect.width;
        float afastamento = largura * 0.55f;

        if (Modo == ModoDeComparacao.LadoALado)
        {
            // Lado a lado é a vista de arrumação: as duas ao lado uma da
            // outra, sem transformação aplicada. O que o jogador mexeu fica
            // guardado e volta quando sobrepuser outra vez.
            if (fixa != null)
                fixa.anchoredPosition = new Vector2(-afastamento, 0f);

            movel.anchoredPosition = new Vector2(afastamento, 0f);
            movel.localRotation = Quaternion.identity;
            movel.localScale = Vector3.one;

            return;
        }

        // A FIXA VAI AO CENTRO. Sem isto ficava onde a vista de arrumação a
        // tinha deixado, e a Posicao da móvel passava a ser medida a partir do
        // centro do painel em vez de a partir da fotografia com que se quer
        // alinhar: o jogo dava Alinhada = true com as duas imagens mais de
        // meia largura afastadas, e no ponto em que o jogador as sobrepunha de
        // facto não disparava nada.
        if (fixa != null)
            fixa.anchoredPosition = Vector2.zero;

        // Normalizado pela LARGURA nos dois eixos, e não por largura em x e
        // altura em y. Com um rect não quadrado, um espaço anisotrópico faz
        // com que rodar em coordenadas normalizadas não seja rodar em píxeis —
        // e o alinhamento lido do lado invertido, que roda a translação,
        // deixava de coincidir por dezenas de píxeis.
        movel.anchoredPosition = Posicao * largura;

        movel.localRotation = Quaternion.Euler(0f, 0f, Rotacao);
        movel.localScale = new Vector3(Escala, Escala, 1f);
    }

    // O clarão apaga-se sozinho. Recebe o delta em vez de ler o Time para ter
    // teste: em EditMode o Time.deltaTime é zero e o flash nunca acabava.
    public void AvancarFlash(float delta)
    {
        if (flashRestante <= 0f)
            return;

        flashRestante = Mathf.Max(0f, flashRestante - Mathf.Max(0f, delta));

        DesenharFlash();
    }

    private void DesenharFlash()
    {
        if (flash == null)
            return;

        float intensidade = IntensidadeDoFlash;

        Color cor = flash.color;
        cor.a = intensidade;
        flash.color = cor;

        // Desligado quando não há clarão: um Image transparente a ecrã inteiro
        // continua a apanhar o rato e tapava as miniaturas por baixo.
        if (flash.gameObject.activeSelf != intensidade > 0f)
            flash.gameObject.SetActive(intensidade > 0f);
    }

    private string Legendar()
    {
        if (Fixa == null || Movel == null)
            return string.Empty;

        string modo = Modo == ModoDeComparacao.LadoALado
            ? "Lado a lado"
            : "Sobreposição";

        // Os anos são o que interessa ver aqui: é a distância entre eles que
        // faz a história. Nada de dizer ao jogador se está perto de alinhar.
        return $"{Fixa.Ano}  ·  {Movel.Ano}\n{modo}   [Espaço] alternar   " +
               "[Arrastar] mover   [Direito] rodar   [Scroll] escala";
    }
}
