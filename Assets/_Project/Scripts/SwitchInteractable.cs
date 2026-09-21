using UnityEngine;

// Interruptor. Comuta as luzes que lhe estiverem ligadas e roda a alavanca.
// Não mexe em pós-processamento nem em luz global: a direcção artística separa
// as épocas pela temperatura das luzes praticáveis (Direcção Artística §7.3),
// e isso é trabalho da FASE 7, não deste script.
public class SwitchInteractable : MonoBehaviour, IInteractable
{
    [Header("Luzes")]
    [SerializeField] private Light[] controlledLights;

    [Header("Alavanca")]
    [SerializeField] private Transform toggleBody;
    [SerializeField] private float toggleAngle = 20f;

    [Header("Estado")]
    [SerializeField] private string stateId;
    [SerializeField] private bool startsOn;

    private bool isOn;

    private Quaternion offRotation;
    private Quaternion onRotation;

    public string StateId => stateId;
    public bool IsOn => isOn;

    private void Start()
    {
        isOn = startsOn;

        if (toggleBody != null)
        {
            // A pose autorada é a do estado inicial, não a de "apagado":
            // com startsOn, um interruptor colocado visualmente em "ligado"
            // saltava mais toggleAngle no primeiro frame.
            Quaternion autorada = toggleBody.localRotation;
            Quaternion deslocada =
                autorada * Quaternion.Euler(toggleAngle, 0f, 0f);

            offRotation = isOn ? deslocada : autorada;
            onRotation = isOn ? autorada : deslocada;
        }

        ApplyState();
    }

    public void Interact()
    {
        isOn = !isOn;
        ApplyState();
    }

    // Instantâneo e não interpolado: um interruptor não tem meio caminho, e o
    // estado das luzes tem de bater certo com a alavanca no mesmo frame.
    private void ApplyState()
    {
        if (controlledLights != null)
        {
            for (int i = 0; i < controlledLights.Length; i++)
            {
                if (controlledLights[i] != null)
                    controlledLights[i].enabled = isOn;
            }
        }

        if (toggleBody != null)
            toggleBody.localRotation = isOn ? onRotation : offRotation;
    }

    public string GetInteractionText()
    {
        return isOn ? "Apagar" : "Acender";
    }
}
