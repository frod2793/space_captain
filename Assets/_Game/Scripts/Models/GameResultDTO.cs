using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct RewardItemDTO
{
    public string ItemId;
    public int Amount;
    public Sprite ItemIcon;
}

[Serializable]
public class GameResultDTO
{
    public bool IsClear;
    public Sprite MvpSprite;
    public string MvpCharacterName;
    public Dictionary<string, int> CharacterDamages;
    public List<RewardItemDTO> StageRewards;

    public GameResultDTO()
    {
        CharacterDamages = new Dictionary<string, int>();
        StageRewards = new List<RewardItemDTO>();
    }
}
