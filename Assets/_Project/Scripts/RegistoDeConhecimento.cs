using System.Collections.Generic;
using UnityEngine;

// Tudo o que o jogador já sabe. É o andar de baixo da FASE 6: o caderno e o
// quadro não guardam nada, só mostram isto.
//
// Vive no objecto do Bootstrap e sobrevive às trocas de cena, porque é isso que
// a FASE 7 vai fazer — a cena de 1986 e a de 2026 trocam por baixo e o que o
// jogador sabe não se perde ao atravessar um flashback.
//
// Por isso é alcançado por Instancia e não por [SerializeField], que é a regra
// desta casa: o Unity NÃO serializa referências entre cenas, e um componente do
// Prototype_Player não consegue apontar para um objecto que vive no Bootstrap e
// é DontDestroyOnLoad. Não é preguiça de ligar no inspector — é a única forma
// que existe.
//
// A Instancia cria o registo se não houver nenhum. Sem isso, entrar em play mode
// directamente no Prototype_Player (que é como se testa este projecto, e como o
// Fase5Capturas corre) dava um caderno sempre vazio com o código todo escrito —
// exactamente a armadilha que a FASE 5 já apanhou duas vezes.
public class RegistoDeConhecimento : MonoBehaviour
{
    // Lista e não dicionário: o Unity não serializa dicionários, a ordem da
    // lista é a ordem por que se aprendeu (que é a progressão), e a FASE 21
    // grava isto tal e qual.
    [SerializeField] private List<Conhecimento> conhecidos = new List<Conhecimento>();

    private static RegistoDeConhecimento instancia;

    // True no que a Instancia criou de emergência, false no que alguém pôs numa
    // cena. A distinção existe porque os dois não valem o mesmo: ver abaixo.
    private bool automatico;

    public bool Automatico => automatico;

    public static RegistoDeConhecimento Instancia
    {
        get
        {
            if (instancia != null && !instancia.automatico)
                return instancia;

            // Um registo POSTO À MÃO ganha sempre a um criado de emergência, e
            // o de emergência sai da frente quando o verdadeiro aparece.
            //
            // Sem esta regra, quem chegasse primeiro ficava com o lugar: entrar
            // no Prototype_Player criava um registo automático, e o do
            // Bootstrap — o que sobrevive às trocas de cena e o que a FASE 21
            // grava — ficava a apanhar pó enquanto o jogo escrevia no outro.
            RegistoDeConhecimento autorado = Autorado();

            if (autorado != null)
            {
                // Delega em Assumir() e não repete a regra aqui. Repetida, as
                // duas cópias divergiram: esta destruía o registo de emergência
                // sem lhe levar o conhecimento, e o jogador perdia em silêncio
                // o que tinha aprendido antes da troca — as descobertas já
                // reveladas nem sequer voltam a entrar, porque o
                // PhotoDiscovery.Revelar() só devolve true à primeira.
                autorado.Assumir();

                return instancia;
            }

            if (instancia != null)
                return instancia;

            GameObject anfitriao = new GameObject("RegistoDeConhecimento");

            instancia = anfitriao.AddComponent<RegistoDeConhecimento>();
            instancia.automatico = true;

            return instancia;
        }
    }

    private static RegistoDeConhecimento Autorado()
    {
        // Sem o FindObjectsSortMode: a sobrecarga que o leva está obsoleta no
        // Unity 6 e este projecto compila com zero avisos — com o log cheio
        // deles, ninguém dá pelo próximo.
        RegistoDeConhecimento[] todos = FindObjectsByType<RegistoDeConhecimento>(
            FindObjectsInactive.Include
        );

        foreach (RegistoDeConhecimento candidato in todos)
        {
            if (candidato != null && !candidato.automatico)
                return candidato;
        }

        return null;
    }

    public IReadOnlyList<Conhecimento> Conhecidos => conhecidos;

    public int Total => conhecidos.Count;

    private void Awake()
    {
        Assumir();
    }

