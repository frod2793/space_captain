using EasyTransition;
using UnityEngine;

public class LobbyInitializer : MonoBehaviour
{
    [SerializeField] private LobbyView m_lobbyView;
    [SerializeField] private TransitionSettings m_transitionSettings;
    
    private UserDataSO m_userData;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        m_userData = Resources.Load<UserDataSO>("UserData");

        if (m_userData == null)
        {
            return;
        }

        var lobbyViewModel = new LobbyViewModel();
        lobbyViewModel.SetData(m_userData.LobbyData, m_userData.StageProgress);

        if (m_transitionSettings != null)
        {
            lobbyViewModel.SetSceneLoader(new EasyTransitionLoader(m_transitionSettings));
        }

        if (m_lobbyView == null)
        {
            m_lobbyView = FindAnyObjectByType<LobbyView>();
        }

        if (m_lobbyView != null)
        {
            m_lobbyView.Initialize(lobbyViewModel);
        }
    }
}
