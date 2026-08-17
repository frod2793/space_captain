using System;
using System.Collections.Generic;
using UnityEngine;

public interface IGameResultViewModel
{
    bool IsClear { get; }
    Sprite MvpSprite { get; }
    string MvpCharacterName { get; }
    IReadOnlyDictionary<string, int> CharacterDamages { get; }
    IReadOnlyDictionary<string, Sprite> CharacterIcons { get; }
    IReadOnlyList<RewardItemDTO> StageRewards { get; }
    bool IsDamageLogExpanded { get; }

    event Action OnClaimDoubleReward;
    event Action OnBackToMain;
    event Action<bool> OnDamageLogToggled;

    void ClaimDoubleReward();
    void BackToMain();
    void ToggleDamageLog();
}
