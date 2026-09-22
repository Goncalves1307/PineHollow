// Estados do player. A pilha de modos vive no PlayerStateMachine.
//
// Os três primeiros são locomoção: mutuamente exclusivos, definidos pelo
// PlayerController a cada frame a partir do input. Os restantes são modos
// que se empilham por cima — quem abre uma camada empurra, quem a fecha tira.
public enum PlayerState
{
    Normal,
    Sprinting,
    Crouching,

    // Previsto pelo plano (2.5) mas ainda sem transições: a interacção de hoje
    // é instantânea (premir E) e não abre camada nenhuma.
    Interacting,

    Inspecting,

    // Ler um documento é uma camada: abre, prende o player e fecha-se no Esc.
    // Não liberta o cursor — lê-se com os olhos, não com o rato.
    Reading,

    Photographing,
    PhotoPreview,
    ViewingAlbum,
    ViewingPhoto,

    // Duas fotografias lado a lado ou sobrepostas, com escala, posição e
    // rotação no rato. Abre por cima do álbum: o Esc fecha a comparação e
    // devolve ao álbum, não ao jogo.
    ComparingPhotos,

    // O caderno: o que o jogador já sabe, por gavetas. Ecrã de rato — a
    // progressão deste jogo é conhecimento e não XP, e é aqui que ela se vê.
    ViewingJournal,

    // O quadro de investigação: os cartões do que se sabe e as ligações que o
    // jogador faz entre eles. Também de rato, e também uma camada por cima do
    // jogo e não um HUD.
    ViewingBoard,

    // Estar em 1986. Entra-se por alinhar duas fotografias — o
    // SistemaDeFlashback consome o ConsumirAlinhamento() e empurra este modo —
    // e sai-se pelo evento que fecha o flashback.
    //
    // É uma camada que NÃO tranca o player: anda-se, olha-se e segue-se gente,
    // porque o GDD §14 diz que todos os flashbacks são jogáveis. É a única
    // camada da pilha de que isso é verdade, e por isso está escrita por nome
    // nos dois switches do PlayerStateMachine em vez de cair no default.
    Flashback
}
