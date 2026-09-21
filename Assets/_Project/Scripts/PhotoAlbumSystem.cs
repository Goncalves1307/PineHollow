using UnityEngine;

public class PhotoAlbumSystem : MonoBehaviour
{
    [Header("Album")]
    [SerializeField] private PhotographySystem photographySystem;
    [SerializeField] private Transform photoList;
    [SerializeField] private GameObject photoThumbnailPrefab;

    [Header("Photo Viewer")]
    [SerializeField] private PhotoViewerSystem photoViewerSystem;

    [Header("State")]
    [SerializeField] private PlayerStateMachine stateMachine;

    public bool IsOpen { get; private set; }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (stateMachine != null &&
            stateMachine.ConsumeBack(PlayerState.ViewingAlbum))
        {
            CloseAlbum();
        }
    }

    public void OpenAlbum()
    {
        IsOpen = true;

        if (stateMachine != null)
            stateMachine.PushMode(PlayerState.ViewingAlbum);

        RefreshAlbum();
    }

    public void CloseAlbum()
    {
        IsOpen = false;

        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.ViewingAlbum);

        // O painel desliga-se aqui, como o código antigo fazia. Sem isto ficava
        // desenhado por cima do jogo e o PhotographySystem batia para sempre no
        // seu `if (photoAlbum.activeSelf) return`, matando o F e o disparo.
        //
        // Este componente vive no próprio painel, por isso isto é a última
        // coisa do método: o estado já está todo consistente quando o Update
        // deixar de correr. Quem abre tem de activar o painel primeiro — e
        // abrir é FASE 5.
        gameObject.SetActive(false);
    }

    public void RefreshAlbum()
    {
        ClearAlbum();

        foreach (Texture2D photo in photographySystem.CapturedPhotos)
        {
            CreateThumbnail(photo);
        }
    }

    private void CreateThumbnail(Texture2D photo)
    {
        GameObject thumbnail =
            Instantiate(photoThumbnailPrefab, photoList);

        PhotoThumbnail photoThumbnail =
            thumbnail.GetComponent<PhotoThumbnail>();

        if (photoThumbnail != null)
        {
            photoThumbnail.Setup(
                photo,
                photoViewerSystem
            );
        }
        else
        {
            Debug.LogError(
                "PhotoThumbnail não encontrado no prefab."
            );
        }
    }

    private void ClearAlbum()
    {
        for (int i = photoList.childCount - 1; i >= 0; i--)
        {
            Destroy(photoList.GetChild(i).gameObject);
        }
    }
}
