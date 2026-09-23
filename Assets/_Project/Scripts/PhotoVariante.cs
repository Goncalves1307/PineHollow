using System;
using UnityEngine;

// Uma versão da mesma fotografia depois de o passado ter sido mexido.
//
// É o que a subtask 869f4yb8z pede — «ao voltar do flashback, uma fotografia
// já vista mostra agora o protagonista atrás do Thomas» — e a regra central
// dos documentos posta em dados: não podes observar o passado sem deixar uma
// marca nele, e a marca aparece AQUI, numa imagem que o jogador já tinha visto
// de outra maneira.
//
// Não é uma segunda fotografia com outro id. É a mesma fotografia, com o mesmo
// id, e é por isso que continua a alinhar com as mesmas, que o álbum não a
// duplica e que o caderno a reconhece. O que muda é o que ela MOSTRA, e quem
// decide qual das versões vale é o EstadoDoMundo, não a fotografia.
//
// Os campos vazios herdam da base: uma variante que só troca a imagem não tem
// de repetir a data, o local e o verso — e repeti-los era arriscar que a
// variante e a base divergissem num sítio onde ninguém repara.
[Serializable]
public class PhotoVariante
{
    [Header("Quando vale")]
    // O par (stateId, chave) do EstadoDoMundo que tem de estar escrito, e o
    // valor que lá tem de estar. «true» por omissão, que é o que o flashback
    // escreve ao voltar.
    [SerializeField] private string stateId;
    [SerializeField] private string chave = "presente";
    [SerializeField] private string valor = "true";

    [Header("O que passa a mostrar")]
    [SerializeField] private Texture2D imagem;

    [TextArea(2, 5)]
    [SerializeField] private string frente;

    [TextArea(2, 5)]
    [SerializeField] private string verso;

    // Vazio herda da base. Não vazio SUBSTITUI a lista: o protagonista não tem
    // nome (canon), e uma fotografia onde ele aparece não ganha uma pessoa
    // nova no caderno — ganha a descoberta abaixo.
    [SerializeField] private string[] personagens;

    [Header("O que o caderno aprende")]
    // A frase que entra no caderno da primeira vez que o jogador vê esta
    // versão. Sem ela, a mudança passava só pela imagem, e o caderno ficava
    // com a descrição antiga — a registar como verdade uma fotografia que já
    // não é assim.
    [TextArea(2, 4)]
    [SerializeField] private string revelacao;

    public string StateId => stateId;
    public string Chave => chave;
    public string Valor => valor;
    public Texture2D Imagem => imagem;
    public string Frente => frente;
    public string Verso => verso;
    public string[] Personagens => personagens;
    public string Revelacao => revelacao;

    // O nome desta versão no caderno. O stateId e a chave dizem de onde ela
    // vem, e são estáveis — é o que o save da FASE 21 vai reencontrar.
    public string Id => stateId + ":" + chave + "=" + valor;

    public PhotoVariante()
    {
    }

    // Uma variante sem stateId nunca vale — não há par a ler —, e é melhor
    // assim do que valer sempre: seria uma fotografia que já nasce alterada.
    public bool ValeEm(EstadoDoMundo mundo)
    {
        if (mundo == null || string.IsNullOrWhiteSpace(stateId))
            return false;

        return mundo.Ler(stateId, chave) == valor;
    }

    internal PhotoVariante Copiar()
    {
        return new PhotoVariante
        {
            stateId = stateId,
            chave = chave,
            valor = valor,
            imagem = imagem,
            frente = frente,
            verso = verso,
            personagens = personagens,
            revelacao = revelacao
        };
    }
}
