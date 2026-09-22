// O quanto o passado dá pelo jogador.
//
// São os quatro degraus que a task da FASE 7 lista como «evolução ao longo do
// jogo», e estão aqui por ordem porque a ordem É a progressão: cada flashback
// sobe um degrau e a subida é o jogo. O GDD diz que os primeiros flashbacks são
// observacionais e que isso muda mais tarde — este enum é essa mudança escrita
// de uma vez, para que nenhum flashback futuro tenha de inventar a sua.
//
// Nesta fase só o primeiro degrau tem conteúdo autorado a usá-lo: o Thomas
// junto ao lago não dá pelo jogador, porque «o jogador percebe que está no
// passado» (§15.1) e mais nada. Os outros três estão implementados e testados,
// à espera dos flashbacks que os hão-de pedir.
public enum NivelDeConsciencia
{
    // Não dá por nada. O jogador é uma câmara com pernas.
    Inconsciente,

    // Reage à presença sem a compreender: pára, olha na direcção, hesita. Não
    // sabe que está a ser visto — sente uma corrente de ar.
    Reage,

    // Vê o jogador e sustenta o olhar. É o degrau que muda o jogo de
    // «assisto ao passado» para «o passado sabe que estou cá».
    Ve,

    // O que faz fica no mundo: a acção desta personagem escreve no
    // EstadoDoMundo e o presente encontra-a lá quando voltar.
    MarcaOPresente
}
