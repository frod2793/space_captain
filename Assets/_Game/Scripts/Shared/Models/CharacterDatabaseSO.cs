using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "SpaceCaptain/CharacterDatabase")]
public class CharacterDatabaseSO : ScriptableObject
{
    [SerializeField] private List<CharacterDataSO> m_characters = new List<CharacterDataSO>();
    
    private Dictionary<string, CharacterDataSO> m_characterCache;

    private void InitializeCache()
    {
        if (m_characterCache == null || m_characterCache.Count != m_characters.Count)
        {
            m_characterCache = m_characters
                .Where(c => c != null && !string.IsNullOrEmpty(c.CharacterID))
                .ToDictionary(c => c.CharacterID, c => c, System.StringComparer.OrdinalIgnoreCase);
        }
    }

    public CharacterDataSO GetCharacter(string characterID)
    {
        InitializeCache();
        
        if (m_characterCache.TryGetValue(characterID, out var data))
        {
            return data;
        }
        
        return null;
    }

    public List<CharacterDataSO> GetAllCharacters()
    {
        return new List<CharacterDataSO>(m_characters);
    }
}
