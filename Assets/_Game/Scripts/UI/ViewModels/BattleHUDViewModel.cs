using System;
using UnityEngine;

public class BattleHUDViewModel : IBattleHUDViewModel
{
    private BattleProgressDTO m_progressData;
    private PlayerSwapManager m_swapManager;
    private bool m_isProcessingUpgrade = false;

    public BattleProgressDTO Progress => m_progressData;

    public PlayerSwapManager SwapManager
    {
        get
        {
            return m_swapManager;
        }
        set
        {
            m_swapManager = value;
        }
    }

    public BattleProgressDTO BattleData
    {
        get
        {
            return m_progressData;
        }
        set
        {
            m_progressData = value;
        }
    }

    public event Action<int> OnTotalKillCountChanged;
    public event Action<int> OnLevelChanged;
    public event Action<float> OnExpRatioChanged;
    public event Action<int> OnWaveChanged;
    public event Action<float> OnPlayTimeChanged;
    public event Action<float> OnBattleSpeedChanged;
    public event Action OnShowUpgradePanel;
    public event Action<UpgradeOptionDTO[]> OnShowUpgradePanelWithOptions;
    public event Action OnHideUpgradePanel;
    public event Action OnShowGameOver;
    private bool m_isUpgradePanelActive = false;
    public event Action<float> OnShipHpChanged;
    public event Action<float> OnBarrierChanged;
    public event Action<int, int> OnBarrierValueWeightChanged;
    public event Action<int> OnShipSkillExecuted;

    public void AddKill()
    {
        if (m_progressData == null)
        {
            return;
        }

        m_progressData.TotalKillCount++;
        m_progressData.CurrentLevelKillCount++;

        if (OnTotalKillCountChanged != null)
        {
            OnTotalKillCountChanged.Invoke(m_progressData.TotalKillCount);
        }

        int killsNeeded = (m_progressData.CurrentLevel + 1) * 5;
        float ratio = (float)m_progressData.CurrentLevelKillCount / killsNeeded;
        
        if (OnExpRatioChanged != null)
        {
            OnExpRatioChanged.Invoke(ratio);
        }

        if (m_progressData.CurrentLevelKillCount >= killsNeeded)
        {
            LevelUp();
        }
    }

    public void UpdatePlayTime(float deltaTime)
    {
        if (m_progressData == null)
        {
            return;
        }

        m_progressData.PlayTime += deltaTime;
        
        if (OnPlayTimeChanged != null)
        {
            OnPlayTimeChanged.Invoke(m_progressData.PlayTime);
        }
    }

    public void SetWave(int wave)
    {
        if (m_progressData == null)
        {
            return;
        }

        m_progressData.CurrentWave = wave;
        
        if (OnWaveChanged != null)
        {
            OnWaveChanged.Invoke(wave);
        }
    }

    public void ToggleBattleSpeed()
    {
        if (m_progressData == null)
        {
            return;
        }

        if (m_progressData.BattleSpeed >= 2.5f)
        {
            m_progressData.BattleSpeed = 1f;
        }
        else
        {
            m_progressData.BattleSpeed += 0.5f;
        }

        if (OnBattleSpeedChanged != null)
        {
            OnBattleSpeedChanged.Invoke(m_progressData.BattleSpeed);
        }
    }

    public void RequestGameOver()
    {
        if (OnShowGameOver != null)
        {
            OnShowGameOver.Invoke();
        }
    }

    public void SelectUpgrade(int index)
    {
        if (m_isProcessingUpgrade || !m_isUpgradePanelActive || m_swapManager == null)
        {
            return;
        }
        m_isProcessingUpgrade = true;

        string targetId = (index == 0) ? "a" : (index == 1 ? "b" : "c");
        
        PlayerCharacterController targetCharacter = null;
        if (m_swapManager != null && m_swapManager.Characters != null)
        {
            for (int i = 0; i < m_swapManager.Characters.Count; i++)
            {
                if (m_swapManager.Characters[i].CharacterID.Equals(targetId, StringComparison.OrdinalIgnoreCase))
                {
                    targetCharacter = m_swapManager.Characters[i];
                    break;
                }
            }
        }

        if (targetCharacter != null)
        {
            if (targetCharacter.Stats != null)
            {
                switch (index)
                {
                    case 0:
                        targetCharacter.Stats.BulletCountBonus++;
                        break;
                    case 1:
                        targetCharacter.Stats.SpreadAngleBonus += 10f;
                        break;
                    case 2:
                        targetCharacter.Stats.SpreadAngleBonus -= 10f;
                        break;
                }
            }
        }

        m_isProcessingUpgrade = false;
        m_isUpgradePanelActive = false;
        
        if (OnHideUpgradePanel != null)
        {
            OnHideUpgradePanel.Invoke();
        }
    }

    public void NotifyShipHpChanged(float ratio)
    {
        if (OnShipHpChanged != null)
        {
            OnShipHpChanged.Invoke(ratio);
        }
    }

    public void NotifyBarrierChanged(float ratio)
    {
        if (OnBarrierChanged != null)
        {
            OnBarrierChanged.Invoke(ratio);
        }
    }

    public void NotifyBarrierValueWeightChanged(int current, int max)
    {
        if (OnBarrierValueWeightChanged != null)
        {
            OnBarrierValueWeightChanged.Invoke(current, max);
        }
    }

    public void ExecuteShipSkill(int index)
    {
        if (OnShipSkillExecuted != null)
        {
            OnShipSkillExecuted.Invoke(index);
        }
    }

    private void LevelUp()
    {
        if (m_progressData == null || m_isUpgradePanelActive)
        {
            return;
        }

        m_isUpgradePanelActive = true;
        m_progressData.CurrentLevelKillCount = 0;
        m_progressData.CurrentLevel++;

        if (OnLevelChanged != null)
        {
            OnLevelChanged.Invoke(m_progressData.CurrentLevel + 1);
        }

        if (OnExpRatioChanged != null)
        {
            OnExpRatioChanged.Invoke(0f);
        }

        if (m_swapManager != null)
        {
            UpgradeOptionDTO[] options = new UpgradeOptionDTO[3];

            for (int i = 0; i < 3; i++)
            {
                string targetId = (i == 0) ? "a" : (i == 1 ? "b" : "c");
                
                PlayerCharacterController character = null;
                if (m_swapManager.Characters != null)
                {
                    for (int j = 0; j < m_swapManager.Characters.Count; j++)
                    {
                        if (m_swapManager.Characters[j].CharacterID.Equals(targetId, StringComparison.OrdinalIgnoreCase))
                        {
                            character = m_swapManager.Characters[j];
                            break;
                        }
                    }
                }
                
                string title = (character != null) ? character.CharacterName : $"Character {targetId}";
                string description = string.Empty;

                switch (i)
                {
                    case 0:
                        description = "탄수 증가";
                        break;
                    case 1:
                        description = "사격 각도 확장";
                        break;
                    case 2:
                        description = "사격 각도 축소";
                        break;
                }

                if (character != null)
                {
                    character.PlayLevelUpEffect();
                }

                options[i] = new UpgradeOptionDTO
                {
                    Title = title,
                    Description = description,
                    CharacterIndex = i
                };
            }

            if (OnShowUpgradePanelWithOptions != null)
            {
                OnShowUpgradePanelWithOptions.Invoke(options);
            }
        }

        if (OnShowUpgradePanel != null)
        {
            OnShowUpgradePanel.Invoke();
        }
    }
}
