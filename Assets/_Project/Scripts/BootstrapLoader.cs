using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Primeira cena da build. O que estiver pendurado neste objecto sobrevive às
/// trocas de cena, que é o que os flashbacks da FASE 7 vão precisar: a cena de
/// 1986 e a de 2026 trocam por baixo, o estado fica aqui.
/// </summary>
public class BootstrapLoader : MonoBehaviour
{
    [Header("Arranque")]
    [SerializeField] private string gameSceneName = "Prototype_Player";

    private static BootstrapLoader instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // Voltar a carregar o Bootstrap e um pedido para recomecar. Quem
            // sobrevive e que tem de carregar a cena de jogo: o Start() do que
            // sobrevive ja correu e nao volta a correr, e sem isto ficavamos
            // presos no Bootstrap para sempre.
            instance.LoadGameScene();

            Destroy(gameObject);

            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Um duplicado ja pediu o carregamento ao que sobrevive, e o Destroy()
        // do Awake() so acontece no fim do frame — sem isto o Start() dele
        // pedia-o outra vez.
        if (instance != this)
        {
            return;
        }

        LoadGameScene();
    }

    private void LoadGameScene()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError(
                "[Bootstrap] Não há cena de jogo configurada."
            );

            return;
        }

        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }
}
