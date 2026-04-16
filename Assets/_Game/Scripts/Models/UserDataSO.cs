using UnityEngine;

[CreateAssetMenu(fileName = "UserData", menuName = "SpaceCaptain/UserData")]
public class UserDataSO : ScriptableObject
{
    [SerializeField] private LobbyDataDTO m_lobbyData = new LobbyDataDTO();
    [SerializeField] private StageProgressDTO m_stageProgress = new StageProgressDTO();

    public LobbyDataDTO LobbyData => m_lobbyData;
    public StageProgressDTO StageProgress => m_stageProgress;

    public void SaveData()
    {
        // 향후 PlayerPrefs나 JSON 저장을 여기에 구현 
    }

    public void LoadData()
    {
        // 향후 데이터 로드 로직 구현 
    }
}
