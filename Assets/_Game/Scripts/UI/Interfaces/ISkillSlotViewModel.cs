using System;
using SpaceCaptain.Player;

public readonly struct SkillSlotUIState
{
    public readonly float Cooldown;
    public readonly string SwapText;
    public readonly CharacterSwapState Status;
    public readonly bool IsInteractable;

    public SkillSlotUIState(float cooldown, string swapText, CharacterSwapState status, bool isInteractable)
    {
        Cooldown = cooldown;
        SwapText = swapText;
        Status = status;
        IsInteractable = isInteractable;
    }
}

public interface ISkillSlotViewModel
{
    event Action<SkillSlotUIState> OnStateUpdated;
    PlayerCharacterController Character { get; set; }
    PlayerSwapManager SwapManager { get; set; }
    void RefreshState();
    void ExecuteAction();
}
