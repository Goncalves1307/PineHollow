using UnityEngine;

// Objecto que se pega e se examina à frente da câmara. Quem faz o trabalho é o
// InspectionSystem; este componente só diz qual é o objecto e o verbo.
public class InspectionInteractable : MonoBehaviour, IInteractable
{
    [Header("Sistema")]
    [SerializeField] private InspectionSystem inspectionSystem;

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

        inspectionSystem.Inspect(gameObject);
    }

    public string GetInteractionText()
    {
        return "Examinar";
    }
}
