using System;
using Cysharp.Threading.Tasks;
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

        bool isReserve = !Character.gameObject.activeSelf;
        float cooldownRatio = Character.Skill != null ? Character.Skill.CooldownRatio : 0f;
        
        string swapText = "";
        if (isReserve && Character.RemainingSwapCooldown > 0)
        {
            int currentCooldownInt = Mathf.CeilToInt(Character.RemainingSwapCooldown);
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
            m_cachedSwapText = "";
        }

        bool isReady = Character.Skill != null && Character.Skill.IsReady;
        bool isInteractable = Character.Stats.CurrentHp > 0 && (SwapManager == null || !SwapManager.IsAnimating);

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
