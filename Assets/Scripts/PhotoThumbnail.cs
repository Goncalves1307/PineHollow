using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhotoThumbnail : MonoBehaviour, IPointerClickHandler
{
    private Texture2D photo;
    private PhotoViewerSystem photoViewerSystem;

    public void Setup(
        Texture2D photo,
        PhotoViewerSystem photoViewerSystem)
    {
        this.photo = photo;
        this.photoViewerSystem = photoViewerSystem;

        RawImage rawImage = GetComponent<RawImage>();

        if (rawImage != null)
        {
            rawImage.texture = photo;
            rawImage.raycastTarget = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("CLIQUE NA MINIATURA");

        if (photo == null)
        {
            Debug.LogError("A miniatura não tem fotografia.");
            return;
        }

        if (photoViewerSystem == null)
        {
            Debug.LogError("PhotoViewerSystem está NULL.");
            return;
        }

        Debug.Log("A abrir fotografia.");

        photoViewerSystem.Open(photo);
    }
}