using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PhotoViewerSystem : MonoBehaviour
{
    [Header("Viewer")]
    [SerializeField] private GameObject photoViewer;
    [SerializeField] private RawImage photoImage;

    public bool IsOpen => photoViewer != null && photoViewer.activeSelf;

    private void Start()
    {
        photoViewer.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        photoViewer.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}