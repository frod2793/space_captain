using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "SpaceCaptain/ItemDatabase")]
public class ItemDatabaseSO : ScriptableObject
{
    [SerializeField] private List<ItemDataDTO> m_items = new List<ItemDataDTO>();

    private Dictionary<string, ItemDataDTO> m_itemCache;

    /// <summary>
    /// 빠른 조회를 위해 Dictionary 캐시를 초기화합니다.
    /// </summary>
    private void InitializeCache()
    {
        if (m_itemCache == null || m_itemCache.Count != m_items.Count)
        {
            m_itemCache = m_items.ToDictionary(item => item.ItemId, item => item);
        }
    }

    /// <summary>
    /// ID를 통해 아이템 데이터를 가져옵니다.
    /// </summary>
    public ItemDataDTO GetItemData(string itemId)
    {
        InitializeCache();

        if (string.IsNullOrEmpty(itemId)) return null;

        if (m_itemCache.TryGetValue(itemId, out var data))
        {
            return data;
        }

        Debug.LogWarning($"[ItemDatabase] Item ID '{itemId}'를 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// 모든 아이템 리스트를 반환합니다.
    /// </summary>
    public IReadOnlyList<ItemDataDTO> GetAllItems() => m_items;
}
