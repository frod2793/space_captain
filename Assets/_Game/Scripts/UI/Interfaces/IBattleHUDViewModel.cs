using System;
using SpaceCaptain.Player.Swap;

public interface IBattleHUDViewModel
{
    BattleProgressDTO Progress { get; }
    IPlayerSwapContext SwapContext { get; set; }
    BattleProgressDTO BattleData { get; set; }
    System.Collections.Generic.IReadOnlyDictionary<string, int> CharacterDamages { get; }

    event Action<int> OnTotalDamageChanged;
    event Action<int> OnLevelChanged;
    event Action<float> OnExpRatioChanged;
    event Action<string> OnCharacterLevelUpEffectRequested;
    event Action<int> OnWaveChanged;
    event Action<float> OnPlayTimeChanged;
    event Action<float> OnBattleSpeedChanged;
    event Action OnShowUpgradePanel;
    event Action<UpgradeOptionDTO[]> OnShowUpgradePanelWithOptions;
    event Action OnHideUpgradePanel;
    event Action OnShowGameOver;
    event Action<float> OnShipHpChanged;
    event Action<float> OnBarrierChanged;
    event Action<int, int> OnBarrierValueWeightChanged;
    event Action<int> OnShipSkillExecuted;

    void AddKill();
    void AddDamage(string damagerID, int amount);
    void UpdatePlayTime(float deltaTime);
    void SetWave(int wave);
    void ToggleBattleSpeed();
    void RequestGameOver();
    void SelectUpgrade(int index);
    void NotifyShipHpChanged(float ratio);
    void NotifyBarrierChanged(float ratio);
    void NotifyBarrierValueWeightChanged(int current, int max);
    void ExecuteShipSkill(int index);
}
