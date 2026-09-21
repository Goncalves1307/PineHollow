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

    // Previsto pelo plano (2.5) mas inalcançável até à FASE 7: o world state
    // 2026/1986 ainda não existe, e é ele que faz entrar e sair daqui.
    Flashback
}
