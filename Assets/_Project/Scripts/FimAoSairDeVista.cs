using UnityEngine;

// Acaba o flashback quando a personagem chega ao fim do seu caminho.
//
// É o fim de «The Photograph»: o Thomas anda junto ao lago, o jogador segue-o
// se quiser, e quando ele sai de vista 1986 acaba. O GDD pede flashbacks
// «relativamente curtos e significativos» — quem decide a duração é o percurso
// da personagem, não um cronómetro, porque um cronómetro cortava a meio de o
// jogador estar a olhar para alguma coisa.
public class FimAoSairDeVista : FimDeFlashback
{
    [SerializeField] private PersonagemHistorica personagem;

    public override bool Terminou()
    {
        if (personagem == null)
        {
            // Sem personagem ligada isto nunca devolvia true e o flashback
            // nunca acabava — e do modo Flashback não se sai pelo Esc.
            Debug.LogError(
                "FimAoSairDeVista em '" + name + "' não tem personagem ligada: " +
                "o flashback acabaria já, para não trancar o jogador.",
                this
            );

            return true;
        }

        return personagem.SaiuDeVista;
    }

    public override void Recomecar()
    {
        if (personagem != null)
            personagem.Recomecar();
    }
}
