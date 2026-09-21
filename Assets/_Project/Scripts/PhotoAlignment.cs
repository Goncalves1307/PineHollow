using System;
using UnityEngine;

// O alinhamento entre duas fotografias, autorado e não adivinhado.
//
// Um PhotoAlignment escrito na fotografia X com comId = Y quer dizer: quando
// Y for sobreposta a X, Y fica alinhada nesta posição, com esta rotação e
// esta escala — mais ou menos a tolerância.
//
// Porquê declarado: comparar pixéis para descobrir sozinho que duas
// fotografias de décadas diferentes mostram o mesmo sítio é um problema de
// visão computacional, não de desenho de jogo, e daria falsos positivos em
// cima de conteúdo narrativo. Declarar mantém o autor no controlo.
//
// E porquê com tolerância: o GDD proíbe marcador a dizer «alinha estas duas».
// O jogador tem de encontrar a sobreposição com a mão, e uma coincidência
// exacta ao pixel não se consegue com o rato. A tolerância é o que separa
// «descobriu» de «acertou por acaso».
[Serializable]
public class PhotoAlignment
{
    [SerializeField] private string comId;

    [Header("Alvo")]
    // Deslocamento em coordenadas normalizadas: (0,0) é sobreposta à
    // fotografia fixa, (0.5, 0) é meia largura à direita. Normalizado à
    // LARGURA nos dois eixos — o espaço é isotrópico de propósito, senão
    // rodar em normalizado não seria rodar em píxeis e a inversa deixava de
    // bater num rect que não fosse quadrado.
    [SerializeField] private Vector2 posicaoAlvo;

    [SerializeField] private float rotacaoAlvo;
    [SerializeField] private float escalaAlvo = 1f;

    [Header("Tolerância")]
    [SerializeField] private float toleranciaPosicao = 0.05f;
    [SerializeField] private float toleranciaRotacao = 5f;
    [SerializeField] private float toleranciaEscala = 0.08f;

    public string ComId => comId;
    public Vector2 PosicaoAlvo => posicaoAlvo;
    public float RotacaoAlvo => rotacaoAlvo;
    public float EscalaAlvo => escalaAlvo;

    public PhotoAlignment()
    {
    }

    private PhotoAlignment(PhotoAlignment outro)
    {
        comId = outro.comId;
        posicaoAlvo = outro.posicaoAlvo;
        rotacaoAlvo = outro.rotacaoAlvo;
        escalaAlvo = outro.escalaAlvo;
        toleranciaPosicao = outro.toleranciaPosicao;
        toleranciaRotacao = outro.toleranciaRotacao;
        toleranciaEscala = outro.toleranciaEscala;
    }

    public PhotoAlignment Copiar()
    {
        return new PhotoAlignment(this);
    }

    // O mesmo alinhamento visto do outro lado: quem autorou o par numa das
    // fotografias não tem de o repetir na outra, e sem inverter de verdade a
    // segunda leitura dava um alvo errado.
    //
    // A transformação é uma semelhança 2D, T(p) = s·R(θ)·p + t. A inversa é
    // T⁻¹(p) = (1/s)·R(−θ)·p − (1/s)·R(−θ)·t — daí a translação também rodar
    // e escalar, e não bastar trocar-lhe o sinal.
    public PhotoAlignment Inverter(string novoComId)
    {
        float escala = Mathf.Approximately(escalaAlvo, 0f) ? 1f : escalaAlvo;

        float radianos = -rotacaoAlvo * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radianos);
        float sin = Mathf.Sin(radianos);

        Vector2 rodada = new Vector2(
            posicaoAlvo.x * cos - posicaoAlvo.y * sin,
            posicaoAlvo.x * sin + posicaoAlvo.y * cos
        );

        return new PhotoAlignment
        {
            comId = novoComId,
            posicaoAlvo = -rodada / escala,
            rotacaoAlvo = -rotacaoAlvo,
            escalaAlvo = 1f / escala,

            // As tolerâncias vivem no espaço da fotografia movida, por isso
            // acompanham a mudança de escala. A de rotação é um ângulo e não
            // muda. A de escala vai a 1/s² e não a 1/s: a imagem do intervalo
            // |e − s| ≤ t por e ↦ 1/e é ≈ t/s², e com 1/s a inversa ficava
            // quatro vezes mais permissiva do que a directa a s = 4.
            toleranciaPosicao = toleranciaPosicao / escala,
            toleranciaRotacao = toleranciaRotacao,
            toleranciaEscala = toleranciaEscala / (escala * escala)
        };
    }

    public bool Coincide(Vector2 posicao, float rotacao, float escala)
    {
        if (Vector2.Distance(posicao, posicaoAlvo) > toleranciaPosicao)
            return false;

        if (DiferencaAngular(rotacao, rotacaoAlvo) > toleranciaRotacao)
            return false;

        return Mathf.Abs(escala - escalaAlvo) <= toleranciaEscala;
    }

    // 359° e 1° estão a 2° um do outro, não a 358°. Sem isto, uma fotografia
    // rodada quase até dar a volta nunca alinhava.
    public static float DiferencaAngular(float a, float b)
    {
        return Mathf.Abs(Mathf.DeltaAngle(a, b));
    }
}
