using UnityEngine;
using UnityEngine.UI;

// O véu que cobre o ecrã enquanto o mundo troca de época.
//
// Existe por uma razão mecânica e não estética: a troca liga e desliga dezenas
// de objectos no mesmo frame, e vê-los aparecer e desaparecer à frente dos
// olhos estraga o efeito inteiro — o jogador não viajou no tempo, viu uma cena
// a ser remontada. O véu cobre, o mundo muda por trás, o véu levanta.
//
// NÃO é pós-processamento, e isso é uma regra dura deste projecto: nada de
// aberração cromática, distorção de lente, panini ou motion blur. Os quatro
// foram removidos na FASE 1 porque DESLOCAM A IMAGEM, e este jogo pede ao
// jogador que alinhe uma fotografia com uma divisão — alinhar contra uma
// imagem que a lente empurrou é impossível e ninguém percebe porquê. Um Image
// preto por cima não move um píxel de sítio.
//
// A cobertura avança por um delta recebido em parâmetro e não pelo Time, para
// ter teste: em EditMode o Time.deltaTime é zero e a transição nunca acabava.
public class TransicaoDeEpoca : MonoBehaviour
{
    [SerializeField] private Image veu;

    [Tooltip("Segundos a cobrir, e outros tantos a descobrir.")]
    [SerializeField] private float duracao = 0.8f;

    // 0 é ecrã limpo, 1 é ecrã coberto.
    public float Cobertura { get; private set; }

    public bool EstaCoberto => Cobertura >= 1f;
    public bool EstaLimpo => Cobertura <= 0f;

    public float Duracao => duracao;

    private void Start()
    {
        Desenhar();
    }

    // Avança na direcção pedida. Devolve true quando chegou ao fim desse lado —
    // é assim que o SistemaDeFlashback sabe que pode trocar o mundo sem que se
    // veja.
    public bool Avancar(float delta, bool aCobrir)
    {
        float passo = duracao <= 0f
            ? 1f
            : Mathf.Max(0f, delta) / duracao;

        Cobertura = aCobrir
            ? Mathf.Min(1f, Cobertura + passo)
            : Mathf.Max(0f, Cobertura - passo);

        Desenhar();

        return aCobrir ? EstaCoberto : EstaLimpo;
    }

    // Põe o véu num estado sem animar. Para quem começa já dentro de uma época
    // e não quer ver a transição de arranque.
    public void Definir(float cobertura)
    {
        Cobertura = Mathf.Clamp01(cobertura);

        Desenhar();
    }

    private void Desenhar()
    {
        if (veu == null)
            return;

        Color cor = veu.color;
        cor.a = Cobertura;
        veu.color = cor;

        // Desligado quando não cobre nada. Um Image a ecrã inteiro, ainda que
        // transparente, continua a apanhar raycasts do UI e a custar
        // preenchimento — é o mesmo cuidado que o clarão da comparação já tem.
        if (veu.gameObject.activeSelf != (Cobertura > 0f))
            veu.gameObject.SetActive(Cobertura > 0f);
    }
}
