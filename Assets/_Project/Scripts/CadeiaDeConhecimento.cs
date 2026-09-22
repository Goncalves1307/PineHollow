using System;
using System.Collections.Generic;
using UnityEngine;

// Um elo da cadeia de progressão: a frase, e o conhecimento que a acende.
[Serializable]
public class EloDaCadeia
{
    // Citado à letra do GDD §17. Placeholder tem o hábito de sobreviver, e esta
    // é a espinha da história — não se parafraseia.
    [SerializeField] private string texto;

    // O id de Conhecimento que faz este elo passar a sabido. Vazio nunca acende:
    // um elo por ligar fica por saber, que é o estado honesto de quem ainda não
    // tem conteúdo que o produza.
    [SerializeField] private string exigeId;

    public string Texto => texto;
    public string ExigeId => exigeId;

    public EloDaCadeia()
    {
    }

    public EloDaCadeia(string texto, string exigeId)
    {
        this.texto = texto;
        this.exigeId = exigeId;
    }
}

// A cadeia de progressão do GDD §17. «A progressão é baseada em conhecimento.
// Não existem níveis ou XP» — e é esta lista que o diz em código: o que o
// jogador pode fazer a seguir é função do que já sabe.
//
// É um ScriptableObject e não uma constante no código porque é conteúdo: a
// FASE 11 vai produzir as descobertas que acendem os elos do meio, e mexer
// numa frase de canon não devia obrigar a recompilar.
//
// Os elos por acender mostram-se na mesma, mas sem o texto. Mostrar a frase de
// um elo que ainda não se sabe era contar a história por antecipação; escondê-lo
// por completo tirava ao jogador a noção de que há caminho pela frente.
[CreateAssetMenu(
    fileName = "CadeiaDeConhecimento",
    menuName = "Pine Hollow/Cadeia de conhecimento"
)]
public class CadeiaDeConhecimento : ScriptableObject
{
    [SerializeField] private List<EloDaCadeia> elos = new List<EloDaCadeia>();

    public IReadOnlyList<EloDaCadeia> Elos =>
        (IReadOnlyList<EloDaCadeia>)elos ?? Array.Empty<EloDaCadeia>();

    public int Total => elos == null ? 0 : elos.Count;

    public bool Sabido(EloDaCadeia elo, RegistoDeConhecimento registo)
    {
        if (elo == null || registo == null)
            return false;

        return registo.Sabe(elo.ExigeId);
    }

    public int Sabidos(RegistoDeConhecimento registo)
    {
        if (elos == null || registo == null)
            return 0;

        int contados = 0;

        foreach (EloDaCadeia elo in elos)
        {
            if (Sabido(elo, registo))
                contados++;
        }

        return contados;
    }

    // Substitui os elos. Usado pelo Fase6Montagem para autorar a cadeia sem
    // abrir o inspector, e pelos testes.
    public void Definir(IEnumerable<EloDaCadeia> novos)
    {
        elos = new List<EloDaCadeia>();

        if (novos == null)
            return;

        foreach (EloDaCadeia elo in novos)
        {
            if (elo != null)
                elos.Add(elo);
        }
    }
}
