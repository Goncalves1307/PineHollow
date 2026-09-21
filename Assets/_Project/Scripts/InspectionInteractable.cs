using UnityEngine;

// Objecto que se pega e se examina à frente da câmara. Quem faz o trabalho é o
// InspectionSystem; este componente só diz qual é o objecto, o verbo e o que
// se vê quando se olha para ele de perto.
public class InspectionInteractable : MonoBehaviour, IInteractable
{
    [Header("Sistema")]
    [SerializeField] private InspectionSystem inspectionSystem;

    // Num jogo sem diálogo constante, é aqui que a narrativa ambiental cabe.
    // O texto é canon — o Pine_Hollow_Historia.md tem os objectos e as linhas
    // exactas, e placeholder tem o hábito de sobreviver.
    [Header("Conteúdo")]
    [TextArea(2, 5)]
    [SerializeField] private string descricao;

    // Todos os outros interactables têm um. A FASE 7 precisa dele para saber
    // que objecto é este nas duas eras, e a FASE 21 para o gravar.
    [Header("Estado")]
    [SerializeField] private string stateId;

    public string StateId => stateId;

    public void Interact()
    {
        // Antes isto era um FindFirstObjectByType a cada tecla E — uma varredura
        // da cena inteira por interacção. A referência vem do inspector, como em
        // todos os outros scripts deste projecto.
        if (inspectionSystem == null)
        {
            Debug.LogError(
                $"{name}: InspectionSystem por ligar no inspector — " +
                "examinar não faz nada.",
                this
            );

            return;
        }

        inspectionSystem.Inspect(gameObject, descricao);
    }

    public string GetInteractionText()
    {
        return "Examinar";
    }
}
