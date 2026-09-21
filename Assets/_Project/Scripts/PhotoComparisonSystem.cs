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
    private readonly List<PhotoDiscovery> descobertasReveladas =
        new List<PhotoDiscovery>();

    public IReadOnlyList<PhotoDiscovery> DescobertasReveladas =>
        descobertasReveladas;

    // Fica a true quando o alinhamento acontece e é consumido uma só vez, à
    // maneira do ConsumeBack. É o gancho da FASE 7: o sistema de flashbacks
    // pergunta por ele no seu Update em vez de haver um evento — este projecto
    // não usa eventos nem coroutines, tudo é polled.
    private bool alinhamentoPorConsumir;

    public void Abrir(PhotoData fixa, PhotoData movel)
    {
        if (fixa == null || movel == null)
            return;

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

        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.ComparingPhotos);
    }

    private void Update()
    {
        IsOpen = painel != null && painel.activeSelf;

        if (!IsOpen)
            return;

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

    public void Mover(Vector2 delta)
    {
        Posicao = new Vector2(
            Mathf.Clamp(Posicao.x + delta.x, -alcanceDaPosicao, alcanceDaPosicao),
            Mathf.Clamp(Posicao.y + delta.y, -alcanceDaPosicao, alcanceDaPosicao)
        );

        Desenhar();

        VerificarAlinhamento();
    }

    public void Rodar(float graus)
    {
        // Entre -180 e 180: sem isto, rodar sempre no mesmo sentido fazia o
        // valor crescer sem fim e a comparação com o alvo deixava de bater.
        Rotacao = Mathf.Repeat(Rotacao + graus + 180f, 360f) - 180f;

        Desenhar();

        VerificarAlinhamento();
    }

    public void Escalar(float delta)
    {
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

            if (descoberta.Revelar())
                descobertasReveladas.Add(descoberta);
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
        RectTransform rect = imagemMovel.rectTransform;

        if (rect == null)
            return;

        if (Modo == ModoDeComparacao.LadoALado)
        {
            // Lado a lado é a vista de arrumação: as duas ao lado uma da
            // outra, sem transformação aplicada. O que o jogador mexeu fica
            // guardado e volta quando sobrepuser outra vez.
            rect.anchoredPosition = new Vector2(rect.rect.width, 0f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            return;
        }

        // A posição é normalizada à largura da própria fotografia, para que o
        // mesmo alinhamento autorado valha em qualquer resolução de ecrã.
        rect.anchoredPosition = new Vector2(
            Posicao.x * rect.rect.width,
            Posicao.y * rect.rect.height
        );

        rect.localRotation = Quaternion.Euler(0f, 0f, Rotacao);
        rect.localScale = new Vector3(Escala, Escala, 1f);
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
