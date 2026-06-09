using UnityEngine;
using SpaceCaptain.Models;

namespace SpaceCaptain.Player.Swap
{
    public interface ICharacterStatus
    {
        string CharacterID { get; }
        string CharacterName { get; }
        Sprite UI_Icon { get; }
        CharacterSwapStatusDTO GetStatusDTO();
        void PlayLevelUpEffect();
        void PlayCooldownFeedback();
    }
}
