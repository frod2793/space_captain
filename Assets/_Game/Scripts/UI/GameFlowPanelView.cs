using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowPanelView : MonoBehaviour
{
    [SerializeField] private GameObject m_startUI;
    [SerializeField] private GameObject m_gameOverUI;

    private IBattleHUDViewModel m_viewModel;
    private IGameProgressViewModel m_progressViewModel;
    private BattleSceneInitializer m_initializer;

    public void Initialize(IBattleHUDViewModel viewModel, IGameProgressViewModel progressViewModel)
    {
        m_viewModel = viewModel;
        m_progressViewModel = progressViewModel;
        m_initializer = FindAnyObjectByType<BattleSceneInitializer>();

        if (m_viewModel != null)
        {
            m_viewModel.OnShowGameOver += ShowGameOver;
        }

        if (m_progressViewModel != null)
        {
            m_progressViewModel.OnGameCleared += ShowGameOver;
        }

        ShowStart();
    }

    private void OnDestroy()
    {
        if (m_viewModel != null)
        {
            m_viewModel.OnShowGameOver -= ShowGameOver;
        }

        if (m_progressViewModel != null)
        {
            m_progressViewModel.OnGameCleared -= ShowGameOver;
        }
    }

    private void ShowStart()
    {
        if (m_startUI != null)
        {
            m_startUI.SetActive(true);
        }

        if (m_gameOverUI != null)
        {
            m_gameOverUI.SetActive(false);
        }
    }

    private void ShowGameOver()
    {
        if (m_startUI != null)
        {
            m_startUI.SetActive(false);
        }

        if (m_gameOverUI != null)
        {
            m_gameOverUI.SetActive(true);
        }
    }

    public void OnStartButtonClicked()
    {
        if (m_startUI != null)
        {
            m_startUI.SetActive(false);
        }

        if (m_initializer != null)
        {
            m_initializer.StartGameTime(); 
        }
    }

    public void OnRetryButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
