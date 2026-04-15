using System;
using Cysharp.Threading.Tasks;
using SpaceCaptain.Player;
using SpaceCaptain.Models;
using UnityEngine;

public class SkillSlotViewModel : ISkillSlotViewModel
{
    public event Action<SkillSlotUIState> OnStateUpdated;

    public PlayerCharacterController Character { get; set; }
    public PlayerSwapManager SwapManager { get; set; }

    private SkillSlotUIState m_lastState;

    public void RefreshState()
    {
        if (Character == null)
        {
            return;
        }

        CharacterSwapStatusDTO statusDto = Character.GetStatusDTO();

        string swapText = string.Empty;
        if (statusDto.State == CharacterSwapState.Reserve)
        {
            if (statusDto.RemainingCooldown > 0f)
            {
                swapText = Mathf.CeilToInt(statusDto.RemainingCooldown).ToString();
            }
        }

        bool isAnimating = (SwapManager != null && SwapManager.IsAnimating);
        bool isSwapGlobalCooldown = (SwapManager != null && SwapManager.CurrentSwapCooldown > 0);
        bool isInteractable = statusDto.IsAvailable && !isAnimating && !isSwapGlobalCooldown;


        SkillSlotUIState currentState = new SkillSlotUIState(
            statusDto.CooldownRatio,
            swapText,
            statusDto.State,
            isInteractable
        );

        if (ShouldUpdate(m_lastState, currentState))
        {
            m_lastState = currentState;
            OnStateUpdated?.Invoke(currentState);
        }
    }

    private bool ShouldUpdate(SkillSlotUIState old, SkillSlotUIState current)
    {
        if (old.Status != current.Status)
        {
            return true;
        }

        if (old.IsInteractable != current.IsInteractable)
        {
            return true;
        }

        if (old.SwapText != current.SwapText)
        {
            return true;
        }

        if (Mathf.Abs(old.Cooldown - current.Cooldown) > 0.005f)
        {
            return true;
        }

        return false;
    }

    public void ExecuteAction()
    {
        if (Character == null || SwapManager == null)
        {
            return;
        }

        SwapManager.ExecuteCharacterActionAsync(Character).Forget();
    }
}
