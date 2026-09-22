using System;
using UnityEngine;

// Uma ligação que o jogador fez entre duas coisas que sabe.
//
// Repare-se no que NÃO tem: não tem «certa», não tem «válida», não tem peso nem
// confiança. É a regra dura do GDD — o quadro ajuda a organizar, não conclui —
// escrita no tipo de dados e não numa intenção. Não há onde guardar um
// veredicto, e por isso não há como o jogo dar um.
//
// Não tem direcção: ligar A a B é o mesmo que ligar B a A. Uma seta dizia ao
// jogador qual das duas causa a outra, que é precisamente a conclusão que o
// quadro não pode tirar por ele.
[Serializable]
public class LigacaoDoQuadro
{
    [SerializeField] private string idA;
    [SerializeField] private string idB;

    public string IdA => idA;
    public string IdB => idB;

    public LigacaoDoQuadro()
    {
    }

    public LigacaoDoQuadro(string idA, string idB)
    {
        this.idA = idA;
        this.idB = idB;
    }

    public bool Une(string a, string b)
    {
        return (idA == a && idB == b) || (idA == b && idB == a);
    }

    public bool Toca(string id)
    {
        return idA == id || idB == id;
    }
}

// Onde é que o jogador pousou um cartão. Guardado à parte do Conhecimento: onde
// está um papel no quadro não é uma coisa que se saiba sobre a história, e não
// tem nada que fazer no save do conhecimento.
[Serializable]
public class PosicaoDeCartao
{
    [SerializeField] private string id;
    [SerializeField] private Vector2 posicao;

    public string Id => id;
    public Vector2 Posicao => posicao;

    public PosicaoDeCartao()
    {
    }

    public PosicaoDeCartao(string id, Vector2 posicao)
    {
        this.id = id;
        this.posicao = posicao;
    }

    public void Pousar(Vector2 nova)
    {
        posicao = nova;
    }
}