    // Decide qual dos registos fica de pé quando aparece um segundo.
    //
    // Público porque em EditMode o Awake não corre, e esta regra não pode ficar
    // sem teste: escrita dentro do Awake, ela estava INVERTIDA — um registo
    // autorado a acordar com um automático já de pé destruía-se a si próprio e
    // deixava o de emergência no lugar, ao contrário do que a Instancia promete
    // três linhas acima. Foi um revisor externo que deu por isso.
    public void Assumir()
    {
        if (instancia != null && instancia != this)
        {
            if (instancia.automatico && !automatico)
            {
                // O autorado toma o lugar do de emergência e leva com ele o que
                // o outro já tinha aprendido — a troca não pode custar ao
                // jogador o que ele descobriu antes dela.
                foreach (Conhecimento aprendido in instancia.conhecidos)
                    Registar(aprendido);

                Destruir(instancia.gameObject);
            }
            else
            {
                // Dois registos é o conhecimento partido ao meio: metade das
                // fontes escreve num, o caderno lê o outro, e ninguém percebe
                // porque é que faltam linhas.
                Destruir(gameObject);

                return;
            }
        }

        instancia = this;

        // DontDestroyOnLoad só aceita objectos de raiz, e só existe em play
        // mode — fora dele queixa-se no log. Um registo pendurado noutra coisa
        // (como nos testes) não sobrevive à troca de cena, e isso está certo:
        // quem o quer permanente põe-no no Bootstrap.
        if (Application.isPlaying && transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instancia == this)
            instancia = null;
    }

    // Esquece a instância estática, e leva com ela a que tiver sido criada de
    // emergência. Público para os testes: sem isto, um teste herdava o registo
    // do anterior e passava por razões que não são as suas — e os automáticos
    // iam-se acumulando na cena de teste até o Autorado() ter de os varrer a
    // todos.
    public static void Esquecer()
    {
        if (instancia != null && instancia.automatico)
            Destruir(instancia.gameObject);

        instancia = null;
    }

    private static void Destruir(GameObject alvo)
    {
        if (Application.isPlaying)
            Destroy(alvo);
        else
            DestroyImmediate(alvo);
    }

    public bool Sabe(string id)
    {
        return Obter(id) != null;
    }

    public Conhecimento Obter(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        foreach (Conhecimento conhecimento in conhecidos)
        {
            if (conhecimento != null && conhecimento.Id == id)
                return conhecimento;
        }

        return null;
    }

    // Devolve true só na primeira vez, à maneira do PhotoDiscovery.Revelar():
    // quem chama usa-o para decidir se avisa o jogador, e aprender duas vezes a
    // mesma coisa não é aprender.
    public bool Registar(Conhecimento conhecimento)
    {
        if (conhecimento == null)
            return false;

        if (string.IsNullOrWhiteSpace(conhecimento.Id))
        {
            // Sem id não há como perguntar por ele depois: nem a cadeia de
            // progressão o encontra, nem o save o reconhece na sessão seguinte.
            Debug.LogError(
                "Conhecimento sem id não se regista — seria uma linha no " +
                "caderno que nada consegue voltar a encontrar."
            );

            return false;
        }

        if (Sabe(conhecimento.Id))
            return false;

        conhecidos.Add(conhecimento);

        return true;
    }

    public bool Registar(
        string id,
        CategoriaDeConhecimento categoria,
        string titulo,
        string texto = null,
        string origem = null)
    {
        return Registar(new Conhecimento(id, categoria, titulo, texto, origem));
    }

    // Os conhecimentos de uma gaveta, pela ordem por que se aprenderam. Aloca
    // uma lista por chamada — é chamado ao abrir o caderno e ao trocar de
    // separador, não por frame.
    public List<Conhecimento> Da(CategoriaDeConhecimento categoria)
    {
        List<Conhecimento> gaveta = new List<Conhecimento>();

        foreach (Conhecimento conhecimento in conhecidos)
        {
            if (conhecimento != null && conhecimento.Categoria == categoria)
                gaveta.Add(conhecimento);
        }

        return gaveta;
    }

    // Esvazia o registo. Para os testes e para o recomeço de jogo — não há
    // caminho no jogo que faça o jogador desaprender.
    public void Limpar()
    {
        conhecidos.Clear();
    }
}
