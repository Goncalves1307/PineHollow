using System.Collections.Generic;
using UnityEngine;

public class PhotoAlbumSystem : MonoBehaviour
{
    [Header("Album")]
    [SerializeField] private PhotographySystem photographySystem;
    [SerializeField] private Transform photoList;
    [SerializeField] private GameObject photoThumbnailPrefab;

    [Header("Photo Viewer")]
    [SerializeField] private PhotoViewerSystem photoViewerSystem;

    [Header("Comparação")]
    [SerializeField] private PhotoComparisonSystem photoComparisonSystem;

    [Header("State")]
    [SerializeField] private PlayerStateMachine stateMachine;

    public bool IsOpen { get; private set; }

    // As que estão marcadas para comparar. No máximo duas — comparar é pôr
    // uma fotografia por cima de outra, e três não é uma operação que o GDD
    // descreva.
    private readonly List<PhotoData> seleccionadas = new List<PhotoData>();

    public IReadOnlyList<PhotoData> Seleccionadas => seleccionadas;

    private void Update()
    {
        if (!IsOpen)
            return;

        // Com a comparação aberta por cima, o Esc é dela. Sem isto, um Esc
        // fechava as duas camadas de uma vez.
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

        seleccionadas.Clear();

        RefreshAlbum();
    }

    public void CloseAlbum()
    {
        IsOpen = false;

        seleccionadas.Clear();

        if (stateMachine != null)
            stateMachine.PopMode(PlayerState.ViewingAlbum);

        // O painel desliga-se aqui, como o código antigo fazia. Sem isto ficava
        // desenhado por cima do jogo e o PhotographySystem batia para sempre no
        // seu `if (photoAlbum.activeSelf) return`, matando o F e o disparo.
        //
        // Este componente vive no próprio painel, por isso isto é a última
        // coisa do método: o estado já está todo consistente quando o Update
        // deixar de correr. Quem abre activa o painel primeiro — é o que o
        // PhotographySystem.TogglePhotoAlbum faz.
        gameObject.SetActive(false);
    }

    public void RefreshAlbum()
    {
        ClearAlbum();

        // O Tab tornou este caminho alcançável pela primeira vez: até agora
        // nada chamava o RefreshAlbum, e por isso nunca se soube o que ele
        // fazia com uma dependência por ligar.
        if (photographySystem == null || photoList == null || photoThumbnailPrefab == null)
        {
            Debug.LogError(
                $"{name}: PhotographySystem, lista ou prefab da miniatura por " +
                "ligar no inspector — o álbum abre vazio.",
                this
            );

            return;
        }

        foreach (PhotoData photo in photographySystem.CapturedPhotos)
        {
            CreateThumbnail(photo);
        }
    }

    // Clique direito numa miniatura marca-a para comparar. Ao marcar a
    // segunda, a comparação abre — não há botão «comparar» na UI, e inventar
    // um obrigava a montar mais coisa na cena para o mesmo resultado.
    public void AlternarSeleccao(PhotoData fotografia)
    {
        if (fotografia == null)
            return;

        if (seleccionadas.Remove(fotografia))
            return;

        seleccionadas.Add(fotografia);

        // A terceira empurra a mais antiga para fora, em vez de não fazer
        // nada: quem clica numa terceira quer trocar, não ser ignorado.
        if (seleccionadas.Count > 2)
            seleccionadas.RemoveAt(0);

        if (seleccionadas.Count == 2)
        {
            AbrirComparacao();

            // A selecção considera-se consumida ao abrir a comparação. Sem
            // isto, fechar a comparação e clicar numa terceira fotografia
            // reabria-a logo, emparelhada com a última do par anterior — o
            // jogador só queria começar uma escolha nova.
            seleccionadas.Clear();
        }
    }

    private void AbrirComparacao()
    {
        if (photoComparisonSystem == null)
        {
            Debug.LogError(
                $"{name}: PhotoComparisonSystem por ligar no inspector — " +
                "seleccionar duas fotografias não abre a comparação.",
                this
            );

            return;
        }

        photoComparisonSystem.Abrir(seleccionadas[0], seleccionadas[1]);
    }

    private void CreateThumbnail(PhotoData photo)
    {
        GameObject thumbnail =
            Instantiate(photoThumbnailPrefab, photoList);

        PhotoThumbnail photoThumbnail =
            thumbnail.GetComponent<PhotoThumbnail>();

        if (photoThumbnail != null)
        {
            photoThumbnail.Setup(
                photo,
                photoViewerSystem,
                this
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
        if (photoList == null)
            return;

        for (int i = photoList.childCount - 1; i >= 0; i--)
        {
            Destroy(photoList.GetChild(i).gameObject);
        }
    }
}
