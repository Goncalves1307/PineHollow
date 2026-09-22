using UnityEngine;

// A luz das duas épocas.
//
// A direcção artística §7 não trata a temperatura de luz como decoração: diz
// que ELA É a regra das duas épocas na prática. 1986 é quente porque há
// tungsténio aceso — a cidade está viva, as casas habitadas, a fábrica a
// trabalhar. 2026 é frio porque já não há nada aceso e só entra o céu. É essa
// diferença que o jogador lê antes de ler qualquer outra coisa, e é meia
// direcção de arte ao preço de uma luz.
//
// A geometria não muda entre épocas — §5, e é o que torna o alinhamento
// possível. Portanto tudo o que distingue as duas épocas sem ser os três a
// cinco marcadores por divisão está neste ficheiro.
public class AmbienteDeEpoca : MonoBehaviour
{
    [Header("A luz que entra")]
    [SerializeField] private Light luzDoCeu;

    [Header("2026 — só entra o céu")]
    [SerializeField] private Color corDe2026 = new Color(0.72f, 0.80f, 0.92f);
    [SerializeField] private float intensidadeDe2026 = 0.9f;
    [SerializeField] private Color ambienteDe2026 = new Color(0.24f, 0.28f, 0.34f);

    [Header("1986 — tungsténio aceso")]
    [SerializeField] private Color corDe1986 = new Color(1f, 0.85f, 0.66f);
    [SerializeField] private float intensidadeDe1986 = 1.25f;
    [SerializeField] private Color ambienteDe1986 = new Color(0.34f, 0.28f, 0.21f);

    public Color CorDe(Epoca epoca) =>
        epoca == Epoca.Passado1986 ? corDe1986 : corDe2026;

    public float IntensidadeDe(Epoca epoca) =>
        epoca == Epoca.Passado1986 ? intensidadeDe1986 : intensidadeDe2026;

    public Color AmbienteDe(Epoca epoca) =>
        epoca == Epoca.Passado1986 ? ambienteDe1986 : ambienteDe2026;

    private void Start()
    {
        Aplicar(EstadoDoMundo.Instancia.EpocaActual);
    }

    // Público e sem input, como manda a casa: o que decide o aspecto de uma
    // época tem de poder ser verificado sem play mode.
    public void Aplicar(Epoca epoca)
    {
        if (luzDoCeu != null)
        {
            luzDoCeu.color = CorDe(epoca);
            luzDoCeu.intensity = IntensidadeDe(epoca);
        }

        // As RenderSettings são GLOBAIS e vivem no ficheiro da cena. Mexer-lhes
        // fora de play mode sujava a cena a cada corrida de testes, e essa
        // sujidade entrava depois num commit sem ninguém reparar — é a mesma
        // família de problemas do batchmode a reserializar o volume profile.
        // Em EditMode verifica-se o que este componente CALCULA (AmbienteDe),
        // que é a parte que tem regra; pintar o mundo é play mode.
        if (Application.isPlaying)
            RenderSettings.ambientLight = AmbienteDe(epoca);
    }
}
