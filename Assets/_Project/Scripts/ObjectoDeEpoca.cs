using UnityEngine;

// Marca um objecto como existindo só numa das épocas.
//
// É o «objectos que existem só numa delas» da subtask 869f4yb88, e é a única
// coisa do mundo que a troca de época liga e desliga. NÃO se põe isto em
// paredes, chão, tectos nem em nada que defina a forma da divisão: a direcção
// artística §5 é categórica em que a geometria nunca muda entre 1986 e 2026,
// porque o jogador alinha uma fotografia de uma época com a divisão da outra e
// uma parede que se mexa torna o alinhamento impossível. Isto é para os três a
// cinco marcadores por divisão — o carro que ainda lá estava, o cartaz que já
// caiu, a pessoa que ainda era viva.
//
// Um objecto que exista nas duas épocas não leva este componente. Marcar tudo
// era pagar uma varredura por objecto da cena para dizer «continua ligado».
[DisallowMultipleComponent]
public class ObjectoDeEpoca : MonoBehaviour
{
    [Header("Em que épocas este objecto existe")]
    [SerializeField] private bool existeEm2026 = true;
    [SerializeField] private bool existeEm1986 = true;

    public bool ExisteEm2026 => existeEm2026;
    public bool ExisteEm1986 => existeEm1986;

    public bool Existe(Epoca epoca)
    {
        return epoca == Epoca.Passado1986 ? existeEm1986 : existeEm2026;
    }

    // Liga ou desliga o objecto conforme a época. Devolve true se isto mudou
    // alguma coisa — quem monta a cena conta as mudanças para decidir se grava.
    public bool Aplicar(Epoca epoca)
    {
        bool deveExistir = Existe(epoca);

        if (gameObject.activeSelf == deveExistir)
            return false;

        gameObject.SetActive(deveExistir);

        return true;
    }

    // Método público e não lógica dentro do Start, porque o Start não corre em
    // EditMode e uma regra que só ele executa é uma regra sem teste — foi assim
    // que a prioridade autorado/automático do registo de conhecimento esteve
    // invertida sem nada a apanhá-la.
    private void Start()
    {
        // Acerta-se sozinho ao nascer, para que um objecto instanciado a meio
        // de um flashback não apareça na época errada até à troca seguinte.
        Aplicar(EstadoDoMundo.Instancia.EpocaActual);
    }

    // Varre a cena e acerta todos. Chamado pelo EstadoDoMundo.MudarPara.
    //
    // FindObjectsInactive.Include não é uma precaução, é o que faz isto
    // funcionar: um objecto que só existe em 1986 está DESLIGADO enquanto se
    // joga em 2026, e uma varredura que só visse os activos nunca mais o
    // encontrava para o voltar a acender. O flashback entrava numa cidade de
    // 1986 vazia e ninguém percebia porquê.
    public static int AplicarATodos(Epoca epoca)
    {
        ObjectoDeEpoca[] todos = FindObjectsByType<ObjectoDeEpoca>(
            FindObjectsInactive.Include
        );

        int mudancas = 0;

        foreach (ObjectoDeEpoca objecto in todos)
        {
            if (objecto != null && objecto.Aplicar(epoca))
                mudancas++;
        }

        return mudancas;
    }

    private void OnValidate()
    {
        // Um objecto que não existe em época nenhuma é sempre engano: ou é
        // lixo que devia ter sido apagado, ou alguém desmarcou as duas caixas
        // à espera de uma terceira que não há. Fica invisível o jogo inteiro
        // sem nunca dar erro — exactamente o tipo de defeito que custou seis
        // semanas noutro projecto desta casa.
        if (!existeEm2026 && !existeEm1986)
        {
            Debug.LogWarning(
                "ObjectoDeEpoca em '" + name + "' não existe em época nenhuma: " +
                "vai ficar desligado o jogo inteiro. Marque 2026, 1986, ou tire " +
                "o componente.",
                this
            );
        }
    }
}
