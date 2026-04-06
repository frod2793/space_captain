using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Slider m_progressSlider;
    [SerializeField] private Slider m_hpSlider;
    [SerializeField] private Slider m_barrierSlider;
    [SerializeField] private Slider m_bossHpSlider;
    [SerializeField] private Slider m_expSlider;

    [SerializeField] private GameObject m_startPanel;
    [SerializeField] private GameObject m_gameOverPanel;
    [SerializeField] private GameObject m_upgradePanel;
    [SerializeField] private UpgradeButton[] m_upgradeButtons;

    [SerializeField] private TMP_Text m_killCountText;
    [SerializeField] private TMP_Text m_speedText;
    [SerializeField] private TMP_Text m_waveText;
    [SerializeField] private TMP_Text m_levelText;
    [SerializeField] private TMP_Text m_playTimeText;

    private MasterShip m_masterShip;
    private Barrier m_barrierSystem;
    private PlayerSwapManager m_swapManager;
    private EnemySpawner m_enemySpawner;

    private int m_killCount = 0;
    private int m_totalKillCount = 0;
    private int m_currentLevel = 0;
    private float m_currentSpeed = 1f;
    private float m_savedTimeScale = 1f;
    private float m_playTime = 0f;
    private bool m_isProcessingUpgrade = false;

    private void Start()
    {
        if (m_startPanel != null)
        {
            m_startPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        if (m_gameOverPanel != null)
        {
            m_gameOverPanel.SetActive(false);
        }
        
        if (m_upgradePanel != null)
        {
            m_upgradePanel.SetActive(false);
        }

        UpdateKillCountUI();
        UpdateLevelUI();
        UpdateExpUI(true);

        m_masterShip = FindAnyObjectByType<MasterShip>();
        if (m_masterShip != null)
        {
            m_masterShip.OnMasterShipDestroyed += ShowGameOver;
            m_masterShip.OnHpChanged += UpdateHpBar;
        }

        m_barrierSystem = FindAnyObjectByType<Barrier>();
        if (m_barrierSystem != null)
        {
            m_barrierSystem.OnBarrierChanged += UpdateBarrierBar;
        }

        m_swapManager = FindAnyObjectByType<PlayerSwapManager>();
        if (m_swapManager != null)
        {
            m_swapManager.OnAllPlayersDead += ShowGameOver;
        }

        m_enemySpawner = FindAnyObjectByType<EnemySpawner>();
        if (m_enemySpawner != null)
        {
            m_enemySpawner.OnWaveChanged += UpdateWaveText;
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

    private void Update()
    {
        if (Time.timeScale > 0)
        {
            m_playTime += Time.unscaledDeltaTime;
            UpdatePlayTimeUI();
        }
    }

    private void OnDestroy()
    {
        if (m_masterShip != null)
        {
            m_masterShip.OnMasterShipDestroyed -= ShowGameOver;
            m_masterShip.OnHpChanged -= UpdateHpBar;
        }

        if (m_barrierSystem != null)
        {
            m_barrierSystem.OnBarrierChanged -= UpdateBarrierBar;
        }

        if (m_swapManager != null)
        {
            m_swapManager.OnAllPlayersDead -= ShowGameOver;
        }

        if (m_enemySpawner != null)
        {
            m_enemySpawner.OnWaveChanged -= UpdateWaveText;
        }

        EnemyController.OnEnemyDead -= HandleEnemyDead;
    }

    private void HandleEnemyDead()
    {
        m_killCount++;
        m_totalKillCount++;

        UpdateKillCountUI();
        UpdateExpUI();

        int killsNeeded = (m_currentLevel + 1) * 5;
        if (m_killCount >= killsNeeded)
        {
            m_killCount = 0;
            m_currentLevel++;
            UpdateLevelUI();
            UpdateExpUI(true);
            ShowUpgradePanel();
        }
    }

    private void UpdateKillCountUI()
    {
        if (m_killCountText != null)
        {
            m_killCountText.text = $"잡은놈수: {m_totalKillCount}";
        }
    }

    private void UpdateLevelUI()
    {
        if (m_levelText != null)
        {
            m_levelText.text = $"LV.{m_currentLevel + 1}";
        }
    }

    private void UpdateExpUI(bool immediate = false)
    {
        if (m_expSlider == null)
        {
            return;
        }

        int killsNeeded = (m_currentLevel + 1) * 5;
        float ratio = (float)m_killCount / killsNeeded;

        m_expSlider.DOKill();
        if (immediate)
        {
            m_expSlider.value = ratio;
        }
        else
        {
            m_expSlider.DOValue(ratio, 0.3f).SetEase(Ease.OutQuad);
        }
    }

    private void UpdateWaveText(int wave)
    {
        if (m_waveText != null)
        {
            m_waveText.text = $"WAVE {wave}";
        }
    }

    private void UpdatePlayTimeUI()
    {
        if (m_playTimeText == null)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(m_playTime / 60f);
        int seconds = Mathf.FloorToInt(m_playTime % 60f);
        m_playTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
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
            m_savedTimeScale = Time.timeScale > 0 ? Time.timeScale : m_currentSpeed;
            m_upgradePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

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
        var targetCharacter = m_swapManager.Characters.Find(c => c.CharacterID.Equals(targetId, StringComparison.OrdinalIgnoreCase));

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
            Time.timeScale = m_savedTimeScale;
        }
    }

    public void OnStartButtonClicked()
    {
        if (m_startPanel != null)
        {
            m_startPanel.SetActive(false);
            Time.timeScale = m_currentSpeed;
        }
    }

    public void OnRetryButtonClicked()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void SetProgressRatio(float ratio)
    {
        if (m_progressSlider != null)
        {
            m_progressSlider.value = ratio;
        }
    }

    public void UpdateHpBar(float ratio)
    {
        if (m_hpSlider != null)
        {
            m_hpSlider.value = ratio;
        }
    }

    public void UpdateBarrierBar(float ratio)
    {
        if (m_barrierSlider != null)
        {
            m_barrierSlider.gameObject.SetActive(true);
            m_barrierSlider.value = ratio;
        }
    }

    public void UpdateBossHpBar(float ratio)
    {
        if (m_bossHpSlider != null)
        {
            m_bossHpSlider.gameObject.SetActive(ratio > 0);
            m_bossHpSlider.value = ratio;
        }
    }

    public void ShowGameOver()
    {
        if (m_gameOverPanel != null)
        {
            m_gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void ToggleBattleSpeed()
    {
        if (m_currentSpeed >= 2.5f)
        {
            m_currentSpeed = 1f;
        }
        else
        {
            m_currentSpeed += 0.5f;
        }

        if (Time.timeScale > 0)
        {
            Time.timeScale = m_currentSpeed;
        }
        else
        {
            m_savedTimeScale = m_currentSpeed;
        }

        if (m_speedText != null)
        {
            m_speedText.text = $"x{m_currentSpeed:F1}";
        }
    }

    public void UseGuidedMissileSkill()
    {
        if (m_masterShip != null)
        {
            m_masterShip.ExecuteGuidedMissile();
        }
    }
}