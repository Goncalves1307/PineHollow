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

    public bool Read(string title, string body)
    {
        if (isReading)
            return false;

        // Sem painel não se empurra o modo. Empurrar na mesma trancava o
        // jogador — nem anda, nem olha, cursor preso — com o ecrã na mesma, e
        // só o Esc o tirava de lá.
        if (readingPanel == null)
        {
            Debug.LogError(
                $"[{name}] Ecrã de leitura sem painel: não abro para não " +
                "trancar o jogador num ecrã que não existe.",
                this);
            return false;
        }

        isReading = true;

        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;

        readingPanel.SetActive(true);

        if (stateMachine != null)
            stateMachine.PushMode(PlayerState.Reading);

        return true;
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
