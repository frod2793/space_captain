using System;

namespace SpaceCaptain.Player.Swap
{
    public interface ISwapEvents
    {
        event Action OnCharactersInitialized;
        event Action<ICharacterStatus, ICharacterStatus> OnSwapStarted;
        event Action<ICharacterStatus> OnSwapCompleted;
        event Action<float> OnSwapCooldownChanged;
    }
}
