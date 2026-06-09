using SpaceCaptain.Models;

namespace SpaceCaptain.Player.Swap
{
    public interface IPlayerSwapContext
    {
        string GetCharacterName(string characterId);
        PlayerStatsDTO GetCharacterStats(string characterId);
    }
}
