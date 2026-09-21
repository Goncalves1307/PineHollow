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

    [Header("Fotografias que começam no álbum")]
    // Quem quiser abrir o jogo já com fotografias na mão põe-nas aqui. Serve
    // o vertical slice, onde as fotografias são objectos que se encontram e
    // não que se tiram, e serve para ver o álbum a funcionar sem depender da
    // captura.
    [SerializeField] private PhotoAsset[] fotografiasIniciais;

    public bool IsPhotographyMode { get; private set; }

    // Deixou de ser List<Texture2D>: uma Texture2D não tem data, local,
    // pessoas nem verso, e é sobre esses campos que a comparação e o
    // alinhamento trabalham.
    public IReadOnlyList<PhotoData> CapturedPhotos => capturedPhotos;

    private readonly List<PhotoData> capturedPhotos =
        new List<PhotoData>();

    private bool isPhotoPreviewOpen;

    private void Start()
    {
        photoAlbum.SetActive(false);
        photoCamera.SetActive(false);
        photoPreview.SetActive(false);

        IsPhotographyMode = false;
        isPhotoPreviewOpen = false;

        CarregarFotografiasIniciais();
    }

    private void CarregarFotografiasIniciais()
    {
        if (fotografiasIniciais == null)
            return;

        foreach (PhotoAsset asset in fotografiasIniciais)
        {
            if (asset != null)
                AddPhoto(asset.Criar());
        }
    }

    // A porta de entrada para fotografias que não vêm da captura: as que o
    // jogador apanha do mundo (PhotoInteractable). Antes não havia nenhuma —
    // a lista era privada e a propriedade só de leitura, e por isso «guardar
    // no álbum ao apanhar» não era sequer exprimível.
    public void AddPhoto(PhotoData fotografia)
    {
        if (fotografia == null)
            return;

        // A mesma fotografia apanhada duas vezes não são duas fotografias.
        // Vale só para as autoradas: as capturas têm id próprio cada uma.
        if (fotografia.Autorada && JaTem(fotografia.Id))
            return;

        capturedPhotos.Add(fotografia);
    }

    public bool JaTem(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        foreach (PhotoData fotografia in capturedPhotos)
        {
            if (fotografia.Id == id)
                return true;
        }

        return false;
    }

    // As texturas das capturas são criadas à mão com `new Texture2D` e o Unity
    // não as recolhe sozinho: cada disparo ficava a ocupar memória até fechar
    // o jogo. As autoradas não se tocam — a imagem delas é um asset, e
    // destruí-la estragava o asset para toda a sessão.
    private void OnDestroy()
    {
        foreach (PhotoData fotografia in capturedPhotos)
        {
            if (fotografia != null && !fotografia.Autorada && fotografia.Imagem != null)
                Destroy(fotografia.Imagem);
        }

        capturedPhotos.Clear();
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

        // A fotografia tirada pelo jogador é 2026 — é o presente do jogo. Não
        // tem autor nem verso: é um registo do que ele viu, e o álbum
        // distingue-a das que encontrou por isso mesmo.
        // 2026 é o presente do jogo e está no cânone. A data por extenso é a
        // hora a que foi tirada — «Agora» não é formato de data nenhum, e o
        // álbum mostra este campo ao lado das fotografias de 1986 que têm
        // dia, mês e hora.
        capturedPhotos.Add(
            PhotoData.DeCaptura(
                newPhoto,
                2026,
                System.DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy, HH:mm")
            )
        );

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

    // Público para ser alcançável sem teclado. O Tab é lido no Update e o
    // Keyboard.current é null em EditMode: com isto privado, o caminho que
    // esteve morto desde que foi escrito continuava sem poder ser testado —
    // e foi assim que ninguém deu por ele.
    public void TogglePhotoAlbum()
    {
        if (photoAlbum.activeSelf)
        {
            // Só fecha se for ele o de cima. Com a comparação ou o viewer
            // abertos por cima, isto fazia PopMode do MEIO da pilha e
            // desligava o painel por baixo de outro que continuava aberto: o
            // Esc seguinte atirava o jogador para o mundo sem passar pelo
            // álbum.
            if (stateMachine != null &&
                !stateMachine.IsTopMode(PlayerState.ViewingAlbum))
            {
                return;
            }

            photoAlbumSystem.CloseAlbum();
            photoAlbum.SetActive(false);

            return;
        }

        if (photoAlbumSystem == null)
        {
            Debug.LogError(
                $"{name}: PhotoAlbumSystem por ligar no inspector — " +
                "o Tab não abre o álbum.",
                this
            );

            return;
        }

        // Só se abre com o player livre. O álbum solta o cursor, e abri-lo por
        // cima de uma inspecção deixava o objecto na mão por trás do painel.
        if (stateMachine != null && stateMachine.OpenModeCount > 0)
            return;

        // O painel primeiro, o OpenAlbum depois — e não ao contrário. O
        // PhotoAlbumSystem vive dentro deste painel: com ele desligado o
        // Update dele não corre, e o ViewingAlbum que o OpenAlbum empurra
        // ficaria na pilha sem ninguém a consumir o Esc. O jogo só saía disso
        // pela saída de emergência da PlayerStateMachine, com aviso no log.
        photoAlbum.SetActive(true);

        photoAlbumSystem.OpenAlbum();
    }
}