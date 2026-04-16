using System;

public interface ILobbyViewModel
{
    string Nickname { get; }
    int Level { get; }
    int Gold { get; }
    int Diamond { get; }
    int CurrentStamina { get; }
    int MaxStamina { get; }

    string CurrentMapName { get; }
    int MaxWaveReached { get; }

    event Action OnDataChanged;

    void StartBattle();
    void OpenSettings();
}
