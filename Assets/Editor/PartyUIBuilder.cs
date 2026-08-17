using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 파티 편성 UI의 프리팹과 씬 오브젝트를 만들고 직렬화 필드를 연결한다.
/// 계획서 Task 8의 수동 배선을 대신한다. 여러 번 돌려도 결과가 같다.
/// </summary>
public static class PartyUIBuilder
{
    private const string SCENE_PATH = "Assets/_Game/Scenes/Main.unity";
    private const string PREFAB_DIR = "Assets/_Game/Prefabs/UI";
    private const string PREFAB_PATH = PREFAB_DIR + "/CharacterSlot.prefab";

    private const string POPUP_NAME = "PartyPopup";
    private const string PARTY_BUTTON_NAME = "Party_Btn";
    private const string PARTY_BUTTON_PATH = "B_Btn_group/Button_2";

    private static readonly Color FIELD_COLOR = new Color(0.90f, 0.25f, 0.20f, 1f);
    private static readonly Color RESERVE_COLOR = new Color(0.30f, 0.80f, 0.35f, 1f);
    private static readonly Color EMPTY_COLOR = new Color(0.85f, 0.85f, 0.85f, 1f);

    [MenuItem("SpaceCaptain/파티 편성 UI 배선")]
    public static void Build()
    {
        var log = new StringBuilder();

        GameObject slotPrefab = BuildSlotPrefab(log);

        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        LobbyView lobbyView = Object.FindAnyObjectByType<LobbyView>(FindObjectsInactive.Include);
        LobbyInitializer initializer = Object.FindAnyObjectByType<LobbyInitializer>(FindObjectsInactive.Include);
        Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);

        if (lobbyView == null || initializer == null || canvas == null)
        {
            Debug.LogError($"[PartyUIBuilder] 필수 오브젝트 없음 - LobbyView:{lobbyView != null} LobbyInitializer:{initializer != null} Canvas:{canvas != null}");
            return;
        }

        log.AppendLine($"씬 로드: {SCENE_PATH}, Canvas='{canvas.name}'");

        PartyPopupView popup = BuildPopup(canvas.transform, slotPrefab, log);
        Button partyButton = BuildPartyButton(canvas.transform, log);

        Wire(lobbyView, "m_partyButton", partyButton, log);
        Wire(initializer, "m_partyPopupView", popup, log);

