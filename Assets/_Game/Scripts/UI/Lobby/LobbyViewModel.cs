using System;
using UnityEngine.SceneManagement;

public class LobbyViewModel : ILobbyViewModel
{
    private LobbyDataDTO m_lobbyData;
    private StageProgressDTO m_stageProgress;
    private ISceneLoader m_sceneLoader;

    public string Nickname => m_lobbyData.Nickname;
    public int Level => m_lobbyData.Level;
    public int Gold => m_lobbyData.Gold;
    public int Diamond => m_lobbyData.Diamond;
    public int CurrentStamina => m_lobbyData.CurrentStamina;
    public int MaxStamina => m_lobbyData.MaxStamina;

    public string CurrentMapName => m_stageProgress.CurrentMapName;
    public int MaxWaveReached => m_stageProgress.MaxWaveReached;

    public event Action OnDataChanged;

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
        if (m_lobbyData.CurrentStamina > 0)
        {
            m_lobbyData.CurrentStamina--;
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
}
