using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Slider m_progressSlider;
    [SerializeField] private Slider m_hpSlider;
    [SerializeField] private Slider m_bossHpSlider;
    [SerializeField] private GameObject m_startPanel;
    [SerializeField] private GameObject m_gameOverPanel;
    [SerializeField] private GameObject m_upgradePanel;
    [SerializeField] private UpgradeButton[] m_upgradeButtons;

    [SerializeField] private TMP_Text m_killCountText;
 
    private MasterShip m_masterShip;
    private PlayerSwapManager m_swapManager;

    private int m_killCount = 0;
    private int m_totalKillCount = 0;
    private const int KILL_FOR_LEVEL_UP = 5;

    private void Start()
    {
        if (m_startPanel != null)
        {
            m_startPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        if (m_gameOverPanel != null) m_gameOverPanel.SetActive(false);
        if (m_upgradePanel != null) m_upgradePanel.SetActive(false);

        UpdateKillCountUI();

        m_masterShip = FindAnyObjectByType<MasterShip>();
        if (m_masterShip != null)
        {
            m_masterShip.OnMasterShipDestroyed += ShowGameOver;
            m_masterShip.OnHpChanged += UpdateHpBar;
        }

        m_swapManager = FindAnyObjectByType<PlayerSwapManager>();
        if (m_swapManager != null)
        {
            m_swapManager.OnAllPlayersDead += ShowGameOver;
        }

        EnemyController.OnEnemyDead += HandleEnemyDead;

        if (m_upgradeButtons != null)
        {
            for (int i = 0; i < m_upgradeButtons.Length; i++)
            {
                if (m_upgradeButtons[i] != null)
                {
                    m_upgradeButtons[i].Initialize(OnUpgradeSelected);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (m_masterShip != null)
        {
            m_masterShip.OnMasterShipDestroyed -= ShowGameOver;
            m_masterShip.OnHpChanged -= UpdateHpBar;
        }

        if (m_swapManager != null)
        {
            m_swapManager.OnAllPlayersDead -= ShowGameOver;
        }

        EnemyController.OnEnemyDead -= HandleEnemyDead;
    }

    private void HandleEnemyDead()
    {
        m_killCount++;
        m_totalKillCount++;

        UpdateKillCountUI();

        if (m_killCount >= KILL_FOR_LEVEL_UP)
        {
            m_killCount = 0;
            ShowUpgradePanel();
        }
    }


    private void UpdateKillCountUI()
    {
        m_killCountText.text = $"잡은놈수: {m_totalKillCount}";
    }

    private void ShowUpgradePanel()
    {
        m_isProcessingUpgrade = false;
        
        if (m_swapManager != null)
        {
            var characters = m_swapManager.Characters;
            if (characters != null)
            {
                for (int i = 0; i < characters.Count; i++)
                {
                    if (characters[i] != null)
                    {
                        characters[i].PlayLevelUpEffect();
                    }
                }
            }
        }

        if (m_upgradePanel != null)
        {
            m_upgradePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    private bool m_isProcessingUpgrade = false;

    private void OnUpgradeSelected(int index)
    {
        if (m_isProcessingUpgrade)
        {
            return;
        }

        m_isProcessingUpgrade = true;

        if (m_swapManager == null || m_swapManager.Characters == null)
        {
            m_isProcessingUpgrade = false;
            return;
        }

        string targetId = index == 0 ? "a" : (index == 1 ? "b" : "c");
        var targetCharacter =
            m_swapManager.Characters.Find(c => c.CharacterID.Equals(targetId, StringComparison.OrdinalIgnoreCase));

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
        if (m_startPanel != null)
        {
            m_startPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    public void OnRetryButtonClicked()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void SetProgressRatio(float ratio)
    {
        if (m_progressSlider != null)
        {
            m_progressSlider.value = ratio;
        }
    }

    /// <summary>
    /// [설명]: 모선 HP 슬라이더의 값을 설정합니다 (0.0 ~ 1.0).
    /// </summary>
    public void UpdateHpBar(float ratio)
    {
        if (m_hpSlider != null)
        {
            m_hpSlider.value = ratio;
        }
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
        if (m_gameOverPanel != null)
        {
            m_gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}