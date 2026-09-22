using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Um papel no quadro: uma coisa que o jogador sabe.
//
// Arrasta-se com o botão esquerdo para arrumar, e marca-se com o direito — dois
// marcados fazem uma ligação. O direito é o mesmo gesto do álbum, onde marcar
// duas miniaturas abre a comparação; o jogo já ensinou este gesto uma vez.
//
// Como o PhotoThumbnail, isto só lê o rato e entrega: quem decide é o
// InvestigationBoardSystem, e é lá que estão os métodos públicos sem input que
// os testes chamam.
public class CartaoDoQuadro : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IPointerClickHandler
{
    [SerializeField] private Image fundo;
    [SerializeField] private TMP_Text rotulo;

    private InvestigationBoardSystem sistema;

    private Vector2 agarre;

    public string Id { get; private set; }

    public Conhecimento Conhecimento { get; private set; }

    public Vector2 Posicao =>
        transform is RectTransform rect ? rect.anchoredPosition : Vector2.zero;

    public string Rotulo => rotulo != null ? rotulo.text : string.Empty;

    public void Configurar(
        InvestigationBoardSystem sistema,
        Conhecimento conhecimento,
        Color cor)
    {
        this.sistema = sistema;

        Conhecimento = conhecimento;

        Id = conhecimento != null ? conhecimento.Id : null;

        if (fundo == null)
            fundo = GetComponent<Image>();

        if (rotulo == null)
            rotulo = GetComponentInChildren<TMP_Text>(true);

        if (rotulo != null)
            rotulo.text = Etiqueta(conhecimento);

        Pintar(cor);
    }

    // A categoria em cima, o título em baixo.
    //
    // Sem a categoria, dois papéis ficavam iguais: a fotografia de 1986 e a
    // data de 1986 chamam-se ambas «17 de Julho de 1986», e no caderno
    // distinguem-se pela gaveta em que estão — o quadro mistura as seis e não
    // tem esse contexto. Quem ligasse um deles não sabia qual tinha ligado.
    public static string Etiqueta(Conhecimento conhecimento)
    {
        if (conhecimento == null)
            return string.Empty;

        string categoria = Conhecimento
            .NomeDaCategoria(conhecimento.Categoria, false)
            .ToUpperInvariant();

        return "<size=62%>" + categoria + "</size>\n" + conhecimento.Titulo;
    }

    public void Pintar(Color cor)
    {
        if (fundo != null)
            fundo.color = cor;
    }

    public void Pousar(Vector2 posicao)
    {
        if (transform is RectTransform rect)
            rect.anchoredPosition = posicao;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Guarda a distância entre o sítio onde se pegou e o centro do cartão.
        // Sem isto, o papel saltava para debaixo do cursor no primeiro pixel de
        // arrasto em vez de acompanhar a mão.
        if (PontoNoQuadro(eventData, out Vector2 ponto))
            agarre = Posicao - ponto;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (sistema == null)
            return;

        if (PontoNoQuadro(eventData, out Vector2 ponto))
            sistema.Mover(Id, ponto + agarre);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (sistema != null)
            sistema.AlternarMarcacao(Id);
    }

    // As posições dos cartões são medidas dentro da superfície do quadro e não
    // no ecrã — um quadro noutra resolução tem de manter os papéis nos mesmos
    // sítios uns em relação aos outros.
    private bool PontoNoQuadro(PointerEventData eventData, out Vector2 ponto)
    {
        ponto = Vector2.zero;

        if (!(transform.parent is RectTransform quadro))
            return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            quadro,
            eventData.position,
            eventData.pressEventCamera,
            out ponto
        );
    }
}
