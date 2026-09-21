using System;
using System.Collections.Generic;
using UnityEngine;

// A fotografia como objecto de dados, que é o que o GDD pede e o que a captura
// do protótipo nunca teve: uma Texture2D não tem data, nem local, nem verso, e
// a comparação e o alinhamento são operações sobre esses campos, não sobre
// pixéis.
//
// Dois tipos de fotografia circulam aqui, e a diferença é um campo e não dois
// caminhos de código:
//
//   - as ENCONTRADAS, autoradas no editor como PhotoAsset. Têm data, local,
//     pessoas e um verso escrito à mão. São estas que o vertical slice usa —
//     as fotografias do slice são objectos que o jogador acha, não que tira
//     (ESTADO.md, «O vertical slice não precisa da máquina fotográfica»).
//   - as TIRADAS pelo jogador, criadas em runtime por DeCaptura. Trazem a
//     imagem e o instante, e mais nada: não têm autor nem verso.
//
// É uma classe serializável e não um ScriptableObject de propósito. O asset
// autorado é o PhotoAsset, que embrulha um destes; assim a fotografia tirada
// em jogo não obriga a CreateInstance nem a escrever assets em disco, e a
// FASE 21 grava o mesmo tipo nos dois casos.
[Serializable]
public class PhotoData
{
    // Os anos em que há fotografias. Canon do GDD — 1986 é o núcleo. Quem
    // autorar um ano fora desta lista está a inventar, e o PhotoAsset avisa.
    public static readonly int[] AnosCanonicos =
    {
        1946, 1958, 1971, 1986, 1994, 2001, 2011, 2016, 2026
    };

    [Header("Identidade")]
    // Estável e escrito à mão nas autoradas: é por ele que o alinhamento
    // declara pares e que a FASE 21 sabe que fotografia já foi descoberta.
    [SerializeField] private string id;

    [Header("Data")]
    [SerializeField] private int ano;

    // A data por extenso, como aparece no verso ou na legenda. A noite de
    // 17 de Julho de 1986 tem horas exactas (23:31, 23:38, 23:42, 23:47,
    // 23:49, 23:52, 23:56, 00:03) e elas cabem aqui — o ano sozinho não as
    // guarda, e são elas que ordenam a sequência da noite.
    [SerializeField] private string data;

    [Header("Conteúdo")]
    [SerializeField] private string local;

    // Thomas Hale, Elias Ward, Ethan Cole. Quem aparece na imagem, não quem a
    // tirou — é a coincidência de pessoas entre décadas que faz a história.
    [SerializeField] private string[] personagens;

    // O que se vê de frente. A imagem é a frente; isto é o que o jogo diz
    // sobre ela quando se examina.
    [TextArea(2, 5)]
    [SerializeField] private string frente;

    // O que está escrito nas costas. Canon, citado à letra do
    // Pine_Hollow_Historia.md — «Ele ainda não chegou.», «Agora és tu.»,
    // «Não reveles todas.», «Thomas não é o único.», «É aqui que começa.»
    [TextArea(2, 5)]
    [SerializeField] private string verso;

    [SerializeField] private Texture2D imagem;

    [Header("Metadados")]
    // Pares livres (película, máquina, exposição, número de negativo). Lista e
    // não campos fixos porque não se sabe hoje o que a FASE 18 vai querer
    // escrever, e acrescentar um campo fixo mais tarde obriga a mexer em todas
    // as fotografias já autoradas.
    [SerializeField] private List<PhotoMetadata> metadados = new List<PhotoMetadata>();

    [Header("Alinhamento")]
    // Com que fotografias esta alinha, e em que posição. Declarado e não
    // adivinhado: é o que torna o alinhamento autorável sem o jogo ter de
    // comparar pixéis. O jogador continua a ter de o descobrir — o GDD proíbe
    // marcador a dizer «alinha estas duas».
    [SerializeField] private List<PhotoAlignment> alinhamentos = new List<PhotoAlignment>();

    [Header("Descobertas")]
    // O que há para encontrar nesta fotografia quando alinhada. Vazio é o
    // normal: a maior parte das fotografias não esconde nada.
    [SerializeField] private List<PhotoDiscovery> descobertas = new List<PhotoDiscovery>();

    // True nas encontradas, false nas tiradas pelo jogador. A distinção que a
    // task pede, e que decide se a fotografia tem verso para ler.
    [SerializeField] private bool autorada;

    public string Id => id;
    public int Ano => ano;
    public string Data => data;
    public string Local => local;
    public string Frente => frente;
    public string Verso => verso;
    public Texture2D Imagem => imagem;
    public bool Autorada => autorada;

    public IReadOnlyList<PhotoMetadata> Metadados => metadados;
    public IReadOnlyList<PhotoDiscovery> Descobertas => descobertas;

    // Nunca null, para quem itera não ter de se defender. Um array por ligar
    // no inspector chega aqui como null e não como vazio.
    public IReadOnlyList<string> Personagens =>
        personagens ?? Array.Empty<string>();

