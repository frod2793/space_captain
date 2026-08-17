using System;
using UnityEngine.SceneManagement;

public class LobbyViewModel : ILobbyViewModel
{
    private LobbyDataDTO m_lobbyData;
    private StageProgressDTO m_stageProgress;
    private ISceneLoader m_sceneLoader;
    private StageDifficulty m_selectedDifficulty = StageDifficulty.Normal;

    public string Nickname => m_lobbyData.Nickname;
    public int Level => m_lobbyData.Level;
    public int Gold => m_lobbyData.Gold;
    public int Diamond => m_lobbyData.Diamond;
    public int CurrentStamina => m_lobbyData.CurrentStamina;
    public int MaxStamina => m_lobbyData.MaxStamina;
    public int RequiredStamina => 1; // 기본 소모량

    public string CurrentMapName => m_stageProgress.CurrentMapName;
    public int MaxWaveReached => m_stageProgress.MaxWaveReached;
    public StageDifficulty SelectedDifficulty => m_selectedDifficulty;

    public string DisplayStageName 
    {
        get
        {
            string difficultyStr = m_selectedDifficulty == StageDifficulty.Normal ? "일반" : "정예";
            return $"{CurrentMapName} ({difficultyStr})";
        }
    }

    public event Action OnDataChanged;
    public event Action OnProfileOpenRequested;
    public event Action OnPartyOpenRequested;

    public void SetData(LobbyDataDTO lobbyData, StageProgressDTO stageProgress)
    {
        m_lobbyData = lobbyData;
        m_stageProgress = stageProgress;
        OnDataChanged?.Invoke();
    }

    public void SetSceneLoader(ISceneLoader sceneLoader)
    {
        m_sceneLoader = sceneLoader;
    }

    public void StartBattle()
    {
        if (m_lobbyData.CurrentStamina >= RequiredStamina)
        {
            m_lobbyData.CurrentStamina -= RequiredStamina;
            OnDataChanged?.Invoke();

            if (m_sceneLoader != null)
            {
                m_sceneLoader.LoadScene("InGame");
            }
            else
            {
                SceneManager.LoadScene("InGame");
            }
        }
    }

    public void OpenSettings()
    {
    }

    public void OpenProfile()
    {
        OnProfileOpenRequested?.Invoke();
    }

    public void OpenParty()
    {
        OnPartyOpenRequested?.Invoke();
    }

    public void SelectDifficulty(StageDifficulty difficulty)
    {
        if (m_selectedDifficulty != difficulty)
        {
            m_selectedDifficulty = difficulty;
            OnDataChanged?.Invoke();
        }
    }
}
