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

    [Header("버튼")]
    [SerializeField] private Button m_battleStartButton;
    [SerializeField] private Button m_settingsButton;

    [Header("일러스트")]
    [SerializeField] private GameObject m_illustrationPlaceholder;

    private ILobbyViewModel m_viewModel;

    public void Initialize(ILobbyViewModel viewModel)
    {
        m_viewModel = viewModel;

        if (ValidateComponents())
        {
            m_viewModel.OnDataChanged += UpdateUI;
            m_battleStartButton.onClick.AddListener(m_viewModel.StartBattle);
            m_settingsButton.onClick.AddListener(m_viewModel.OpenSettings);
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
    }

    private void UpdateUI()
    {
        m_nicknameText.text = m_viewModel.Nickname;
        m_levelText.text = $"LV.{m_viewModel.Level}";
        m_goldText.text = m_viewModel.Gold.ToString("N0");
        m_diamondText.text = m_viewModel.Diamond.ToString("N0");
        m_staminaText.text = $"{m_viewModel.CurrentStamina} / {m_viewModel.MaxStamina}";

        m_mapNameText.text = m_viewModel.CurrentMapName;
        m_maxWaveText.text = $"최고 기록: {m_viewModel.MaxWaveReached} 웨이브";
    }
}
