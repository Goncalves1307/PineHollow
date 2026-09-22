using UnityEngine;

// Uma fotografia pousada no mundo: apanha-se, examina-se, vira-se para ler o
// que está escrito atrás, e fica guardada no álbum.
//
// É o interactable que o vertical slice usa. O GDD e a FASE 8 listam
// «1.ª e 2.ª fotografia» e «inspeção» sem nunca listarem «tirar fotografia» —
// as fotografias do slice são objectos que o jogador encontra, não que tira.
//
// Não faz o trabalho: entrega-o ao InspectionSystem, como o
// InspectionInteractable já fazia. O que acrescenta é o verso e o álbum.
public class PhotoInteractable : MonoBehaviour, IInteractable
{
    [Header("Sistemas")]
    [SerializeField] private InspectionSystem inspectionSystem;
    [SerializeField] private PhotographySystem photographySystem;

    [Header("Conteúdo")]
    // A fotografia autorada. Vem de um asset e não de campos aqui para que a
    // mesma fotografia possa aparecer em mais do que um sítio e para que a
    // FASE 18 a produza sem abrir a cena.
    [SerializeField] private PhotoAsset fotografia;

    [Header("Estado")]
    // Todos os outros interactables têm um. A FASE 7 precisa dele para saber
    // que objecto é este nas duas eras, e a FASE 21 para o gravar.
    [SerializeField] private string stateId;

    public string StateId => stateId;

    // Fica a true depois de apanhada. Legível de fora porque é isso que a
    // FASE 21 grava — que fotografias é que o jogador já descobriu.
    public bool Apanhada { get; private set; }

    public void Interact()
    {
        if (inspectionSystem == null || fotografia == null)
        {
            Debug.LogError(
                $"{name}: InspectionSystem ou fotografia por ligar no " +
                "inspector — examinar não faz nada.",
                this
            );

            return;
        }

        PhotoData dados = fotografia.Criar();

        // O álbum primeiro, o exame depois. Ao contrário, sair da inspecção
        // sem que o álbum tivesse recebido nada deixava a fotografia por
        // descobrir apesar de o jogador a ter tido na mão.
        Guardar(dados);

        // A frente é o que se vê, o verso é o que lá está escrito. O
        // InspectionSystem trata do resto — reparenta, tranca o player, roda,
        // faz zoom e devolve tudo ao sítio no Esc.
        inspectionSystem.Inspect(gameObject, dados.Frente, dados.Verso);
    }

    private void Guardar(PhotoData dados)
    {
        if (photographySystem == null)
        {
            // Não é motivo para não deixar examinar: vê-se a fotografia, só
            // não fica no álbum. Mas não pode ficar calado, que é o género de
            // falha que só dá pelo Tab meia hora depois.
            Debug.LogError(
                $"{name}: PhotographySystem por ligar no inspector — " +
                "a fotografia não vai para o álbum.",
                this
            );

            return;
        }

        photographySystem.AddPhoto(dados);

        // O que o jogador aprendeu por a ter tido na mão: a fotografia, o ano,
        // o sítio e quem lá está. Vai para o registo e não fica só no álbum —
        // o álbum é uma pilha de imagens, o caderno é o que ele sabe.
        FontesDeConhecimento.RegistarFotografia(
            RegistoDeConhecimento.Instancia,
            dados
        );

        Apanhada = true;
    }

    public string GetInteractionText()
    {
        // Só o verbo: o [E] é posto pelo InteractionSystem. E é «Examinar»
        // como em qualquer outro objecto — dizer «Apanhar fotografia» aqui
        // contava ao jogador o que ele devia descobrir ao chegar perto.
        return "Examinar";
    }
}
