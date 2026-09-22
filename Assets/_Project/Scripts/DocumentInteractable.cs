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

    // A investigação da FASE 6 lê o que o documento diz para o pôr no caderno.
    // Eram privados sem getter, e por isso o caderno sabia que um documento
    // tinha sido lido e não sabia qual.
    public string Title => title;
    public string Body => body;

    // A investigação da FASE 6 precisa de saber o que já foi lido; o save da
    // FASE 21 precisa de o gravar.
    //
    // Continua a ser um campo privado NÃO serializado, de propósito: marcá-lo
    // com [SerializeField] gravava estado de jogo dentro da cena em disco, que
    // é o mesmo erro que a FASE 5 evitou ao fazer o PhotoAsset entregar uma
    // cópia. Quem se lembra entre sessões é o RegistoDeConhecimento.
    public bool HasBeenRead => hasBeenRead;

    public void Interact()
    {
        if (readingSystem == null)
            return;

        // Só conta como lido se o ecrã abriu mesmo. Marcá-lo antes dava um
        // documento lido para a investigação da FASE 6 sem o jogador ter visto
        // uma linha.
        if (!readingSystem.Read(title, body))
            return;

        hasBeenRead = true;

        FontesDeConhecimento.RegistarDocumento(
            RegistoDeConhecimento.Instancia,
            stateId,
            title,
            body
        );
    }

    public string GetInteractionText()
    {
        return "Ler";
    }
}
