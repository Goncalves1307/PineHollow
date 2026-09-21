using UnityEngine;
using TMPro;

// Ecrã de leitura de documentos. Segue a receita das camadas do AGENTS.md:
// empurra o modo, fecha no ConsumeBack, e não liberta o cursor.
//
// Não é HUD: só existe enquanto se lê, que é o que o GDD permite ("não deve
// existir um HUD permanente pesado" — GDD:345).
public class ReadingSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject readingPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Estado")]
    [SerializeField] private PlayerStateMachine stateMachine;

    private bool isReading;

    public bool IsReading => isReading;

    private void Start()
    {
        if (readingPanel != null)
            readingPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isReading)
            return;

        // ConsumeBack devolve true só a quem está no topo da pilha, uma vez por
        // Esc. É o que impede uma tecla de fechar duas camadas de uma vez.
        if (stateMachine != null &&
            stateMachine.ConsumeBack(PlayerState.Reading))
        {
            Close();
        }
    }

    public void Read(string title, string body)
    {
        if (isReading)
            return;

        isReading = true;

        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;

        if (readingPanel != null)
            readingPanel.SetActive(true);

        if (stateMachine != null)
            stateMachine.PushMode(PlayerState.Reading);
    }

    public void Close()
    {
        if (!isReading)
            return;

        isReading = false;

        if (readingPanel != null)
            readingPanel.SetActive(false);

        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.Reading);
    }
}
