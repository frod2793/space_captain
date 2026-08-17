using System;

public interface ILobbyViewModel
{
    string Nickname { get; }
    int Level { get; }
    int Gold { get; }
    int Diamond { get; }
    int CurrentStamina { get; }
    int MaxStamina { get; }
    int RequiredStamina { get; }

    string CurrentMapName { get; }
    int MaxWaveReached { get; }
    StageDifficulty SelectedDifficulty { get; }
    string DisplayStageName { get; }

    event Action OnDataChanged;
    event Action OnProfileOpenRequested;
    event Action OnPartyOpenRequested;

    void StartBattle();
    void OpenSettings();
    void OpenProfile();
    void OpenParty();
    void SelectDifficulty(StageDifficulty difficulty);
}
