using System.Collections.Generic;
using UnityEngine;

// Realce do objecto focado.
//
// Vai por MaterialPropertyBlock e nunca por `renderer.material`: o segundo
// instancia o material e, como o projecto não tem materiais próprios, todos os
// objectos partilham o mesmo — pintar um pintava a cena inteira, e a instância
// ficava a vazar em cada foco.
//
// É a matéria a aquecer, não luz a acender: sobe o valor e puxa o tom para o
// quente sobre o _BaseColor, sem emissão e sem shader novo. A direcção
// artística é explícita — «um acento por plano, e é sempre matéria, nunca luz»
// (§7.2) — e a emissão, além disso, nem sequer é fiável por property block:
// se o material tiver a keyword _EMISSION desligada, escrever _EmissionColor
// não produz efeito nenhum.
//
// Não há risco de contaminar uma fotografia: o InteractionSystem limpa o foco
// assim que qualquer camada abre, e o modo fotografia é uma delas — o realce
// nunca está aceso enquanto se enquadra ou se alinha.
public class HighlightSystem : MonoBehaviour
{
    [Header("Realce")]
    // Folha morta, o acento quente da paleta. Não é um vermelho de videojogo:
    // a ferrugem é o único vermelho do jogo e não se gasta aqui.
    [SerializeField] private Color highlightTint = new Color(0.54f, 0.35f, 0.17f);

    [SerializeField, Range(0f, 1f)] private float tintAmount = 0.18f;
    [SerializeField, Range(0f, 1f)] private float lift = 0.12f;

    [Header("Resposta")]
    // Rápido a chegar, lento a sair: o realce não pisca quando o raio entra e
    // sai do alvo em frames seguidos.
    [SerializeField] private float fadeInSpeed = 8f;
    [SerializeField] private float fadeOutSpeed = 4f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<Color> originais = new List<Color>();

    private MaterialPropertyBlock bloco;

    private GameObject focado;
    private float intensidade;

    public GameObject Focado => focado;
    public float Intensidade => intensidade;

    private void Awake()
    {
        bloco = new MaterialPropertyBlock();
    }

    public void Focus(GameObject alvo)
    {
        if (alvo == focado)
            return;

        Clear();

        if (alvo == null)
            return;

        focado = alvo;

        alvo.GetComponentsInChildren(true, renderers);

        for (int i = 0; i < renderers.Count; i++)
            originais.Add(LerCor(renderers[i]));
    }

    public void Clear()
    {
        if (focado != null)
        {
            intensidade = 0f;
            Aplicar();
        }

        focado = null;
        intensidade = 0f;

        renderers.Clear();
        originais.Clear();
    }

    private void Update()
    {
        bool tem = focado != null;

        float alvo = tem ? 1f : 0f;
        float velocidade = tem ? fadeInSpeed : fadeOutSpeed;

        intensidade = Mathf.MoveTowards(
            intensidade, alvo, Time.deltaTime * velocidade);

        if (tem)
            Aplicar();
    }

    private Color LerCor(Renderer r)
    {
        if (r == null || r.sharedMaterial == null)
            return Color.white;

        if (r.sharedMaterial.HasProperty(BaseColorId))
            return r.sharedMaterial.GetColor(BaseColorId);

        if (r.sharedMaterial.HasProperty(ColorId))
            return r.sharedMaterial.GetColor(ColorId);

        return Color.white;
    }

    private void Aplicar()
    {
        if (bloco == null)
            bloco = new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];

            if (r == null)
                continue;

            Color origem = i < originais.Count ? originais[i] : Color.white;
            Color realcada = Realcar(origem, intensidade);

            r.GetPropertyBlock(bloco);

            if (r.sharedMaterial != null &&
                r.sharedMaterial.HasProperty(BaseColorId))
            {
                bloco.SetColor(BaseColorId, realcada);
            }

            if (r.sharedMaterial != null &&
                r.sharedMaterial.HasProperty(ColorId))
            {
                bloco.SetColor(ColorId, realcada);
            }

            r.SetPropertyBlock(bloco);
        }
    }

    // Exposto para os testes poderem afirmar a curva sem depender de render.
    public Color Realcar(Color origem, float t)
    {
        Color quente = Color.Lerp(origem, highlightTint, tintAmount);
        Color clara = quente + new Color(lift, lift, lift, 0f);

        Color destino = new Color(
            Mathf.Clamp01(clara.r),
            Mathf.Clamp01(clara.g),
            Mathf.Clamp01(clara.b),
            origem.a);

        return Color.Lerp(origem, destino, Mathf.Clamp01(t));
    }
}
