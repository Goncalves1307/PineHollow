using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhotoThumbnail : MonoBehaviour, IPointerClickHandler
{
    private PhotoData photo;
    private PhotoViewerSystem photoViewerSystem;
    private PhotoAlbumSystem photoAlbumSystem;

    public PhotoData Photo => photo;

    public void Setup(
        PhotoData photo,
        PhotoViewerSystem photoViewerSystem,
        PhotoAlbumSystem photoAlbumSystem)
    {
        this.photo = photo;
        this.photoViewerSystem = photoViewerSystem;
        this.photoAlbumSystem = photoAlbumSystem;

        RawImage rawImage = GetComponent<RawImage>();

        if (rawImage != null)
        {
            rawImage.texture = photo != null ? photo.Imagem : null;
            rawImage.raycastTarget = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (photo == null)
        {
            Debug.LogError("A miniatura não tem fotografia.");
            return;
        }

        // Direito marca para comparar, esquerdo abre. São gestos diferentes
        // porque são intenções diferentes — e um botão «comparar» na UI
        // obrigava a montar mais coisa na cena para o mesmo resultado.
        if (eventData != null &&
            eventData.button == PointerEventData.InputButton.Right)
        {
            if (photoAlbumSystem != null)
                photoAlbumSystem.AlternarSeleccao(photo);

            return;
        }

        if (photoViewerSystem == null)
        {
            Debug.LogError("PhotoViewerSystem está NULL.");
            return;
        }

        photoViewerSystem.Open(photo);
    }
}
