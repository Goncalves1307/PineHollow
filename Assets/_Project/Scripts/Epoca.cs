// As duas épocas de Pine Hollow.
//
// Duas e não uma lista aberta, apesar de o cânone ter nove anos com fotografias
// (1946, 1958, 1971, 1986, 1994, 2001, 2011, 2016, 2026). Os outros sete são
// DATAS DE FOTOGRAFIAS, não sítios onde se joga: o jogador vê-os na imagem e
// aprende-os no caderno, nunca lá põe os pés. Jogam-se dois — o presente e a
// noite de 17 de Julho de 1986 — e é isso que este enum diz.
//
// A restrição que manda aqui é a da direcção artística §5: a GEOMETRIA NUNCA
// MUDA entre épocas. Só muda a luz, o grading, três a cinco superfícies por
// divisão e até dez props-marcador. Não é orçamento — o jogador alinha uma
// fotografia de 1986 com uma divisão de 2026, e se a parede mudar de sítio não
// há alinhamento nenhum, há um puzzle impossível. É por isso que as duas épocas
// são dois estados da MESMA cena e não duas cenas: duas cenas divergem à
// primeira vez que alguém arrasta uma parede num editor e não na outra.
public enum Epoca
{
    Presente2026,
    Passado1986
}
