using System;
using UnityEngine;

// O que há para encontrar dentro de uma fotografia. São as sete categorias da
// checklist da FASE 5, e não uma lista aberta, porque o jogo tem de saber
// agrupá-las no quadro de investigação da FASE 6.
public enum TipoDeDescoberta
{
    Estrutura,
    Pessoa,
    Porta,
    Sombra,
    Objecto,
    Simbolo,

    // A que carrega a história: uma parede que noutra época não estava lá.
    // «Não podes observar o passado sem deixar uma marca nele.»
    AlteracaoArquitectonica
}

// Uma descoberta concreta numa fotografia: o quê, onde na imagem, e se o
// jogador já lá chegou.
//
// O `revelada` é estado de jogo e não de autoria — por isso o PhotoAsset
// entrega sempre uma cópia do seu PhotoData. Sem isso, descobrir uma coisa a
// meio de uma sessão escrevia no asset em disco e a descoberta aparecia já
// feita na sessão seguinte, dentro do editor.
[Serializable]
public class PhotoDiscovery
{
    [SerializeField] private string id;
    [SerializeField] private TipoDeDescoberta tipo;

    [TextArea(2, 4)]
    [SerializeField] private string descricao;

    // Onde está na imagem, em coordenadas normalizadas (0–1 a contar do canto
    // inferior esquerdo). Normalizadas e não em pixéis porque a mesma
    // fotografia é desenhada em miniatura, no viewer e na comparação, em três
    // tamanhos diferentes.
    [SerializeField] private Vector2 posicaoNaImagem;

    // Só se revela quando esta fotografia está alinhada com a outra. Vazio
    // significa que basta examinar a fotografia com atenção.
    [SerializeField] private string revelaAoAlinharCom;

    [SerializeField] private bool revelada;

    public string Id => id;
    public TipoDeDescoberta Tipo => tipo;
    public string Descricao => descricao;
    public Vector2 PosicaoNaImagem => posicaoNaImagem;
    public string RevelaAoAlinharCom => revelaAoAlinharCom;
    public bool Revelada => revelada;

    public PhotoDiscovery()
    {
    }

    private PhotoDiscovery(PhotoDiscovery outra)
    {
        id = outra.id;
        tipo = outra.tipo;
        descricao = outra.descricao;
        posicaoNaImagem = outra.posicaoNaImagem;
        revelaAoAlinharCom = outra.revelaAoAlinharCom;

        // A cópia nasce por revelar, mesmo que o asset tenha ficado com a
        // caixa marcada de uma corrida anterior no editor.
        revelada = false;
    }

    public PhotoDiscovery Copiar()
    {
        return new PhotoDiscovery(this);
    }

    // Esta descoberta sai deste alinhamento? Sem par declarado, sai de
    // examinar a fotografia e não de alinhar coisa nenhuma.
    public bool SaiDoAlinhamentoCom(PhotoData outra)
    {
        if (string.IsNullOrWhiteSpace(revelaAoAlinharCom))
            return false;

        return outra != null && outra.Id == revelaAoAlinharCom;
    }

    // Devolve true só na primeira vez. Quem chama usa-o para decidir se avisa
    // o jogador — avisar duas vezes da mesma descoberta é ruído.
    public bool Revelar()
    {
        if (revelada)
            return false;

        revelada = true;

        return true;
    }
}
