using UnityEngine;
using UnityEngine.UI;

// A mira, e o realce do objecto focado.
//
// O realce vive aqui e não no objecto de propósito. A direcção artística fixa
// «um acento por plano, e é sempre matéria, nunca luz» (§7.2), e o núcleo do
// jogo é comparar e alinhar uma fotografia com o mundo (§2): um objecto que
// brilha ao ser olhado corrompe a comparação de que a mecânica depende. A mira
// é interface, não mundo — não entra em fotografia nenhuma.
//
// E resolve a queixa que a task fazia: o feedback estava no canto, longe de
// onde o olho já está.
public class CrosshairSystem : MonoBehaviour
{
    [Header("Mira")]
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private Graphic[] strokes;

    [Header("Repouso")]
    [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.35f;
    [SerializeField] private float idleScale = 1f;

    [Header("Com foco")]
    [SerializeField, Range(0f, 1f)] private float focusAlpha = 0.85f;
    [SerializeField] private float focusScale = 1.35f;

    [Header("Resposta")]
    // Rápido o suficiente para parecer imediato, lento o suficiente para não
    // piscar quando o raio entra e sai do alvo em frames seguidos.
    [SerializeField] private float speed = 12f;

    [Header("Estado")]
    [SerializeField] private InteractionSystem interactionSystem;
    [SerializeField] private PlayerStateMachine stateMachine;

    private float alpha;
    private float scale;

    private void Start()
    {
        alpha = idleAlpha;
        scale = idleScale;
        Apply();
    }

    private void Update()
    {
        if (crosshair == null)
            return;

        // Com uma camada aberta ou o cursor solto não se está a apontar a nada:
        // a mira sai do ecrã em vez de ficar por cima da UI.
        bool visivel =
            stateMachine != null &&
            stateMachine.OpenModeCount == 0 &&
            !stateMachine.IsCursorFree;

        if (crosshair.gameObject.activeSelf != visivel)
            crosshair.gameObject.SetActive(visivel);

        if (!visivel)
            return;

        bool comFoco =
            interactionSystem != null && interactionSystem.HasFocus;

        float alvoAlpha = comFoco ? focusAlpha : idleAlpha;
        float alvoScale = comFoco ? focusScale : idleScale;

        float passo = Time.deltaTime * speed;

        alpha = Mathf.MoveTowards(alpha, alvoAlpha, passo);
        scale = Mathf.MoveTowards(scale, alvoScale, passo);

        Apply();
    }

    private void Apply()
    {
        if (crosshair == null)
            return;

        crosshair.localScale = new Vector3(scale, scale, 1f);

        if (strokes == null)
            return;

        for (int i = 0; i < strokes.Length; i++)
        {
            if (strokes[i] == null)
                continue;

            Color cor = strokes[i].color;
            cor.a = alpha;
            strokes[i].color = cor;
        }
    }
}
