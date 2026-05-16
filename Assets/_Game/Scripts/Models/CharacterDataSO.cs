using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "SpaceCaptain/CharacterData")]
public class CharacterDataSO : ScriptableObject
{
    [SerializeField] private string m_characterID;
    [SerializeField] private string m_characterName;
    [SerializeField] private GameObject m_prefab;
    [SerializeField] private Sprite m_uiIcon;
    [SerializeField] private PlayerStatsDTO m_baseStats;

    public string CharacterID => m_characterID;
    public string CharacterName => m_characterName;
    public GameObject Prefab => m_prefab;
    public Sprite UI_Icon => m_uiIcon;
    public PlayerStatsDTO BaseStats => m_baseStats;
}
