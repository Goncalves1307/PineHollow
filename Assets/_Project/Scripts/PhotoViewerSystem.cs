using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PhotoViewerSystem : MonoBehaviour
{
    [Header("Viewer")]
    [SerializeField] private GameObject photoViewer;
    [SerializeField] private RawImage photoImage;

    [Header("Frente e verso")]
    // A legenda é a frente: data, local e quem lá está. O verso é o que
    // alguém escreveu à mão nas costas — canon, citado à letra.
    [SerializeField] private TMP_Text legenda;
    [SerializeField] private TMP_Text versoTexto;

    [Header("State")]
    [SerializeField] private PlayerStateMachine stateMachine;

    public bool IsOpen => photoViewer != null && photoViewer.activeSelf;

    public PhotoData Fotografia { get; private set; }

    public bool AMostrarVerso { get; private set; }

    private void Start()
    {
        // Este componente vive no próprio painel. Desligá-lo à bruta seria
        // desligar-se a si próprio no primeiro frame em que abrisse — e como o
        // modo já estava empurrado, o Update deixava de correr e ninguém
        // voltava a consumir o Esc: jogo trancado.
        //
        // A pilha é que distingue os dois casos: se ViewingPhoto está aberto,
        // este Start é o do painel a acordar por Open(); se não está, o painel
        // ficou activo na cena por engano e tem mesmo de ser desligado.
        bool aberturaEmCurso =
            stateMachine != null &&
            stateMachine.IsModeOpen(PlayerState.ViewingPhoto);

        if (photoViewer != null && !aberturaEmCurso)
            photoViewer.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        // Virar só responde a quem está no topo. Sem isto, a comparação
        // aberta por cima deixava o Q a virar a fotografia por trás dela.
        if (stateMachine != null &&
            stateMachine.IsTopMode(PlayerState.ViewingPhoto))
        {
            if (Keyboard.current != null &&
                Keyboard.current.qKey.wasPressedThisFrame)
            {
                Virar();
            }
        }

        if (stateMachine != null &&
            stateMachine.ConsumeBack(PlayerState.ViewingPhoto))
        {
            Close();
        }
    }

    public void Open(PhotoData fotografia)
    {
        if (fotografia == null)
            return;

        Fotografia = fotografia;

        // Abre-se sempre pela frente. Uma fotografia aberta de costas por ter
        // ficado assim da vez anterior é uma surpresa sem motivo.
        AMostrarVerso = false;

        Desenhar();

        photoViewer.SetActive(true);

        if (stateMachine != null)
            stateMachine.PushMode(PlayerState.ViewingPhoto);
    }

    // Uma fotografia sem nada escrito nas costas não se vira: não há segunda
    // face, e virá-la mostrava um painel vazio.
    public bool Virar()
    {
        if (Fotografia == null || !Fotografia.TemVerso)
            return false;

        AMostrarVerso = !AMostrarVerso;

        Desenhar();

        return true;
    }

    public void Close()
    {
        photoViewer.SetActive(false);

        Fotografia = null;
        AMostrarVerso = false;

        // Fechar devolve o controlo: antes deixava o cursor solto.
        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.ViewingPhoto);
    }

    private void Desenhar()
    {
        bool frente = !AMostrarVerso;

        if (photoImage != null)
        {
            photoImage.texture = Fotografia != null ? Fotografia.Imagem : null;
            photoImage.gameObject.SetActive(frente);
        }

        if (legenda != null)
        {
            legenda.text = Legendar(Fotografia);
            legenda.gameObject.SetActive(frente);
        }

        if (versoTexto != null)
        {
            versoTexto.text = Versar(Fotografia);
            versoTexto.gameObject.SetActive(!frente);
        }
    }

    // Data, local e quem lá está — os campos que a Texture2D não tinha e que
    // são a razão de esta fase existir. O que estiver por preencher não deixa
    // uma linha vazia nem um separador solto.
    public static string Legendar(PhotoData fotografia)
    {
        if (fotografia == null)
            return string.Empty;

        StringBuilder texto = new StringBuilder();

        Acrescentar(texto, fotografia.Data);
        Acrescentar(texto, fotografia.Local);

        if (fotografia.Personagens.Count > 0)
            Acrescentar(texto, string.Join(", ", fotografia.Personagens));

        return texto.ToString();
    }

    // O verso é o que lá está escrito à mão, mais os metadados — película,
    // máquina, número de negativo. É nas costas de uma fotografia que essas
    // coisas vivem, e era o único item da checklist que o tipo carregava e
    // nada mostrava.
    public static string Versar(PhotoData fotografia)
    {
        if (fotografia == null)
            return string.Empty;

        StringBuilder texto = new StringBuilder();

        Acrescentar(texto, fotografia.Verso);

        foreach (PhotoMetadata entrada in fotografia.Metadados)
        {
            if (entrada == null || string.IsNullOrWhiteSpace(entrada.Valor))
                continue;

            string chave = string.IsNullOrWhiteSpace(entrada.Chave)
                ? string.Empty
                : entrada.Chave + ": ";

            Acrescentar(texto, chave + entrada.Valor);
        }

        return texto.ToString();
    }

    private static void Acrescentar(StringBuilder texto, string linha)
    {
        if (string.IsNullOrWhiteSpace(linha))
            return;

        if (texto.Length > 0)
            texto.Append('\n');

        texto.Append(linha);
    }
}
