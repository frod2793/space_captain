using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyView : MonoBehaviour
{
    [Header("유저 정보 패널")]
    [SerializeField] private TMP_Text m_nicknameText;
    [SerializeField] private TMP_Text m_levelText;
    [SerializeField] private TMP_Text m_goldText;
    [SerializeField] private TMP_Text m_diamondText;
    [SerializeField] private TMP_Text m_staminaText;

    [Header("스테이지 정보 패널")]
    [SerializeField] private TMP_Text m_mapNameText;
    [SerializeField] private TMP_Text m_maxWaveText;
    [SerializeField] private Button m_normalDifficultyButton;
    [SerializeField] private Button m_eliteDifficultyButton;

    [Header("버튼")]
    [SerializeField] private Button m_battleStartButton;
    [SerializeField] private TMP_Text m_staminaCostText;
    [SerializeField] private Button m_settingsButton;
    [SerializeField] private Button m_profileButton;
    [SerializeField] private Button m_partyButton;

    [Header("일러스트")]
    [SerializeField] private Image m_characterIllustration;

    private ILobbyViewModel m_viewModel;

    public void Initialize(ILobbyViewModel viewModel)
    {
        m_viewModel = viewModel;

        if (ValidateComponents())
        {
            m_viewModel.OnDataChanged += UpdateUI;
            m_battleStartButton.onClick.AddListener(func_OnBattleStartClicked);
            m_settingsButton.onClick.AddListener(func_OnSettingsClicked);
            
            if (m_profileButton != null)
            {
                m_profileButton.onClick.AddListener(func_OnProfileClicked);
            }

            if (m_partyButton != null)
            {
                m_partyButton.onClick.AddListener(func_OnPartyClicked);
            }

            if (m_normalDifficultyButton != null)
            {
                m_normalDifficultyButton.onClick.AddListener(func_OnNormalDifficultyClicked);
            }

            if (m_eliteDifficultyButton != null)
            {
                m_eliteDifficultyButton.onClick.AddListener(func_OnEliteDifficultyClicked);
            }
            
            UpdateUI();
        }
    }

    private bool ValidateComponents()
    {
        if (m_viewModel == null)
        {
            return false;
        }

        if (m_nicknameText == null || m_levelText == null || m_goldText == null ||
            m_diamondText == null || m_staminaText == null || m_mapNameText == null ||
            m_maxWaveText == null || m_battleStartButton == null || m_settingsButton == null)
        {
            return false;
        }
        return true;
    }

    private void OnDestroy()
    {
        if (m_viewModel != null)
        {
            m_viewModel.OnDataChanged -= UpdateUI;
        }

        if (m_battleStartButton != null)
        {
            m_battleStartButton.onClick.RemoveAllListeners();
        }

        if (m_settingsButton != null)
        {
            m_settingsButton.onClick.RemoveAllListeners();
        }

        if (m_profileButton != null)
        {
            m_profileButton.onClick.RemoveAllListeners();
        }

        if (m_partyButton != null)
        {
            m_partyButton.onClick.RemoveAllListeners();
        }

        if (m_normalDifficultyButton != null)
        {
            m_normalDifficultyButton.onClick.RemoveAllListeners();
        }

        if (m_eliteDifficultyButton != null)
        {
            m_eliteDifficultyButton.onClick.RemoveAllListeners();
        }
    }

    private void UpdateUI()
    {
        m_nicknameText.text = m_viewModel.Nickname;
        m_levelText.text = $"LV.{m_viewModel.Level}";
        m_goldText.text = m_viewModel.Gold.ToString("N0");
        m_diamondText.text = m_viewModel.Diamond.ToString("N0");
        m_staminaText.text = $"{m_viewModel.CurrentStamina} / {m_viewModel.MaxStamina}";

        if (m_staminaCostText != null)
        {
            m_staminaCostText.text = $"-{m_viewModel.RequiredStamina}";
        }

        m_mapNameText.text = m_viewModel.DisplayStageName;
        m_maxWaveText.text = $"최고 기록: {m_viewModel.MaxWaveReached} 웨이브";
    }

    public void func_OnNormalDifficultyClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.SelectDifficulty(StageDifficulty.Normal);
        }
    }

    public void func_OnEliteDifficultyClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.SelectDifficulty(StageDifficulty.Elite);
        }
    }

    private void func_OnBattleStartClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.StartBattle();
        }
    }

    private void func_OnSettingsClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.OpenSettings();
        }
    }

    private void func_OnProfileClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.OpenProfile();
        }
    }

    private void func_OnPartyClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.OpenParty();
        }
    }
}
