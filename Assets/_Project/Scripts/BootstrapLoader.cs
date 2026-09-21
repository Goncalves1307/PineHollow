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
        // Se voltarmos ao Bootstrap por engano, não queremos dois.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
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
