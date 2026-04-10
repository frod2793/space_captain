using System;
using UnityEngine;

public interface ISkillSlotViewModel
{
    event Action<float, string, bool, bool, bool> OnStateUpdated;
    PlayerCharacterController Character { get; set; }
    PlayerSwapManager SwapManager { get; set; }
    void RefreshState();
    void ExecuteAction();
}
