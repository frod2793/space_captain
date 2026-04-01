using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [설명]: 게임 내 모든 UI 요소를 총괄 관리하는 매니저 클래스입니다.
/// 프로토타이핑 단계를 위해 중앙 집중식으로 제작되었습니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("진행도 UI")]
    [SerializeField] private Slider m_progressSlider;

    [Header("모선 HP UI")]
    [SerializeField] private Slider m_hpSlider;

    [Header("보스 UI")]
    [SerializeField] private Slider m_bossHpSlider;

    [Header("상태 패널 UI")]
    [SerializeField] private GameObject m_startPanel;
    [SerializeField] private GameObject m_gameOverPanel;

    private void Start()
    {
        // 초기 상태 설정: 시작 패널 활성화, 게임 일시정지
        if (m_startPanel != null) m_startPanel.SetActive(true);
        if (m_gameOverPanel != null) m_gameOverPanel.SetActive(false);
        
        Time.timeScale = 0f;

        // 모선 이벤트 구독
        var masterShip = Object.FindAnyObjectByType<MasterShip>();
        if (masterShip != null)
        {
            masterShip.OnMasterShipDestroyed += ShowGameOver;
            masterShip.OnHpChanged += UpdateHpBar; // HP 변경 이벤트 연동
        }

        // [추가]: 모든 플레이어 캐릭터 사망 이벤트 구독
        var swapManager = Object.FindAnyObjectByType<PlayerSwapManager>();
        if (swapManager != null)
        {
            swapManager.OnAllPlayersDead += ShowGameOver;
        }
    }

    /// <summary>
    /// [설명]: 게임 시작 버튼 클릭 시 호출됩니다.
    /// </summary>
    public void OnStartButtonClicked()
    {
        if (m_startPanel != null) m_startPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// [설명]: 재시도 버튼 클릭 시 호출됩니다. 현재 씬을 다시 로드합니다.
    /// </summary>
    public void OnRetryButtonClicked()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// [설명]: 진행도 슬라이더의 값을 설정합니다 (0.0 ~ 1.0).
    /// </summary>
    public void SetProgressRatio(float ratio)
    {
        if (m_progressSlider != null) m_progressSlider.value = ratio;
    }

    /// <summary>
    /// [설명]: 모선 HP 슬라이더의 값을 설정합니다 (0.0 ~ 1.0).
    /// </summary>
    public void UpdateHpBar(float ratio)
    {
        if (m_hpSlider != null) m_hpSlider.value = ratio;
    }

    /// <summary>
    /// [설명]: 보스 HP 슬라이더의 값을 설정하고 가시성을 제어합니다 (0.0 ~ 1.0).
    /// </summary>
    public void UpdateBossHpBar(float ratio)
    {
        if (m_bossHpSlider != null)
        {
            m_bossHpSlider.gameObject.SetActive(ratio > 0);
            m_bossHpSlider.value = ratio;
        }
    }

    /// <summary>
    /// [설명]: 게임 오버 패널을 표시하고 게임을 일시정지합니다.
    /// </summary>
    public void ShowGameOver()
    {
        if (m_gameOverPanel != null) m_gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }
}
