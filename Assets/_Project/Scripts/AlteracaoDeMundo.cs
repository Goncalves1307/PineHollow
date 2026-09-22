using System;
using UnityEngine;

// Uma coisa que ficou diferente porque alguém lhe mexeu.
//
// É o «sítio único onde uma alteração feita no passado fica registada para o
// presente a ler» que a subtask 869f4yb88 pede, reduzido à sua peça mais
// pequena. Uma destas por par (objecto, propriedade).
//
// Classe serializável e não ScriptableObject, pela mesma razão que o
// Conhecimento e o PhotoData: isto nasce de o jogo ser jogado, e escrever um
// asset por cada porta que se abre era sujar o disco com estado de sessão. A
// FASE 21 grava uma lista destes — este tipo É o formato do save da parte do
// mundo, e está escrito assim de propósito.
//
// O valor é uma string e não um bool, apesar de hoje só haver portas abertas e
// fechadas. Um bool obrigava a um segundo tipo à primeira vez que alguma coisa
// precisasse de guardar QUAL — qual a fotografia que está na ranhura, qual o
// lado para onde o interruptor ficou. A conversão está aqui dentro e em mais
// lado nenhum, para que só haja uma forma de escrever «true».
[Serializable]
public class AlteracaoDeMundo
{
    // Quem mudou. É o stateId do interactable, não o nome do GameObject — o
    // nome muda com o cenário e o stateId foi autorado para durar.
    [SerializeField] private string stateId;

    // O que nele mudou. Um objecto pode ter mais do que uma propriedade com
    // estado, e sem isto a segunda esmagava a primeira.
    [SerializeField] private string chave;

    [SerializeField] private string valor;

    public string StateId => stateId;
    public string Chave => chave;
    public string Valor => valor;

    // Construtor sem argumentos para a serialização do Unity.
    public AlteracaoDeMundo()
    {
    }

    public AlteracaoDeMundo(string stateId, string chave, string valor)
    {
        this.stateId = stateId;
        this.chave = chave;
        this.valor = valor;
    }

    public void Actualizar(string novoValor)
    {
        valor = novoValor;
    }

    public bool Corresponde(string stateId, string chave)
    {
        return this.stateId == stateId && this.chave == chave;
    }

    // Invariante, e não o ToString() da cultura corrente: em pt-PT o bool não
    // muda, mas o dia em que isto guardar um número, um save escrito numa
    // máquina com vírgula decimal não abre noutra com ponto. Custa nada agora
    // e é impossível de corrigir depois de haver saves lá fora.
    public static string Escrever(bool valor)
    {
        return valor ? "true" : "false";
    }

    public static bool Ler(string valor, bool omissao = false)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return omissao;

        return valor == "true";
    }
}
