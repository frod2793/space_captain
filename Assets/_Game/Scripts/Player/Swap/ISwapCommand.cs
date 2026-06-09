using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace SpaceCaptain.Player.Swap
{
    public interface ISwapCommand
    {
        bool IsAnimating { get; }
        float CurrentSwapCooldown { get; }
        float SwapDuration { get; }
        IReadOnlyList<ICharacterStatus> Characters { get; }
        UniTask ExecuteCharacterActionAsync(ICharacterStatus target);
    }
}
