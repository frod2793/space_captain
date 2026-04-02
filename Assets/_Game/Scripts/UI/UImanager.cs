using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Slider m_progressSlider;
    [SerializeField] private Slider m_hpSlider;
    [SerializeField] private Slider m_bossHpSlider;
    [SerializeField] private GameObject m_startPanel;
    [SerializeField] private GameObject m_gameOverPanel;
    [SerializeField] private GameObject m_upgradePanel;
    [SerializeField] private UpgradeButton[] m_upgradeButtons;

    private int m_killCount = 0;
    private const int KILL_FOR_LEVEL_UP = 5;

    private void Start()
    {
        if (m_startPanel != null) m_startPanel.SetActive(true);
        if (m_gameOverPanel != null) m_gameOverPanel.SetActive(false);
        if (m_upgradePanel != null) m_upgradePanel.SetActive(false);
        
        Time.timeScale = 0f;

        var masterShip = Object.FindAnyObjectByType<MasterShip>();
        if (masterShip != null)
        {
            masterShip.OnMasterShipDestroyed += ShowGameOver;
            masterShip.OnHpChanged += UpdateHpBar;
        }

        var swapManager = Object.FindAnyObjectByType<PlayerSwapManager>();
        if (swapManager != null)
        {
            swapManager.OnAllPlayersDead += ShowGameOver;
        }

        EnemyController.OnEnemyDead += HandleEnemyDead;

        for (int i = 0; i < m_upgradeButtons.Length; i++)
        {
            m_upgradeButtons[i].Initialize(OnUpgradeSelected);
        }
    }

    private void OnDestroy()
    {
        EnemyController.OnEnemyDead -= HandleEnemyDead;
    }

    private void HandleEnemyDead()
    {
        m_killCount++;
        if (m_killCount >= KILL_FOR_LEVEL_UP)
        {
            m_killCount = 0;
            ShowUpgradePanel();
        }
    }

    private void ShowUpgradePanel()
    {
        m_isProcessingUpgrade = false; // 새 업그레이드 기회 제공

        var swapManager = Object.FindAnyObjectByType<PlayerSwapManager>();
        if (swapManager != null)
        {
            var characters = swapManager.Characters;
            if (characters != null)
            {
                for (int i = 0; i < characters.Count; i++)
                {
                    if (characters[i] != null) characters[i].PlayLevelUpEffect();
                }
            }
        }

        if (m_upgradePanel != null) m_upgradePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private bool m_isProcessingUpgrade = false;

    private void OnUpgradeSelected(int index)
    {
        if (m_isProcessingUpgrade) return;
        m_isProcessingUpgrade = true;

        var swapManager = Object.FindAnyObjectByType<PlayerSwapManager>();
        if (swapManager == null || swapManager.Characters == null)
        {
            m_isProcessingUpgrade = false;
            return;
        }

        string targetId = index == 0 ? "a" : (index == 1 ? "b" : "c");
        var targetCharacter = swapManager.Characters.Find(c => c.CharacterID.Equals(targetId, System.StringComparison.OrdinalIgnoreCase));
        
        if (targetCharacter == null)
        {
            m_isProcessingUpgrade = false;
            return;
        }

        var targetStats = targetCharacter.Stats;
        if (targetStats == null)
        {
            m_isProcessingUpgrade = false;
            return;
        }
        

        switch (index)
        {
            case 0:
                targetStats.BulletCountBonus++;
                break;
            case 1:
                targetStats.SpreadAngleBonus += 10f; 
                break;
            case 2:
                targetStats.SpreadAngleBonus -= 10f; 
                break;
        }
        
        if (m_upgradePanel != null) 
        {
            m_upgradePanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    public void OnStartButtonClicked()
    {
        if (m_startPanel != null) m_startPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnRetryButtonClicked()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

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
