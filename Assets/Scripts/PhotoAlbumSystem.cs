using UnityEngine;

public class PhotoAlbumSystem : MonoBehaviour
{
    [Header("Album")]
    [SerializeField] private PhotographySystem photographySystem;
    [SerializeField] private Transform photoList;
    [SerializeField] private GameObject photoThumbnailPrefab;

    [Header("Photo Viewer")]
    [SerializeField] private PhotoViewerSystem photoViewerSystem;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;

    public bool IsOpen { get; private set; }

    public void OpenAlbum()
    {
        IsOpen = true;

        playerController.IsMovementLocked = true;
        playerController.IsLookLocked = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshAlbum();
    }

public void CloseAlbum()
{
    IsOpen = false;

    playerController.IsMovementLocked = false;
    playerController.IsLookLocked = false;

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
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