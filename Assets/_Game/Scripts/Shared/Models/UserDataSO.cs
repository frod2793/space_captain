using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UserData", menuName = "SpaceCaptain/UserData")]
public class UserDataSO : ScriptableObject
{
    private const string SAVE_KEY = "SpaceCaptain.Deck";
    private const char SEPARATOR = '\n';

    [SerializeField] private LobbyDataDTO m_lobbyData = new LobbyDataDTO();
    [SerializeField] private StageProgressDTO m_stageProgress = new StageProgressDTO();

    public LobbyDataDTO LobbyData => m_lobbyData;
    public StageProgressDTO StageProgress => m_stageProgress;

    /// <summary>
    /// 편성만 저장한다. LobbyData 전체를 저장하면 스태미나처럼
    /// 소모만 되고 회복 로직이 없는 값까지 굳어버려 전투 시작이 영구히 막힌다.
    /// </summary>
    public void SaveData()
    {
        PlayerPrefs.SetString(SAVE_KEY, string.Join(SEPARATOR.ToString(), m_lobbyData.DeckCharacters));
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        string saved = PlayerPrefs.GetString(SAVE_KEY, string.Empty);

        if (string.IsNullOrEmpty(saved))
        {
            return;
        }

        // 새 인스턴스를 대입하면 ViewModel이 들고 있는 참조가 끊기므로 제자리에서 채운다
        List<string> deck = m_lobbyData.DeckCharacters;
        deck.Clear();

        string[] ids = saved.Split(SEPARATOR);

        for (int i = 0; i < ids.Length; i++)
        {
            if (!string.IsNullOrEmpty(ids[i]))
            {
                deck.Add(ids[i]);
            }
        }
    }
}