        var database = AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>("Assets/_Game/Resources/CharacterDatabase.asset");
        Wire(initializer, "m_characterDatabase", database, log);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[PartyUIBuilder] 배선 완료\n" + log);
        Verify();
    }

    // ---------- 프리팹 ----------

    private static GameObject BuildSlotPrefab(StringBuilder log)
    {
        Directory.CreateDirectory(PREFAB_DIR);

        var root = new GameObject("CharacterSlot", typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)root.transform;
        rect.sizeDelta = new Vector2(160f, 160f);

        var frame = root.GetComponent<Image>();
        frame.color = EMPTY_COLOR;

        var button = root.GetComponent<Button>();
        button.targetGraphic = frame;

        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(root.transform, false);
        Stretch((RectTransform)icon.transform, 8f);
        var iconImage = icon.GetComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        var label = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(root.transform, false);
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.offsetMin = new Vector2(0f, 0f);
        labelRect.offsetMax = new Vector2(0f, 32f);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22f;
        text.raycastTarget = false;
        text.color = Color.black;

        var view = root.AddComponent<CharacterSlotView>();
        Wire(view, "m_iconImage", iconImage, log);
        Wire(view, "m_frameImage", frame, log);
        Wire(view, "m_button", button, log);
        Wire(view, "m_nameText", text, log);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        Object.DestroyImmediate(root);

        log.AppendLine($"프리팹 생성: {PREFAB_PATH}");
        return prefab;
    }

    // ---------- 팝업 ----------

    private static PartyPopupView BuildPopup(Transform canvas, GameObject slotPrefab, StringBuilder log)
    {
        Transform existing = canvas.Find(POPUP_NAME);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
            log.AppendLine("기존 PartyPopup 제거 후 재생성");
        }

        var popupGo = new GameObject(POPUP_NAME, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        popupGo.transform.SetParent(canvas, false);
        Stretch((RectTransform)popupGo.transform, 0f);
        popupGo.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f, 0.96f);

        var combatPower = MakeText(popupGo.transform, "CombatPowerText", new Vector2(0f, 380f), new Vector2(400f, 70f), 44f);

        // 필드 슬롯 3 + 예비 슬롯 2. 배열 순서가 곧 인게임 역할이다.
        var slots = new CharacterSlotView[5];
        for (int i = 0; i < 3; i++)
        {
            slots[i] = MakeSlot(popupGo.transform, slotPrefab, $"FieldSlot_{i}", new Vector2((i - 1) * 180f, 140f));
        }
        for (int i = 0; i < 2; i++)
        {
            slots[3 + i] = MakeSlot(popupGo.transform, slotPrefab, $"ReserveSlot_{i}", new Vector2((i - 0.5f) * 180f, -60f));
        }

        Button autoArrange = MakeButton(popupGo.transform, "AutoArrangeButton", "자동편성", new Vector2(160f, -300f), new Vector2(220f, 80f));
        Button close = MakeButton(popupGo.transform, "CloseButton", "닫기", new Vector2(-160f, -300f), new Vector2(220f, 80f));

        // 선택 패널
        var selectPanel = new GameObject("SelectPanel", typeof(RectTransform), typeof(Image));
        selectPanel.transform.SetParent(popupGo.transform, false);
        Stretch((RectTransform)selectPanel.transform, 0f);
        selectPanel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f, 0.98f);

        var grid = new GameObject("GridContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        grid.transform.SetParent(selectPanel.transform, false);
        var gridRect = (RectTransform)grid.transform;
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(600f, 600f);
        gridRect.anchoredPosition = new Vector2(0f, 40f);
        var gridLayout = grid.GetComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(160f, 160f);
        gridLayout.spacing = new Vector2(20f, 20f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;

        Button selectClose = MakeButton(selectPanel.transform, "SelectCloseButton", "취소", new Vector2(0f, -340f), new Vector2(220f, 80f));

        var view = popupGo.AddComponent<PartyPopupView>();
        Wire(view, "m_slotViews", slots, log);
        Wire(view, "m_combatPowerText", combatPower, log);
        Wire(view, "m_autoArrangeButton", autoArrange, log);
        Wire(view, "m_closeButton", close, log);
        Wire(view, "m_selectPanel", selectPanel, log);
        Wire(view, "m_gridContainer", grid.transform, log);
        Wire(view, "m_cellPrefab", slotPrefab.GetComponent<CharacterSlotView>(), log);
        Wire(view, "m_selectCloseButton", selectClose, log);
        Wire(view, "m_fieldColor", FIELD_COLOR, log);
        Wire(view, "m_reserveColor", RESERVE_COLOR, log);
        Wire(view, "m_emptyColor", EMPTY_COLOR, log);

        selectPanel.SetActive(false);
        popupGo.SetActive(false);

        log.AppendLine("PartyPopup 생성 (슬롯 0~2 필드, 3~4 예비)");
        return view;
    }

    /// <summary>
    /// 편성 진입점은 하단 탭의 요원 버튼(B_Btn_group/Button_2)이다.
    /// 새 버튼을 만들지 않는다.
    /// </summary>
    private static Button BuildPartyButton(Transform canvas, StringBuilder log)
    {
        // 이전 버전이 만들어 둔 임시 버튼 정리
        Transform stale = canvas.Find(PARTY_BUTTON_NAME);
        if (stale != null)
        {
            Object.DestroyImmediate(stale.gameObject);
            log.AppendLine($"임시 버튼 {PARTY_BUTTON_NAME} 제거");
        }

        Transform target = canvas.Find(PARTY_BUTTON_PATH);

        if (target == null)
        {
            Debug.LogError($"[PartyUIBuilder] 편성 버튼을 찾을 수 없다: Canvas/{PARTY_BUTTON_PATH}");
            return null;
        }

        var button = target.GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError($"[PartyUIBuilder] {PARTY_BUTTON_PATH}에 Button 컴포넌트가 없다");
            return null;
        }

        log.AppendLine($"편성 진입점: Canvas/{PARTY_BUTTON_PATH}");
        return button;
    }

    // ---------- 조립 도우미 ----------

    private static CharacterSlotView MakeSlot(Transform parent, GameObject prefab, string name, Vector2 pos)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        var rect = (RectTransform)instance.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        return instance.GetComponent<CharacterSlotView>();
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.20f, 0.35f, 0.65f, 1f);
        go.GetComponent<Button>().targetGraphic = image;

        TextMeshProUGUI text = MakeText(go.transform, "Label", Vector2.zero, size, 30f);
        text.text = label;
        Stretch((RectTransform)text.transform, 0f);

        return go.GetComponent<Button>();
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private static void Wire(Object target, string fieldName, object value, StringBuilder log)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);

        if (prop == null)
        {
            Debug.LogError($"[PartyUIBuilder] 필드 없음: {target.GetType().Name}.{fieldName}");
            return;
        }

        if (value is Color color)
        {
            prop.colorValue = color;
        }
        else if (value is CharacterSlotView[] array)
        {
            prop.arraySize = array.Length;
            for (int i = 0; i < array.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
            }
        }
        else
        {
            prop.objectReferenceValue = (Object)value;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine($"  연결: {target.GetType().Name}.{fieldName}");
    }

    // ---------- 검증 ----------

    [MenuItem("SpaceCaptain/파티 편성 UI 배선 검증")]
    public static void Verify()
    {
        EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        var failures = new List<string>();

        PartyPopupView popup = Object.FindAnyObjectByType<PartyPopupView>(FindObjectsInactive.Include);
        LobbyView lobbyView = Object.FindAnyObjectByType<LobbyView>(FindObjectsInactive.Include);
        LobbyInitializer initializer = Object.FindAnyObjectByType<LobbyInitializer>(FindObjectsInactive.Include);

        if (popup == null)
        {
            failures.Add("PartyPopupView가 씬에 없다");
        }
        else
        {
            if (popup.GetComponent<CanvasGroup>() == null)
            {
                failures.Add("PartyPopup에 CanvasGroup이 없다 (Show/Hide 트윈이 전제한다)");
            }

            CheckRef(popup, "m_combatPowerText", failures);
            CheckRef(popup, "m_autoArrangeButton", failures);
            CheckRef(popup, "m_closeButton", failures);
            CheckRef(popup, "m_selectPanel", failures);
            CheckRef(popup, "m_gridContainer", failures);
            CheckRef(popup, "m_cellPrefab", failures);
            CheckRef(popup, "m_selectCloseButton", failures);
            CheckArray(popup, "m_slotViews", 5, failures);
        }

        if (lobbyView == null)
        {
            failures.Add("LobbyView가 씬에 없다");
        }
        else
        {
            CheckRef(lobbyView, "m_partyButton", failures);
        }

        if (initializer == null)
        {
            failures.Add("LobbyInitializer가 씬에 없다");
        }
        else
        {
            CheckRef(initializer, "m_partyPopupView", failures);
            CheckRef(initializer, "m_characterDatabase", failures);
        }

        if (failures.Count == 0)
        {
            Debug.Log("[PartyUIBuilder] 검증 통과: 배선 항목 전부 연결됨");
        }
        else
        {
            Debug.LogError("[PartyUIBuilder] 검증 실패:\n - " + string.Join("\n - ", failures));
        }
    }

    private static void CheckRef(Object target, string fieldName, List<string> failures)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);

        if (prop == null)
        {
            failures.Add($"{target.GetType().Name}.{fieldName} 필드가 존재하지 않는다");
        }
        else if (prop.objectReferenceValue == null)
        {
            failures.Add($"{target.GetType().Name}.{fieldName} 미연결");
        }
    }

    private static void CheckArray(Object target, string fieldName, int expected, List<string> failures)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);

        if (prop == null || !prop.isArray)
        {
            failures.Add($"{target.GetType().Name}.{fieldName} 배열이 아니다");
            return;
        }

        if (prop.arraySize != expected)
        {
            failures.Add($"{target.GetType().Name}.{fieldName} 길이 {prop.arraySize} (기대 {expected})");
            return;
        }

        for (int i = 0; i < expected; i++)
        {
            if (prop.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                failures.Add($"{target.GetType().Name}.{fieldName}[{i}] 미연결");
            }
        }
    }
}
