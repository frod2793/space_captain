using System;
using System.Collections.Generic;

public interface IPartyViewModel
{
    /// <summary>길이 5 고정. 빈칸은 null이며 항상 뒤쪽에만 연속으로 존재한다.</summary>
    IReadOnlyList<CharacterDataSO> Deck { get; }

    /// <summary>선택 그리드에 뿌릴 전체 캐릭터.</summary>
    IReadOnlyList<CharacterDataSO> AllCharacters { get; }

    int CombatPower { get; }

    /// <summary>선택 화면이 채울 슬롯. 선택 중이 아니면 -1.</summary>
    int PendingSlot { get; }

    void BeginSelect(int slot);
    void PickCharacter(string characterID);
    void ClearSlot(int slot);
    void CancelSelect();
    void AutoArrange();

    /// <summary>덱을 LobbyData에 반영하고 저장한다.</summary>
    void Commit();

    event Action OnDeckChanged;
    event Action OnSelectRequested;
    event Action OnSelectClosed;
}
