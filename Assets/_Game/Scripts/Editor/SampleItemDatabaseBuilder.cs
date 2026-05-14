using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace SpaceCaptain.Editor
{
    public class SampleItemDatabaseBuilder
    {
        [MenuItem("Tools/SpaceCaptain/Generate Sample Item Database (Sheet)")]
        public static void BuildDatabaseFromSheet()
        {
            string sheetPath = "Assets/_Game/Art/Sample/item_Sheet.png";
            string dirPath = "Assets/_Game/Resources";
            string dbPath = dirPath + "/ItemDatabase.asset";
            
            // 1. Resources 폴더 확인
            if (!AssetDatabase.IsValidFolder(dirPath))
            {
                AssetDatabase.CreateFolder("Assets/_Game", "Resources");
            }

            // 2. SO 에셋 로드 또는 생성
            ItemDatabaseSO db = AssetDatabase.LoadAssetAtPath<ItemDatabaseSO>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<ItemDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            // 3. 스프라이트 시트에서 모든 서브 스프라이트 로드
            Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath);
            var sprites = assets.OfType<Sprite>().OrderBy(s => GetIndexFromName(s.name)).ToList();

            if (sprites.Count == 0)
            {
                Debug.LogError($"[ItemDatabase] '{sheetPath}'에서 스프라이트를 찾을 수 없습니다. Texture Type이 Sprite (2D and UI)이고 Sprite Mode가 Multiple인지 확인하십시오.");
                return;
            }

            // 4. 리플렉션을 통해 private 리스트 접근
            FieldInfo field = typeof(ItemDatabaseSO).GetField("m_items", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogError("[ItemDatabase] ItemDatabaseSO에서 'm_items' 필드를 찾을 수 없습니다.");
                return;
            }

            // 5. 데이터 매핑
            List<ItemDataDTO> newList = new List<ItemDataDTO>();
            for (int i = 0; i < sprites.Count; i++)
            {
                Sprite s = sprites[i];
                newList.Add(new ItemDataDTO
                {
                    ItemId = $"item_{i:D2}", // item_00, item_01 ...
                    ItemName = $"샘플 아이템 {i}",
                    ItemIcon = s,
                    Description = $"이것은 {s.name} 스프라이트를 사용하는 샘플 아이템입니다."
                });
            }

            // 6. 데이터 주입 및 저장
            field.SetValue(db, newList);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[ItemDatabase] '{sheetPath}'의 스프라이트 {sprites.Count}개를 기반으로 데이터베이스가 갱신되었습니다.\n위치: {dbPath}");
        }

        private static int GetIndexFromName(string name)
        {
            // "item_Sheet_12" 같은 이름에서 숫자 추출
            string[] parts = name.Split('_');
            if (parts.Length > 0 && int.TryParse(parts.Last(), out int result))
            {
                return result;
            }
            return 0;
        }
    }
}
