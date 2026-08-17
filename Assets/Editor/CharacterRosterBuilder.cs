using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 캐릭터 로스터를 무기군 수에 맞춘다.
/// 이미 무기군 이름을 가진 캐릭터는 그 무기군에 고정하고,
/// 남은 무기군을 나머지 캐릭터에 순서대로 배정한 뒤 모자란 만큼만 새로 만든다. 멱등이다.
/// </summary>
public static class CharacterRosterBuilder
{
    /// <summary>기획서의 무기군 카드 9종. 이 순서가 로스터 순서다.</summary>
    private static readonly string[] WEAPON_GROUPS =
    {
        "소총",
        "샷건",
        "권총",
        "레이저",
        "저격총",
        "기관총",
        "검",
        "지팡이",
        "유탄 발사기",
    };

    private const string RESOURCE_DIR = "Assets/_Game/Resources";
    private const string DATABASE_PATH = RESOURCE_DIR + "/CharacterDatabase.asset";
    private const string TEMPLATE_PATH = RESOURCE_DIR + "/a_CharacterData.asset";

    [MenuItem("SpaceCaptain/캐릭터를 무기군 수에 맞추기")]
    public static void Build()
    {
        var log = new StringBuilder();

        var database = AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(DATABASE_PATH);
        var template = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(TEMPLATE_PATH);

        if (database == null || template == null)
        {
            Debug.LogError($"[CharacterRoster] DB 또는 템플릿 없음 - DB:{database != null} 템플릿:{template != null}");
            return;
        }

        var so = new SerializedObject(database);
        SerializedProperty list = so.FindProperty("m_characters");

        for (int i = list.arraySize - 1; i >= 0; i--)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                list.DeleteArrayElementAtIndex(i);
            }
        }

        var existing = new List<CharacterDataSO>();
        for (int i = 0; i < list.arraySize; i++)
        {
            existing.Add((CharacterDataSO)list.GetArrayElementAtIndex(i).objectReferenceValue);
        }

        log.AppendLine($"무기군 {WEAPON_GROUPS.Length}종, 기존 캐릭터 {existing.Count}종");

        // 이미 무기군 이름을 쓰는 캐릭터는 그 자리에 고정한다
        var assigned = new CharacterDataSO[WEAPON_GROUPS.Length];
        var unpinned = new List<CharacterDataSO>();

        foreach (CharacterDataSO character in existing)
        {
            int index = System.Array.IndexOf(WEAPON_GROUPS, character.CharacterName);

            if (index >= 0 && assigned[index] == null)
            {
                assigned[index] = character;
                log.AppendLine($"  고정: {character.CharacterID} = {character.CharacterName}");
            }
            else
            {
                unpinned.Add(character);
            }
        }

        var takenIds = new HashSet<string>(existing.Select(c => c.CharacterID));
        int cursor = 0;

        for (int i = 0; i < WEAPON_GROUPS.Length; i++)
        {
            if (assigned[i] != null)
            {
                continue;
            }

            if (cursor < unpinned.Count)
            {
                CharacterDataSO reused = unpinned[cursor++];
                string before = reused.CharacterName;
                Rename(reused, WEAPON_GROUPS[i]);
                assigned[i] = reused;
                log.AppendLine($"  이름 변경: {reused.CharacterID} '{before}' -> '{WEAPON_GROUPS[i]}'");
            }
            else
            {
                string id = NextId(takenIds);
                takenIds.Add(id);
                assigned[i] = CreateCharacter(id, WEAPON_GROUPS[i], template);
                log.AppendLine($"  추가: {id}_CharacterData = {WEAPON_GROUPS[i]}");
            }
        }

        list.arraySize = WEAPON_GROUPS.Length;
        for (int i = 0; i < WEAPON_GROUPS.Length; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = assigned[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.AppendLine($"최종 캐릭터 {list.arraySize}종");
        Debug.Log("[CharacterRoster] 완료\n" + log);
    }

    private static void Rename(CharacterDataSO character, string name)
    {
        var so = new SerializedObject(character);
        so.FindProperty("m_characterName").stringValue = name;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(character);
    }

    private static string NextId(HashSet<string> taken)
    {
        for (char c = 'a'; c <= 'z'; c++)
        {
            string candidate = c.ToString();

            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }

        return System.Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static CharacterDataSO CreateCharacter(string id, string name, CharacterDataSO template)
    {
        var data = ScriptableObject.CreateInstance<CharacterDataSO>();

        var so = new SerializedObject(data);
        var templateSo = new SerializedObject(template);

        so.FindProperty("m_characterID").stringValue = id;
        so.FindProperty("m_characterName").stringValue = name;

        // 프리팹/아이콘/스탯은 기존 캐릭터를 따른다. 무기군별 아트와 밸런스는 별도 작업.
        so.FindProperty("m_prefab").objectReferenceValue = templateSo.FindProperty("m_prefab").objectReferenceValue;
        so.FindProperty("m_uiIcon").objectReferenceValue = templateSo.FindProperty("m_uiIcon").objectReferenceValue;

        SerializedProperty stats = so.FindProperty("m_baseStats");
        SerializedProperty templateStats = templateSo.FindProperty("m_baseStats");
        stats.FindPropertyRelative("ID").stringValue = id;
        stats.FindPropertyRelative("MoveSpeed").floatValue = templateStats.FindPropertyRelative("MoveSpeed").floatValue;
        stats.FindPropertyRelative("AttackDamage").intValue = templateStats.FindPropertyRelative("AttackDamage").intValue;
        stats.FindPropertyRelative("MaxHp").intValue = templateStats.FindPropertyRelative("MaxHp").intValue;
        stats.FindPropertyRelative("CurrentHp").intValue = templateStats.FindPropertyRelative("MaxHp").intValue;
        stats.FindPropertyRelative("IsActive").boolValue = false;
        stats.FindPropertyRelative("Level").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(data, $"{RESOURCE_DIR}/{id}_CharacterData.asset");
        return data;
    }
}
