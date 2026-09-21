using UnityEngine;
using UnityEngine.UI;

public class PhotoViewerSystem : MonoBehaviour
{
    [Header("Viewer")]
    [SerializeField] private GameObject photoViewer;
    [SerializeField] private RawImage photoImage;

    [Header("State")]
    [SerializeField] private PlayerStateMachine stateMachine;

    public bool IsOpen => photoViewer != null && photoViewer.activeSelf;

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

        if (stateMachine != null &&
            stateMachine.ConsumeBack(PlayerState.ViewingPhoto))
        {
            Close();
        }
    }

    public void Open(Texture2D photo)
    {
        if (photo == null)
            return;

        photoImage.texture = photo;

        photoViewer.SetActive(true);

        if (stateMachine != null)
            stateMachine.PushMode(PlayerState.ViewingPhoto);
    }

    public void Close()
    {
        photoViewer.SetActive(false);

        // Fechar devolve o controlo: antes deixava o cursor solto.
        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.ViewingPhoto);
    }
}
