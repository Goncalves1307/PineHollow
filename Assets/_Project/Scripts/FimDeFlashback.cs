using UnityEngine;

// O que faz um flashback acabar.
//
// Classe base e não um bool no SistemaDeFlashback porque os sete flashbacks do
// GDD não acabam todos da mesma maneira: «The Photograph» acaba quando o Thomas
// sai de vista, «The Factory» quando ele entra na zona subterrânea, «23:47»
// quando o jogador vê o que não devia ter visto. O condutor pergunta
// Terminou() e não quer saber de qual é.
//
// Abstracta e MonoBehaviour para poder ser ligada no inspector: o Unity não
// serializa interfaces, e um [SerializeField] de um IFimDeFlashback chegava
// sempre nulo — o tipo de armadilha que não dá erro, dá um flashback que nunca
// acaba.
public abstract class FimDeFlashback : MonoBehaviour
{
    public abstract bool Terminou();

    // Chamado quando o flashback recomeça, para que a condição possa ser
    // cumprida outra vez. Sem isto, rever um flashback acabava-o no primeiro
    // frame — a condição já estava satisfeita da vez anterior.
    public virtual void Recomecar()
    {
    }
}
