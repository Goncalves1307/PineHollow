using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Uma linha da gaveta aberta. Clicar mostra o que se sabe sobre ela.
public class EntradaDoJournal : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text rotulo;

    private JournalSystem sistema;

    public Conhecimento Conhecimento { get; private set; }

    public string Rotulo => rotulo != null ? rotulo.text : string.Empty;

    public void Configurar(
        JournalSystem sistema,
        Conhecimento conhecimento,
        Color cor)
    {
        this.sistema = sistema;

        Conhecimento = conhecimento;

        if (rotulo == null)
            rotulo = GetComponentInChildren<TMP_Text>(true);

        if (rotulo == null)
            return;

        rotulo.text = conhecimento != null ? conhecimento.Titulo : string.Empty;
        rotulo.color = cor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (sistema != null)
            sistema.Seleccionar(Conhecimento);
    }
}
