using EasyTransition;
using UnityEngine;

public class LobbyInitializer : MonoBehaviour
{
    [SerializeField] private LobbyView m_lobbyView;
    [SerializeField] private UserProfilePopupView m_profilePopupView;
    [SerializeField] private TransitionSettings m_transitionSettings;
    
    private UserDataSO m_userData;
    private UserProfileViewModel m_userProfileViewModel;

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

        // 프로필 팝업 뷰모델 초기화
        m_userProfileViewModel = new UserProfileViewModel();
        m_userProfileViewModel.SetData(m_userData.LobbyData.UID, m_userData.LobbyData.ProfileIconID);

        if (m_profilePopupView != null)
        {
            m_profilePopupView.Initialize(m_userProfileViewModel);
            m_profilePopupView.gameObject.SetActive(false);
        }

        // 이벤트 바인딩
        lobbyViewModel.OnProfileOpenRequested += () => 
        {
            if (m_profilePopupView != null) m_profilePopupView.Show();
        };

        m_userProfileViewModel.OnCloseRequested += () => 
        {
            if (m_profilePopupView != null) m_profilePopupView.Hide();
        };

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
