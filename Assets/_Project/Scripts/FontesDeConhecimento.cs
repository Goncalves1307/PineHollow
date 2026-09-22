using UnityEngine;

// Como é que o que está no jogo vira uma linha no caderno.
//
// Vive num sítio só, e não espalhado por cada interactable, porque a regra de
// como se chama uma pessoa, uma data ou um local tem de ser a mesma em todo o
// lado: duas fotografias com «Thomas Hale» têm de dar a MESMA entrada, senão o
// caderno enche-se de duplicados e a cadeia de progressão nunca acende.
//
// É uma classe estática sem estado — as fontes chamam-na, ela escreve no
// registo, e é testável sem cena, sem input e sem play mode.
public static class FontesDeConhecimento
{
    // Uma fotografia que o jogador teve na mão. Dá mais do que uma linha: a
    // fotografia em si, o ano, o sítio e cada pessoa que lá aparece — que é
    // precisamente o que a checklist da task pede («pessoas, locais, datas,
    // fotografias»), e sai tudo dos campos que a FASE 5 já autorou.
    //
    // Devolve quantas linhas são novas.
    public static int RegistarFotografia(RegistoDeConhecimento registo, PhotoData fotografia)
    {
        if (registo == null || fotografia == null)
            return 0;

        int novas = 0;

        string origem = fotografia.Id;

        if (!string.IsNullOrWhiteSpace(fotografia.Id))
        {
            string titulo = string.IsNullOrWhiteSpace(fotografia.Data)
                ? fotografia.Id
                : fotografia.Data;

            // A frente é o que o jogo diz sobre a imagem; quando não há frente
            // autorada — e a de 1994 não tem — a legenda do viewer serve, e é
            // a mesma que o jogador já leu ao examinar a fotografia. Sem isto,
            // metade das fotografias entrava no caderno com o detalhe vazio.
            string descricao = string.IsNullOrWhiteSpace(fotografia.Frente)
                ? PhotoViewerSystem.Legendar(fotografia)
                : fotografia.Frente;

            if (registo.Registar(
                    Conhecimento.Identificar(CategoriaDeConhecimento.Fotografia, fotografia.Id),
                    CategoriaDeConhecimento.Fotografia,
                    titulo,
                    descricao,
                    origem))
            {
                novas++;
            }
        }

        // A data é indexada pelo ANO e não pela data por extenso: os anos são
        // canon e fechados (PhotoData.AnosCanonicos), e duas fotografias da
        // noite de 17 de Julho de 1986 são a mesma data na cabeça do jogador,
        // ainda que tenham horas diferentes escritas no verso.
        if (fotografia.Ano > 0)
        {
            string titulo = string.IsNullOrWhiteSpace(fotografia.Data)
                ? fotografia.Ano.ToString()
                : fotografia.Data;

            if (registo.Registar(
                    Conhecimento.Identificar(
                        CategoriaDeConhecimento.Data,
                        fotografia.Ano.ToString()),
                    CategoriaDeConhecimento.Data,
                    titulo,
                    null,
                    origem))
            {
                novas++;
            }
        }

        if (!string.IsNullOrWhiteSpace(fotografia.Local))
        {
            if (registo.Registar(
                    Conhecimento.Identificar(CategoriaDeConhecimento.Local, fotografia.Local),
                    CategoriaDeConhecimento.Local,
                    fotografia.Local,
                    null,
                    origem))
            {
                novas++;
            }
        }

        foreach (string pessoa in fotografia.Personagens)
        {
            if (string.IsNullOrWhiteSpace(pessoa))
                continue;

            if (registo.Registar(
                    Conhecimento.Identificar(CategoriaDeConhecimento.Pessoa, pessoa),
                    CategoriaDeConhecimento.Pessoa,
                    pessoa,
                    null,
                    origem))
            {
                novas++;
            }
        }

        return novas;
    }

    // Uma descoberta revelada num alinhamento.
    //
    // O par vai no texto porque é a única coisa que diz ao jogador de onde é que
    // aquilo saiu — sem isso, o caderno ficava com uma frase sem proveniência e
    // ele não tinha como voltar lá.
    public static bool RegistarDescoberta(
        RegistoDeConhecimento registo,
        PhotoData fotografia,
        PhotoDiscovery descoberta,
        PhotoData par)
    {
        if (registo == null || fotografia == null || descoberta == null)
            return false;

        string id = Conhecimento.Identificar(
            CategoriaDeConhecimento.Descoberta,
            fotografia.Id,
            descoberta.Id
        );

        string proveniencia = par == null
            ? "Vista em " + fotografia.Id + "."
            : "Revelada ao alinhar " + fotografia.Id + " com " + par.Id + ".";

        return registo.Registar(
            id,
            CategoriaDeConhecimento.Descoberta,
            descoberta.Descricao,
            proveniencia,
            fotografia.Id
        );
    }

    // Um documento lido. O stateId é a chave preferida porque é o que a FASE 7
    // usa para reconhecer o mesmo objecto nas duas épocas e o que a FASE 21
    // grava; o título só entra quando não há stateId, e aí fica escrito no log
    // que o caderno está a segurar-se numa string de conteúdo.
    public static bool RegistarDocumento(
        RegistoDeConhecimento registo,
        string stateId,
        string titulo,
        string corpo)
    {
        if (registo == null)
            return false;

        string chave = stateId;

        if (string.IsNullOrWhiteSpace(chave))
        {
            if (string.IsNullOrWhiteSpace(titulo))
                return false;

            chave = titulo;

            Debug.LogWarning(
                "Documento sem stateId: o caderno vai indexá-lo pelo título " +
                "(\"" + titulo + "\"). Mudar o título passa a criar uma " +
                "entrada nova em vez de actualizar a que lá está."
            );
        }

        return registo.Registar(
            Conhecimento.Identificar(CategoriaDeConhecimento.Documento, chave),
            CategoriaDeConhecimento.Documento,
            string.IsNullOrWhiteSpace(titulo) ? chave : titulo,
            corpo,
            stateId
        );
    }
}
