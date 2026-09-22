using UnityEngine;

// O som das duas épocas, cruzado em vez de cortado.
//
// Cortar seco denuncia a troca: ouve-se o corte e percebe-se que houve um
// carregamento, não uma viagem. O cruzamento faz a fábrica de 1986 aparecer por
// baixo do silêncio de 2026 antes de o véu levantar — o jogador já está no
// passado quando o vê.
//
// 1986 tem a fábrica a trabalhar e a cidade viva; 2026 tem o vento e pouco
// mais. A mistura avança por um delta recebido em parâmetro, pela razão do
// costume: em EditMode o Time.deltaTime é zero.
public class AmbienteSonoroDeEpoca : MonoBehaviour
{
    [SerializeField] private AudioSource somDe2026;
    [SerializeField] private AudioSource somDe1986;

    [Tooltip("Segundos de cruzamento de uma época para a outra.")]
    [SerializeField] private float duracaoDoCruzamento = 1.5f;

    [Range(0f, 1f)]
    [SerializeField] private float volumeMaximo = 1f;

    // 0 é 2026 a tocar sozinho, 1 é 1986 a tocar sozinho.
    public float Mistura { get; private set; }

    public float VolumeDe2026 => (1f - Mistura) * volumeMaximo;
    public float VolumeDe1986 => Mistura * volumeMaximo;

    private void Start()
    {
        Definir(EstadoDoMundo.Instancia.EpocaActual);

        Tocar();
    }

    // Põe a mistura onde deve estar sem cruzar nada. Para arrancar já dentro
    // de uma época.
    public void Definir(Epoca epoca)
    {
        Mistura = epoca == Epoca.Passado1986 ? 1f : 0f;

        Aplicar();
    }

    // Aproxima a mistura da época pedida. Devolve true quando lá chegou.
    public bool Avancar(float delta, Epoca alvo)
    {
        float destino = alvo == Epoca.Passado1986 ? 1f : 0f;

        float passo = duracaoDoCruzamento <= 0f
            ? 1f
            : Mathf.Max(0f, delta) / duracaoDoCruzamento;

        Mistura = Mathf.MoveTowards(Mistura, destino, passo);

        Aplicar();

        return Mathf.Approximately(Mistura, destino);
    }

    private void Aplicar()
    {
        if (somDe2026 != null)
            somDe2026.volume = VolumeDe2026;

        if (somDe1986 != null)
            somDe1986.volume = VolumeDe1986;
    }

    // As duas fontes tocam SEMPRE, em ciclo, e o que muda é o volume. Parar e
    // arrancar a que entra faria a ambiência começar do princípio a cada
    // flashback — e uma ambiência que recomeça ouve-se, porque o ouvido
    // reconhece o mesmo arranque à segunda vez.
    private void Tocar()
    {
        Arrancar(somDe2026);
        Arrancar(somDe1986);
    }

    private static void Arrancar(AudioSource fonte)
    {
        if (fonte == null || fonte.clip == null)
            return;

        fonte.loop = true;

        if (!fonte.isPlaying)
            fonte.Play();
    }
}
