using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PhotographySystem : MonoBehaviour
{
    [Header("Photography")]
    [SerializeField] private GameObject photoCamera;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera photoCaptureCamera;
    [SerializeField] private RenderTexture photoRenderTexture;

    [Header("Photo Preview")]
    [SerializeField] private GameObject photoPreview;
    [SerializeField] private RawImage photoImage;

    [Header("Photo Album")]
    [SerializeField] private PhotoAlbumSystem photoAlbumSystem;
    [SerializeField] private GameObject photoAlbum;

    public bool IsPhotographyMode { get; private set; }

    public IReadOnlyList<Texture2D> CapturedPhotos => capturedPhotos;

    private readonly List<Texture2D> capturedPhotos =
        new List<Texture2D>();

    private bool isPhotoPreviewOpen;

    private void Start()
    {
        photoAlbum.SetActive(false);
        photoCamera.SetActive(false);
        photoPreview.SetActive(false);

        IsPhotographyMode = false;
        isPhotoPreviewOpen = false;

        playerController.IsMovementLocked = false;
        playerController.IsLookLocked = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // =========================
        // ÁLBUM
        // =========================

        if (Keyboard.current != null &&
            Keyboard.current.tabKey.wasPressedThisFrame &&
            !IsPhotographyMode &&
            !isPhotoPreviewOpen)
        {
            TogglePhotoAlbum();
        }

        // Se o álbum estiver aberto,
        // não processamos o resto da fotografia.
        if (photoAlbum.activeSelf)
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                photoAlbumSystem.CloseAlbum();
            }

            return;
        }

        // =========================
        // MODO FOTOGRAFIA
        // =========================

        if (!IsPhotographyMode)
        {
            if (Keyboard.current != null &&
                Keyboard.current.fKey.wasPressedThisFrame)
            {
                EnterPhotographyMode();
            }

            return;
        }

        playerController.IsMovementLocked = true;
        playerController.IsLookLocked = false;

        // =========================
        // PREVIEW DA FOTOGRAFIA
        // =========================

        if (isPhotoPreviewOpen)
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ClosePhotoPreview();
            }

            return;
        }

        // =========================
        // TIRAR FOTOGRAFIA
        // =========================

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            TakePhoto();
        }

        // =========================
        // SAIR DO MODO FOTOGRAFIA
        // =========================

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitPhotographyMode();
        }
    }

    private void EnterPhotographyMode()
    {
        IsPhotographyMode = true;

        photoCamera.SetActive(true);

        playerController.IsMovementLocked = true;
        playerController.IsLookLocked = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ExitPhotographyMode()
    {
        IsPhotographyMode = false;
        isPhotoPreviewOpen = false;

        photoCamera.SetActive(false);
        photoPreview.SetActive(false);

        playerController.IsMovementLocked = false;
        playerController.IsLookLocked = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void TakePhoto()
    {
        photoCaptureCamera.transform.position =
            playerCamera.transform.position;

        photoCaptureCamera.transform.rotation =
            playerCamera.transform.rotation;

        photoCaptureCamera.fieldOfView =
            playerCamera.fieldOfView;

        photoCaptureCamera.Render();

        Texture2D newPhoto = CreatePhotoTexture();

        capturedPhotos.Add(newPhoto);

        photoImage.texture = newPhoto;

        photoPreview.SetActive(true);

        isPhotoPreviewOpen = true;

        Debug.Log(
            "Fotografia tirada! Total: " +
            capturedPhotos.Count
        );
    }

    private Texture2D CreatePhotoTexture()
    {
        RenderTexture previousActiveTexture =
            RenderTexture.active;

        RenderTexture.active =
            photoRenderTexture;

        Texture2D photo = new Texture2D(
            photoRenderTexture.width,
            photoRenderTexture.height,
            TextureFormat.RGB24,
            false
        );

        photo.ReadPixels(
            new Rect(
                0,
                0,
                photoRenderTexture.width,
                photoRenderTexture.height
            ),
            0,
            0
        );

        photo.Apply();

        RenderTexture.active =
            previousActiveTexture;

        return photo;
    }

    private void ClosePhotoPreview()
    {
        photoPreview.SetActive(false);
        isPhotoPreviewOpen = false;
    }

    private void TogglePhotoAlbum()
    {
        if (photoAlbum.activeSelf)
{
    if (Keyboard.current != null &&
        Keyboard.current.escapeKey.wasPressedThisFrame)
    {
        photoAlbumSystem.CloseAlbum();
        photoAlbum.SetActive(false);
    }

    return;
}
    }
}