using UnityEngine;

// Documento que se lê — carta, recorte, relatório. O texto vive no próprio
// objecto e é canon: o Pine_Hollow_Historia.md tem as linhas exactas, e
// placeholder tem o hábito de sobreviver.
public class DocumentInteractable : MonoBehaviour, IInteractable
{
    [Header("Conteúdo")]
    [SerializeField] private string title = "Documento";
    [TextArea(3, 12)]
    [SerializeField] private string body;

    [Header("Ecrã")]
    [SerializeField] private ReadingSystem readingSystem;

    [Header("Estado")]
    [SerializeField] private string stateId;

    private bool hasBeenRead;

    public string StateId => stateId;

    // A investigação da FASE 6 precisa de saber o que já foi lido; o save da
    // FASE 21 precisa de o gravar.
    public bool HasBeenRead => hasBeenRead;

    public void Interact()
    {
        if (readingSystem == null)
            return;

        hasBeenRead = true;
        readingSystem.Read(title, body);
    }

    public string GetInteractionText()
    {
        return "Ler";
    }
}
