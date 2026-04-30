using System;
using System.Collections.Generic;
using UnityEngine;

public class GameResultViewModel : IGameResultViewModel
{
    private readonly GameResultDTO m_resultData;
    private bool m_isDamageLogExpanded;

    public bool IsClear => m_resultData.IsClear;
    public Sprite MvpSprite => m_resultData.MvpSprite;
    public string MvpCharacterName => m_resultData.MvpCharacterName;
    public IReadOnlyDictionary<string, int> CharacterDamages => m_resultData.CharacterDamages;
    public IReadOnlyList<RewardItemDTO> StageRewards => m_resultData.StageRewards;
    public bool IsDamageLogExpanded => m_isDamageLogExpanded;

    public event Action OnClaimDoubleReward;
    public event Action OnBackToMain;
    public event Action<bool> OnDamageLogToggled;

    public GameResultViewModel(GameResultDTO resultData)
    {
        m_resultData = resultData ?? new GameResultDTO();
        m_isDamageLogExpanded = false;
    }

    public void ClaimDoubleReward()
    {
        OnClaimDoubleReward?.Invoke();
    }

    public void BackToMain()
    {
        OnBackToMain?.Invoke();
    }

    public void ToggleDamageLog()
    {
        m_isDamageLogExpanded = !m_isDamageLogExpanded;
        OnDamageLogToggled?.Invoke(m_isDamageLogExpanded);
    }
}
