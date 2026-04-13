using System;
using Cysharp.Threading.Tasks;
using SpaceCaptain.Player;
using UnityEngine;

public class SkillSlotViewModel : ISkillSlotViewModel
{
    public event Action<float, string, bool, bool, bool> OnStateUpdated;

    public PlayerCharacterController Character { get; set; }
    public PlayerSwapManager SwapManager { get; set; }

    private int m_lastSwapCooldownInt = -1;
    private string m_cachedSwapText = "";

    public void RefreshState()
    {
        if (Character == null || Character.Stats == null)
        {
            return;
        }

        var skill = Character.Skill;
        bool isReserve = Character.SwapState == CharacterSwapState.Reserve;
        float cooldownRatio = (skill != null) ? skill.CooldownRatio : 0f;
        
        string swapText = string.Empty;
        float remainingCooldown = Character.RemainingSwapCooldown;

        if (isReserve && remainingCooldown > 0)
        {
            int currentCooldownInt = Mathf.CeilToInt(remainingCooldown);
            if (currentCooldownInt != m_lastSwapCooldownInt)
            {
                m_lastSwapCooldownInt = currentCooldownInt;
                m_cachedSwapText = currentCooldownInt.ToString();
            }
            swapText = m_cachedSwapText;
        }
        else
        {
            m_lastSwapCooldownInt = -1;
            m_cachedSwapText = string.Empty;
        }

        bool isReady = (skill != null) && skill.IsReady;
        bool isAnimating = (SwapManager != null) && SwapManager.IsAnimating;
        bool isInteractable = (Character.SwapState != CharacterSwapState.Dead) && !isAnimating;

        OnStateUpdated?.Invoke(cooldownRatio, swapText, isReady, isInteractable, isReserve);
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
