using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PhotographySystem : MonoBehaviour
{
    [Header("Photography")]
    [SerializeField] private GameObject photoCamera;
    [SerializeField] private PlayerStateMachine stateMachine;
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

        // Com o álbum aberto não se processa o resto da fotografia. O Esc do
        // álbum é do PhotoAlbumSystem, não daqui.
        if (photoAlbum.activeSelf)
            return;

        // =========================
        // MODO FOTOGRAFIA
        // =========================

        if (!IsPhotographyMode)
        {
            // Só se entra com o player livre: nada de abrir o modo fotografia
            // por cima de uma inspecção, de um ecrã aberto, ou com o rato solto
            // pelo Esc — daí ser o lock e não a contagem da pilha.
            if (Keyboard.current != null &&
                Keyboard.current.fKey.wasPressedThisFrame &&
                stateMachine != null &&
                !stateMachine.IsMovementLocked)
            {
                EnterPhotographyMode();
            }

            return;
        }

        // =========================
        // PREVIEW DA FOTOGRAFIA
        // =========================

        if (isPhotoPreviewOpen)
        {
            if (stateMachine != null &&
                stateMachine.ConsumeBack(PlayerState.PhotoPreview))
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

            // Tirar a fotografia abre o preview. Sair fica para o Esc seguinte.
            return;
        }

        // =========================
        // SAIR DO MODO FOTOGRAFIA
        // =========================

        if (stateMachine != null &&
            stateMachine.ConsumeBack(PlayerState.Photographing))
        {
            ExitPhotographyMode();
        }
    }

    private void EnterPhotographyMode()
    {
        IsPhotographyMode = true;

        photoCamera.SetActive(true);

        if (stateMachine != null)
            stateMachine.PushMode(PlayerState.Photographing);
    }

    private void ExitPhotographyMode()
    {
        IsPhotographyMode = false;
        isPhotoPreviewOpen = false;

        photoCamera.SetActive(false);
        photoPreview.SetActive(false);

        if (stateMachine != null)
        {
            // O preview pode estar aberto por cima: sai-se das duas camadas.
            stateMachine.PopMode(PlayerState.PhotoPreview);
            stateMachine.PopMode(PlayerState.Photographing);
        }
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

        if (stateMachine != null)
            stateMachine.PushMode(PlayerState.PhotoPreview);

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

        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.PhotoPreview);
    }

    private void TogglePhotoAlbum()
    {
        if (photoAlbum.activeSelf)
        {
            photoAlbumSystem.CloseAlbum();
            photoAlbum.SetActive(false);

            return;
        }

        // Abrir o álbum é FASE 5 e está atrás do PhotoData: por isso o Tab
        // ainda não abre nada. O que saiu daqui foi o segundo leitor do Esc —
        // com o álbum aberto, quem o fecha é o PhotoAlbumSystem.
    }
}