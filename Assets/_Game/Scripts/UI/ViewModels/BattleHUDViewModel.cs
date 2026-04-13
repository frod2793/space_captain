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
        get => m_swapManager; 
        set => m_swapManager = value; 
    }

    public BattleProgressDTO BattleData
    {
        get => m_progressData;
        set => m_progressData = value;
    }

    public event Action<int> OnTotalKillCountChanged;
    public event Action<int> OnLevelChanged;
    public event Action<float> OnExpRatioChanged;
    public event Action<int> OnWaveChanged;
    public event Action<float> OnPlayTimeChanged;
    public event Action<float> OnBattleSpeedChanged;
    public event Action OnShowUpgradePanel;
    public event Action OnHideUpgradePanel;
    public event Action OnShowGameOver;
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

        OnTotalKillCountChanged?.Invoke(m_progressData.TotalKillCount);

        int killsNeeded = (m_progressData.CurrentLevel + 1) * 5;
        float ratio = (float)m_progressData.CurrentLevelKillCount / killsNeeded;
        OnExpRatioChanged?.Invoke(ratio);

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
        OnPlayTimeChanged?.Invoke(m_progressData.PlayTime);
    }

    public void SetWave(int wave)
    {
        if (m_progressData == null)
        {
            return;
        }

        m_progressData.CurrentWave = wave;
        OnWaveChanged?.Invoke(wave);
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

        OnBattleSpeedChanged?.Invoke(m_progressData.BattleSpeed);
    }

    public void RequestGameOver()
    {
        OnShowGameOver?.Invoke();
    }

    public void SelectUpgrade(int index)
    {
        if (m_isProcessingUpgrade || m_swapManager == null)
        {
            return;
        }
        m_isProcessingUpgrade = true;

        string targetId = index == 0 ? "a" : (index == 1 ? "b" : "c");
        var targetCharacter = m_swapManager.Characters.Find(c => c.CharacterID.Equals(targetId, StringComparison.OrdinalIgnoreCase));

        if (targetCharacter != null && targetCharacter.Stats != null)
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

        m_isProcessingUpgrade = false;
        OnHideUpgradePanel?.Invoke();
    }

    public void NotifyShipHpChanged(float ratio)
    {
        OnShipHpChanged?.Invoke(ratio);
    }

    public void NotifyBarrierChanged(float ratio)
    {
        OnBarrierChanged?.Invoke(ratio);
    }

    public void NotifyBarrierValueWeightChanged(int current, int max)
    {
        OnBarrierValueWeightChanged?.Invoke(current, max);
    }

    public void ExecuteShipSkill(int index)
    {
        OnShipSkillExecuted?.Invoke(index);
    }

    private void LevelUp()
    {
        if (m_progressData == null)
        {
            return;
        }

        m_progressData.CurrentLevelKillCount = 0;
        m_progressData.CurrentLevel++;
        
        OnLevelChanged?.Invoke(m_progressData.CurrentLevel + 1);
        OnExpRatioChanged?.Invoke(0f);
        
        if (m_swapManager != null)
        {
            for (int i = 0; i < m_swapManager.Characters.Count; i++)
            {
                if (m_swapManager.Characters[i] != null)
                {
                    m_swapManager.Characters[i].PlayLevelUpEffect();
                }
            }
        }

        OnShowUpgradePanel?.Invoke();
    }
}
