using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowPanelView : MonoBehaviour
{
    [SerializeField] private GameObject m_startUI;

    private BattleSceneInitializer m_initializer;

    public void Initialize()
    {
        m_initializer = FindAnyObjectByType<BattleSceneInitializer>();
        ShowStart();
    }

    private void ShowStart()
    {
        if (m_startUI != null)
        {
            m_startUI.SetActive(true);
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
}
