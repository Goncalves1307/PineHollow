using UnityEngine;
using UnityEngine.UI;

// A mira — o `+` que o GDD:342 mostra como sendo, com o prompt, a interface
// toda. Não existia no canvas apesar disso.
//
// Só faz duas coisas: estar lá, e sair do ecrã quando o player não manda. O
// realce do objecto focado é do HighlightSystem: dois canais de feedback para
// a mesma coisa é ruído num jogo que preza contenção.
public class CrosshairSystem : MonoBehaviour
{
    [Header("Mira")]
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private Graphic[] strokes;

    [Header("Aspecto")]
    [SerializeField, Range(0f, 1f)] private float alpha = 0.35f;

    [Header("Estado")]
    [SerializeField] private PlayerStateMachine stateMachine;

    private void Start()
    {
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
    }

    private void Apply()
    {
        if (crosshair == null)
            return;

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
