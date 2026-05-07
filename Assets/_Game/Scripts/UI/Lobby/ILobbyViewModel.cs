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
    StageDifficulty SelectedDifficulty { get; }
    string DisplayStageName { get; }

    event Action OnDataChanged;
    event Action OnProfileOpenRequested;

    void StartBattle();
    void OpenSettings();
    void OpenProfile();
    void SelectDifficulty(StageDifficulty difficulty);
}
