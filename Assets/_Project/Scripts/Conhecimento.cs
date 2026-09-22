using System;
using System.Text;
using UnityEngine;

// As seis gavetas do caderno, e não uma lista aberta — como o TipoDeDescoberta
// da FASE 5, e pela mesma razão: o quadro de investigação tem de saber agrupar,
// e um jogo cuja progressão é conhecimento precisa de saber o que é que sabe.
//
// Repare-se que NÃO é o TipoDeDescoberta. Aquele classifica o que se vê DENTRO
// de uma imagem (uma porta, uma sombra, uma pessoa naquela zona da fotografia);
// este classifica o que o jogador passou a saber. «Pessoa» ali é «vê-se alguém
// neste canto», aqui é «Thomas Hale». São eixos diferentes e juntá-los dava uma
// gaveta de Sombras no caderno.
public enum CategoriaDeConhecimento
{
    Pessoa,
    Local,
    Data,
    Fotografia,
    Documento,
    Descoberta
}

// Uma coisa que o jogador passou a saber.
//
// É uma classe serializável e não um ScriptableObject pela mesma razão que o
// PhotoData: isto nasce em runtime, do jogo a ser jogado, e escrever um asset
// por cada coisa aprendida era sujar o disco com estado de sessão. A FASE 21
// grava uma lista destes — este tipo É o formato do save da parte do
// conhecimento, e foi decidido assim de propósito.
//
// Não guarda quando foi registado: a ordem é a da lista no RegistoDeConhecimento,
// e um campo a duplicar o índice era uma segunda fonte de verdade para a mesma
// coisa.
[Serializable]
public class Conhecimento
{
    // Único no jogo inteiro e estável entre sessões — é por ele que a cadeia de
    // progressão pergunta «isto já se sabe?» e que o save reconhece o que já cá
    // estava. Construa-se sempre com Chave()/Identificar(), nunca à mão.
    [SerializeField] private string id;

    [SerializeField] private CategoriaDeConhecimento categoria;

    // O que aparece na lista do caderno.
    [SerializeField] private string titulo;

    [TextArea(2, 5)]
    [SerializeField] private string texto;

    // Onde é que isto foi aprendido — o id da fotografia, do documento. Serve
    // para o caderno poder dizer «visto em» sem ter de adivinhar, e para se
    // perceber, a ler um save, de onde veio cada linha.
    [SerializeField] private string origem;

    public string Id => id;
    public CategoriaDeConhecimento Categoria => categoria;
    public string Titulo => titulo;
    public string Texto => texto;
    public string Origem => origem;

    // Construtor sem argumentos para a serialização do Unity.
    public Conhecimento()
    {
    }

    public Conhecimento(
        string id,
        CategoriaDeConhecimento categoria,
        string titulo,
        string texto = null,
        string origem = null)
    {
        this.id = id;
        this.categoria = categoria;
        this.titulo = titulo;
        this.texto = texto;
        this.origem = origem;
    }

    // O id canónico de um conhecimento: a categoria em minúsculas, dois pontos,
    // e as partes que o identificam.
    //
    // As partes vão todas porque os ids das peças de baixo NÃO são únicos: o
    // PhotoDiscovery.Id é único dentro de uma fotografia e mais nada (hoje o
    // Fase5Montagem escreve "porta-lateral" em todas as que cria). Uma descoberta
    // é identificada pela fotografia mais o seu id, e não pelo seu id sozinho —
    // sem isto, a segunda fotografia com uma porta lateral silenciava a primeira.
    public static string Identificar(CategoriaDeConhecimento categoria, params string[] partes)
    {
        StringBuilder construido = new StringBuilder();

        construido.Append(categoria.ToString().ToLowerInvariant());

        if (partes != null)
        {
            foreach (string parte in partes)
            {
                string chave = Chave(parte);

                // Uma parte em branco NÃO se salta. Saltá-la encurtava o id em
                // silêncio, e duas coisas diferentes acabavam com o mesmo:
                // duas descobertas sem `id` na mesma fotografia davam ambas
                // `descoberta:<foto>`, a segunda era engolida pelo Registar, e
                // o jogador via o clarão a revelar duas e o caderno a mostrar
                // uma. Sem id, devolve vazio — e aí o Registar queixa-se alto.
                if (chave.Length == 0)
                    return string.Empty;

                construido.Append(':');
                construido.Append(chave);
            }
        }

        return construido.ToString();
    }

    // O nome de uma gaveta em pt-PT. No plural para a lombada do caderno, no
    // singular para um cartão do quadro — ali cada papel é uma coisa só.
    //
    // Vive aqui e não no separador do caderno porque o quadro precisa do mesmo
    // vocabulário: duas listas de nomes acabavam a divergir.
    public static string NomeDaCategoria(CategoriaDeConhecimento categoria, bool plural)
    {
        switch (categoria)
        {
            case CategoriaDeConhecimento.Pessoa:
                return plural ? "Pessoas" : "Pessoa";

            case CategoriaDeConhecimento.Local:
                return plural ? "Locais" : "Local";

            case CategoriaDeConhecimento.Data:
                return plural ? "Datas" : "Data";

            case CategoriaDeConhecimento.Fotografia:
                return plural ? "Fotografias" : "Fotografia";

            case CategoriaDeConhecimento.Documento:
                return plural ? "Documentos" : "Documento";

            case CategoriaDeConhecimento.Descoberta:
                return plural ? "Descobertas" : "Descoberta";

            default:
                return categoria.ToString();
        }
    }

    // Texto livre reduzido a chave: minúsculas, e tudo o que não for letra ou
    // algarismo vira hífen. Os acentos ficam — são letras, e tirá-los fazia
    // "Estúdio" e "Estudio" serem chaves diferentes consoante quem escreveu.
    public static string Chave(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        StringBuilder chave = new StringBuilder(texto.Length);

        bool hifenPendente = false;

        // Normalizado para a forma composta ANTES de percorrer. Os acentos
        // ficam de propósito — são letras —, mas em NFD vêm como caracteres
        // combinatórios separados, que não são letra nem algarismo e viravam
        // hífen: «Estúdio» colado de um documento dava `est-dio` e o mesmo nome
        // escrito no editor dava `estúdio`. Duas linhas no caderno para a mesma
        // coisa, e um elo da cadeia que nunca acendia.
        string normalizado = texto.Trim().Normalize(NormalizationForm.FormC);

        foreach (char c in normalizado.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                if (hifenPendente && chave.Length > 0)
                    chave.Append('-');

                hifenPendente = false;

                chave.Append(c);
            }
            else
            {
                hifenPendente = true;
            }
        }

        return chave.ToString();
    }
}
