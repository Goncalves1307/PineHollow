using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Uma gaveta do caderno, na linha de cima. Clicar troca a gaveta mostrada.
//
// Não faz o trabalho: diz ao JournalSystem qual foi clicada, como o
// PhotoThumbnail faz com o álbum. O input vive aqui e a decisão vive lá — é a
// regra que mantém o que decide testável sem rato.
public class SeparadorDoJournal : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text rotulo;

    private JournalSystem sistema;

    public CategoriaDeConhecimento Categoria { get; private set; }

    public string Rotulo => rotulo != null ? rotulo.text : string.Empty;

    public void Configurar(
        JournalSystem sistema,
        CategoriaDeConhecimento categoria,
        Color cor)
    {
        this.sistema = sistema;

        Categoria = categoria;

        if (rotulo == null)
            rotulo = GetComponentInChildren<TMP_Text>(true);

        if (rotulo == null)
            return;

        rotulo.text = Nome(categoria);
        rotulo.color = cor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (sistema != null)
            sistema.Mostrar(Categoria);
    }

    // Em pt-PT e no plural, que é como se lê a lombada de uma gaveta.
    public static string Nome(CategoriaDeConhecimento categoria)
    {
        return Conhecimento.NomeDaCategoria(categoria, true);
    }
}
