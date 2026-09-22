using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// O quadro de investigação: um cartão por cada coisa que o jogador sabe, e as
// ligações que ELE faz entre elas.
//
// A regra dura do GDD (§16) é o que define este ficheiro: «o board não deve
// resolver automaticamente todas as teorias. O objetivo é ajudar o jogador a
// organizar informação.» Aqui isso são três invariantes, e não uma intenção:
//
//   1. Nenhum caminho de código cria uma ligação. A única forma de nascer uma
//      é AlternarLigacao, e quem lhe chama é o cartão que o jogador marcou.
//      Reconstruir() monta cartões e nunca liga nada.
//   2. Nenhum caminho avalia uma ligação. Não há «certa», não há cor de
//      validação, não há som de acerto — o LigacaoDoQuadro não tem sequer onde
//      guardar um veredicto. Uma ligação errada e uma certa são indistinguíveis
//      para o jogo.
//   3. Não há contador de «x de y ligações encontradas». É o «checklist
//      excessivo» que a task manda evitar, e é também o que denunciaria quantas
//      faltam — que é a conclusão por outras palavras.
//
// Como o caderno, vive FORA do seu painel, para o B ser lido com o quadro
// fechado.
public class InvestigationBoardSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject painel;

    // Onde os cartões e as linhas vivem. As posições dos cartões são medidas
    // aqui dentro, e não no ecrã: o quadro tem de poder ser maior ou menor sem
    // os papéis mudarem de sítio uns em relação aos outros.
    [SerializeField] private RectTransform superficie;

    [SerializeField] private GameObject modeloDoCartao;
    [SerializeField] private GameObject modeloDaLigacao;

    [SerializeField] private TMP_Text aviso;

    [Header("Cores")]
    // Paleta do Pine_Hollow_Direccao_Artistica.md §7.2.
    [SerializeField] private Color corDoCartao = new Color(0.784f, 0.769f, 0.722f);
    [SerializeField] private Color corDoCartaoMarcado = new Color(0.659f, 0.698f, 0.710f);

    [Header("Disposição")]
    [SerializeField] private Vector2 espacamento = new Vector2(260f, 96f);
    [SerializeField] private int colunas = 4;
    [SerializeField] private float espessuraDaLinha = 4f;

    [Header("State")]
    [SerializeField] private PlayerStateMachine stateMachine;

    public bool IsOpen { get; private set; }

    private readonly List<CartaoDoQuadro> cartoes = new List<CartaoDoQuadro>();

    private readonly List<RectTransform> linhas = new List<RectTransform>();

    [SerializeField] private List<LigacaoDoQuadro> ligacoes =
        new List<LigacaoDoQuadro>();

    [SerializeField] private List<PosicaoDeCartao> posicoes =
        new List<PosicaoDeCartao>();

    private readonly List<string> marcados = new List<string>();

    public IReadOnlyList<CartaoDoQuadro> Cartoes => cartoes;
    public IReadOnlyList<LigacaoDoQuadro> Ligacoes => ligacoes;
    public IReadOnlyList<string> Marcados => marcados;

    private void Start()
    {
        if (painel != null)
            painel.SetActive(false);
    }

    private void Update()
    {
        if (IsOpen &&
            stateMachine != null &&
            stateMachine.ConsumeBack(PlayerState.ViewingBoard))
        {
            Fechar();

            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.bKey.wasPressedThisFrame)
        {
            Alternar();
        }
    }

    // Público pela mesma razão do caderno e do álbum: o B é lido no Update e o
    // Keyboard.current é null em EditMode.
    public void Alternar()
    {
        if (IsOpen)
        {
            if (stateMachine != null &&
                !stateMachine.IsTopMode(PlayerState.ViewingBoard))
            {
                return;
            }

            Fechar();

            return;
        }

        if (stateMachine != null && stateMachine.OpenModeCount > 0)
            return;

        Abrir();
    }

    public void Abrir()
    {
        if (IsOpen)
            return;

        if (painel == null)
        {
            Debug.LogError(
                $"{name}: painel do quadro por ligar no inspector — " +
                "o B não abre nada.",
                this
            );

            return;
        }

        IsOpen = true;

        painel.SetActive(true);

        marcados.Clear();

        Reconstruir();

        if (stateMachine != null)
            stateMachine.PushMode(PlayerState.ViewingBoard);
    }

    public void Fechar()
    {
        if (!IsOpen)
            return;

        IsOpen = false;

        marcados.Clear();

        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.ViewingBoard);

        if (painel != null)
            painel.SetActive(false);
    }

    // Monta um cartão por conhecimento. NÃO liga nada — as ligações que já
    // existem são as que o jogador fez e ficaram guardadas; ligações novas só
    // nascem por AlternarLigacao.
    public void Reconstruir()
    {
        if (superficie == null || modeloDoCartao == null)
        {
            Debug.LogError(
                $"{name}: superfície ou modelo do cartão por ligar no " +
                "inspector — o quadro abre vazio.",
                this
            );

            return;
        }

        LimparCartoes();

        RegistoDeConhecimento registo = RegistoDeConhecimento.Instancia;

        int indice = 0;

        foreach (Conhecimento conhecimento in registo.Conhecidos)
        {
            if (conhecimento == null)
                continue;

            GameObject objecto = Instantiate(modeloDoCartao, superficie);

            objecto.SetActive(true);

            CartaoDoQuadro cartao = objecto.GetComponent<CartaoDoQuadro>();

            if (cartao == null)
            {
                // Destruir e não só saltar: o clone já está instanciado e
                // activo. Saltá-lo deixava um papel em branco no meio do quadro
                // a cada Reconstruir(), e o Reconstruir corre a cada abertura.
                Remover(objecto);

                Debug.LogError(
                    "CartaoDoQuadro não encontrado no modelo do cartão."
                );

                continue;
            }

            cartao.Configurar(this, conhecimento, corDoCartao);

            cartao.Pousar(PosicaoDe(conhecimento.Id, indice, registo.Total));

            cartoes.Add(cartao);

            indice++;
        }

        if (aviso != null)
        {
            aviso.text = cartoes.Count == 0
                ? "O quadro está vazio. Traz alguma coisa do mundo primeiro."
                : "Botão direito em dois cartões liga-os. Arrasta para arrumar.";
        }

        DesenharLigacoes();
    }

    // Onde é que este cartão fica. Se o jogador já o arrumou, fica onde ele o
    // pôs; se não, cai numa grelha — e numa grelha estável, para o quadro não
    // baralhar sozinho de cada vez que se abre.
    private Vector2 PosicaoDe(string id, int indice, int total)
    {
        PosicaoDeCartao guardada = Guardada(id);

        if (guardada != null)
            return guardada.Posicao;

        int seguras = Mathf.Max(1, colunas);

        int coluna = indice % seguras;
        int linha = indice / seguras;

        // Centrada nos dois eixos. Sem a metade vertical, a grelha começava no
        // meio do quadro e crescia para baixo: a primeira captura tinha metade
        // de cima vazia e a última fila encostada ao rodapé.
        int filas = Mathf.CeilToInt(total / (float)seguras);

        Vector2 inicial = new Vector2(
            (coluna - (seguras - 1) * 0.5f) * espacamento.x,
            ((filas - 1) * 0.5f - linha) * espacamento.y
        );

        posicoes.Add(new PosicaoDeCartao(id, inicial));

        return inicial;
    }

    private PosicaoDeCartao Guardada(string id)
    {
        foreach (PosicaoDeCartao posicao in posicoes)
        {
            if (posicao != null && posicao.Id == id)
                return posicao;
        }

        return null;
    }

    // Arrumar um cartão. Guarda onde ficou, para o quadro estar como o jogador o
    // deixou quando voltar — arrumar é metade do que um quadro destes faz.
    public void Mover(string id, Vector2 posicao)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        PosicaoDeCartao guardada = Guardada(id);

        if (guardada == null)
            posicoes.Add(new PosicaoDeCartao(id, posicao));
        else
            guardada.Pousar(posicao);

        CartaoDoQuadro cartao = Cartao(id);

        if (cartao != null)
            cartao.Pousar(posicao);

        DesenharLigacoes();
    }

    // Marcar dois cartões liga-os, à maneira do clique direito do álbum: marcar
    // a segunda miniatura é o que abre a comparação, e marcar o segundo cartão é
    // o que faz a ligação. Não se inventou um botão «ligar» para isto.
    public void AlternarMarcacao(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (marcados.Remove(id))
        {
            Pintar();

            return;
        }

        marcados.Add(id);

        if (marcados.Count >= 2)
        {
            AlternarLigacao(marcados[0], marcados[1]);

            marcados.Clear();
        }

        Pintar();
    }

    // A ÚNICA porta de entrada de uma ligação. Devolve true se acabou de nascer,
    // false se acabou de ser desfeita — e não devolve nem diz se está certa,
    // porque o jogo não sabe nem pode saber.
    public bool AlternarLigacao(string idA, string idB)
    {
        if (string.IsNullOrWhiteSpace(idA) ||
            string.IsNullOrWhiteSpace(idB) ||
            idA == idB)
        {
            return false;
        }

        for (int i = 0; i < ligacoes.Count; i++)
        {
            if (ligacoes[i] != null && ligacoes[i].Une(idA, idB))
            {
                ligacoes.RemoveAt(i);

                DesenharLigacoes();

                return false;
            }
        }

        ligacoes.Add(new LigacaoDoQuadro(idA, idB));

        DesenharLigacoes();

        return true;
    }

    public bool EstaLigado(string idA, string idB)
    {
        foreach (LigacaoDoQuadro ligacao in ligacoes)
        {
            if (ligacao != null && ligacao.Une(idA, idB))
                return true;
        }

        return false;
    }

    public CartaoDoQuadro Cartao(string id)
    {
        foreach (CartaoDoQuadro cartao in cartoes)
        {
            if (cartao != null && cartao.Id == id)
                return cartao;
        }

        return null;
    }

    public bool EstaMarcado(string id)
    {
        return marcados.Contains(id);
    }

    private void Pintar()
    {
        foreach (CartaoDoQuadro cartao in cartoes)
        {
            if (cartao == null)
                continue;

            cartao.Pintar(
                EstaMarcado(cartao.Id) ? corDoCartaoMarcado : corDoCartao
            );
        }
    }

    public void DesenharLigacoes()
    {
        if (superficie == null || modeloDaLigacao == null)
            return;

        LimparLinhas();

        foreach (LigacaoDoQuadro ligacao in ligacoes)
        {
            if (ligacao == null)
                continue;

            CartaoDoQuadro a = Cartao(ligacao.IdA);
            CartaoDoQuadro b = Cartao(ligacao.IdB);

            // Uma ligação a um cartão que não está no quadro não se desenha,
            // mas também não se apaga: o conhecimento pode não estar montado
            // neste instante e apagar a ligação era decidir por ele.
            if (a == null || b == null)
                continue;

            GameObject objecto = Instantiate(modeloDaLigacao, superficie);

            objecto.SetActive(true);

            RectTransform linha = (RectTransform)objecto.transform;

            // Atrás dos cartões, senão a linha corta-lhes o texto ao meio.
            linha.SetAsFirstSibling();

            ColocarLinha(linha, a.Posicao, b.Posicao, espessuraDaLinha);

            linhas.Add(linha);
        }
    }

    // Põe um rectângulo fino a unir dois pontos. Estático e sem input: é
    // aritmética, e aritmética que só corre em play mode é aritmética por
    // verificar.
    public static void ColocarLinha(
        RectTransform linha,
        Vector2 a,
        Vector2 b,
        float espessura)
    {
        if (linha == null)
            return;

        Vector2 delta = b - a;

        linha.anchorMin = new Vector2(0.5f, 0.5f);
        linha.anchorMax = new Vector2(0.5f, 0.5f);
        linha.pivot = new Vector2(0.5f, 0.5f);

        linha.anchoredPosition = (a + b) * 0.5f;
        linha.sizeDelta = new Vector2(delta.magnitude, espessura);

        linha.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg
        );
    }

    private void LimparCartoes()
    {
        foreach (CartaoDoQuadro cartao in cartoes)
        {
            if (cartao != null)
                Remover(cartao.gameObject);
        }

        cartoes.Clear();
    }

    private void LimparLinhas()
    {
        foreach (RectTransform linha in linhas)
        {
            if (linha != null)
                Remover(linha.gameObject);
        }

        linhas.Clear();
    }

    // Destroy() em EditMode só acontece no fim do frame, e num teste isso deixa
    // as linhas antigas penduradas enquanto se conta as novas. Em jogo é
    // Destroy() como em todo o lado — mas desligado primeiro: o Destroy de play
    // mode também é adiado até ao fim do frame, e até lá o objecto continua
    // activo a ocupar o seu lugar no layout, ao lado do que o substitui.
    private static void Remover(GameObject alvo)
    {
        if (alvo == null)
            return;

        alvo.SetActive(false);

        if (Application.isPlaying)
            Destroy(alvo);
        else
            DestroyImmediate(alvo);
    }
}