    public IReadOnlyList<PhotoAlignment> Alinhamentos =>
        (IReadOnlyList<PhotoAlignment>)alinhamentos ?? Array.Empty<PhotoAlignment>();

    // Uma fotografia sem nada escrito nas costas não se vira — não há segunda
    // face para mostrar, e virá-la mostrava um painel vazio.
    public bool TemVerso => !string.IsNullOrWhiteSpace(verso);

    // Construtor sem argumentos para a serialização do Unity. Não é para ser
    // chamado a partir do jogo: usa-se DeCaptura ou um PhotoAsset.
    public PhotoData()
    {
    }

    // A fotografia que o jogador tira. Sem autor, sem verso, sem alinhamentos:
    // é um registo do que ele viu, e é assim que se distingue no álbum.
    public static PhotoData DeCaptura(Texture2D imagem, int ano, string data)
    {
        return new PhotoData
        {
            id = "captura-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            imagem = imagem,
            ano = ano,
            data = data,
            autorada = false,
            personagens = Array.Empty<string>(),
            alinhamentos = new List<PhotoAlignment>(),
            metadados = new List<PhotoMetadata>(),
            descobertas = new List<PhotoDiscovery>()
        };
    }

    // O alinhamento a aplicar a `movida` quando é sobreposta a esta. Procura
    // nos dois sentidos: basta que uma das duas declare o par, para não
    // obrigar quem autora a escrevê-lo duas vezes e a arriscar escrevê-lo só
    // de um lado. Quando a declaração está do outro lado, o alvo vem
    // invertido — ler o mesmo alvo nos dois sentidos dava a posição errada.
    //
    // Devolve null quando as duas não alinham, que é o caso da esmagadora
    // maioria dos pares.
    public PhotoAlignment AlinhamentoPara(PhotoData movida)
    {
        if (movida == null || movida == this)
            return null;

        PhotoAlignment directo = Procurar(this, movida.id);

        if (directo != null)
            return directo;

        PhotoAlignment inverso = Procurar(movida, id);

        return inverso?.Inverter(movida.id);
    }

    public bool AlinhaCom(PhotoData outra)
    {
        return AlinhamentoPara(outra) != null;
    }

    private static PhotoAlignment Procurar(PhotoData origem, string destinoId)
    {
        // Sem id não há como lhe apontar, e um "" a bater com outro "" dava
        // alinhamentos entre todas as fotografias por preencher.
        if (origem.alinhamentos == null || string.IsNullOrEmpty(destinoId))
            return null;

        foreach (PhotoAlignment alinhamento in origem.alinhamentos)
        {
            if (alinhamento != null && alinhamento.ComId == destinoId)
                return alinhamento;
        }

        return null;
    }

    // A cópia que o PhotoAsset entrega ao jogo. Tem de ser cópia: as
    // descobertas guardam se já foram reveladas, e revelar uma escrevendo no
    // ScriptableObject sujava o asset em disco — dentro do editor a descoberta
    // aparecia já feita na sessão seguinte, e no build não persistia de todo.
    // Duas maneiras diferentes de estar errado a partir do mesmo código.
    //
    // O idPorOmissao é o nome do asset: quem autora não tem de escrever o id
    // duas vezes, e uma fotografia sem id não é referenciável pelo alinhamento.
    internal PhotoData CopiaAutorada(string idPorOmissao)
    {
        PhotoData copia = new PhotoData
        {
            id = string.IsNullOrWhiteSpace(id) ? idPorOmissao : id,
            ano = ano,
            data = data,
            local = local,
            frente = frente,
            verso = verso,
            imagem = imagem,
            autorada = true,
            personagens = personagens ?? Array.Empty<string>(),
            alinhamentos = new List<PhotoAlignment>(),
            metadados = new List<PhotoMetadata>(),
            descobertas = new List<PhotoDiscovery>()
        };

        if (alinhamentos != null)
        {
            foreach (PhotoAlignment alinhamento in alinhamentos)
            {
                if (alinhamento != null)
                    copia.alinhamentos.Add(alinhamento.Copiar());
            }
        }

        if (metadados != null)
        {
            foreach (PhotoMetadata entrada in metadados)
            {
                if (entrada != null)
                    copia.metadados.Add(new PhotoMetadata(entrada.Chave, entrada.Valor));
            }
        }

        if (descobertas != null)
        {
            foreach (PhotoDiscovery descoberta in descobertas)
            {
                if (descoberta != null)
                    copia.descobertas.Add(descoberta.Copiar());
            }
        }

        return copia;
    }
}

// Um par chave/valor dos metadados. Classe própria e não Dictionary porque o
// Unity não serializa dicionários, e isto tem de aparecer no inspector para
// quem autora as fotografias.
[Serializable]
public class PhotoMetadata
{
    [SerializeField] private string chave;
    [SerializeField] private string valor;

    public string Chave => chave;
    public string Valor => valor;

    public PhotoMetadata()
    {
    }

    public PhotoMetadata(string chave, string valor)
    {
        this.chave = chave;
        this.valor = valor;
    }
}
