# 로비 파티 편성 UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 로비에서 파티 5명(필드 3 + 예비 2)을 편성하고 그 결과를 저장하는 UI를 만든다.

**Architecture:** 편성 로직 전부를 MonoBehaviour가 아닌 `PartyViewModel`에 넣어 씬 없이 테스트한다. 뷰(`PartyPopupView`)는 이벤트를 받아 다시 그리기만 한다. 덱은 `List<string>` 하나이고 **인덱스가 곧 역할**(0=Active, 1~2=Standby, 3~4=Reserve)이라 별도 자료구조를 두지 않는다. 편성 화면에서 덱 중간에 빈칸이 생기지 않게 항상 앞으로 당겨(compact) 인게임의 스폰 순서와 일치시킨다.

**Tech Stack:** Unity 6000.3.19f1, C#, TextMeshPro, DOTween, NUnit (PlayMode 테스트), PlayerPrefs + `JsonUtility`

**설계 문서:** `docs/superpowers/specs/2026-08-17-lobby-party-ui-design.md`

## Global Constraints

- 새 패키지·새 의존성 추가 금지. 이미 들어있는 것(TMP, DOTween, UniTask)만 쓴다.
- 코딩 컨벤션은 기존 코드를 따른다: private 필드 `m_` 접두사, 중괄호는 항상 새 줄, `if` 본문 한 줄이어도 중괄호 사용, 주석은 한국어.
- `LobbyDataDTO` / `PlayerStatsDTO` / `CharacterDataSO`의 기존 필드를 변경하지 않는다. 새 필드도 추가하지 않는다.
- `BattleSceneInitializer.cs`는 이 계획에서 한 줄도 수정하지 않는다.
- 덱 크기는 5, 필드 크기는 3. 하드코딩된 숫자 대신 `PartyViewModel.DECK_SIZE` / `PartyViewModel.FIELD_SIZE` 상수를 쓴다.
- 전투력 공식은 `AttackDamage * 10 + MaxHp`. 계수 10은 `ATTACK_WEIGHT` 상수로 둔다.
- `Assets/_Game/Scripts/UI/Lobby/` 아래 파일은 네임스페이스를 쓰지 않는다(기존 Lobby 파일들이 전역 네임스페이스다).
- 새 `.cs` 파일을 만들면 Unity가 `.meta`를 생성한다. 커밋할 때 `.meta`도 같이 `git add` 한다.
- **테스트에서 게임 코드 타입을 직접 참조하지 않는다.** `Game.Tests.asmdef`는 `Assembly-CSharp`를 참조할 수 없다(Unity에서 asmdef → 기본 어셈블리 참조는 불가능). `UserDataSO`, `PartyViewModel`, `CharacterDataSO` 같은 타입은 전부 `TestReflectionHelper.GetGameType(...)`으로 얻고 리플렉션으로 다룬다. 기존 `CharacterSystemTests` / `UserDataSaveTests`가 같은 방식이다.

## 실행 환경

전 태스크에서 아래 두 변수를 쓴다. 각 셸 세션에서 한 번 설정한다.

```bash
export UNITY="/Users/woodenshield/Desktop/UNITY/Editor/6000.3.19f1/Unity.app/Contents/MacOS/Unity"
export PROJ="/Users/woodenshield/Desktop/UNITY/Projects/space_captain/space_captain-LobbyParty-2"
```

**중요:** batchmode 명령은 Unity 에디터 GUI가 이 프로젝트를 열고 있으면 `Library` 락 때문에 실패한다. 실행 전 에디터를 닫는다.

**테스트 실행:**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "<클래스명>" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "Test(s)? (run|Suite)|Failed|Passed|error CS"
```

`-runTests`는 자동으로 종료하므로 `-quit`을 붙이지 않는다.

**컴파일만 확인:**

```bash
"$UNITY" -batchmode -quit -projectPath "$PROJ" -logFile - 2>&1 | grep -E "error CS" ; echo "exit=$?"
```

`grep`이 아무것도 못 찾으면 `exit=1`이 뜬다. **이게 성공 신호다.**

---

### Task 1: UserDataSO 저장/로드 ✅ 완료

`SaveData`/`LoadData`가 빈 스텁이라 편성 결과가 앱 재시작 시 사라진다. 다른 태스크가 이걸 호출하므로 먼저 만든다.

**Files:**
- Modify: `Assets/_Game/Scripts/Models/UserDataSO.cs:12-20`
- Create: `Assets/_Game/Tests/PlayMode/UserDataSaveTests.cs`

**Interfaces:**
- Consumes: 없음
- Produces: `UserDataSO.SaveData()` — `LobbyData`를 PlayerPrefs에 JSON으로 저장. `UserDataSO.LoadData()` — 저장본이 있으면 기존 `LobbyData` 인스턴스에 덮어쓴다(참조 유지). 키는 `"SpaceCaptain.LobbyData"`.

- [x] **Step 1: 실패하는 테스트 작성** — 완료 (커밋 `231e117`)

실제 구현본: `Assets/_Game/Tests/PlayMode/UserDataSaveTests.cs`

테스트 3개: 저장 후 로드 시 덱 복원 / `LoadData`가 `LobbyData` 인스턴스 참조를 유지 /
저장본이 없으면 기존 값을 건드리지 않음.

**계획서 초안은 게임 타입을 직접 참조했으나 그대로는 컴파일되지 않아 리플렉션으로 바꿔 구현했다.**
`Game.Tests.asmdef`는 `Assembly-CSharp`를 참조할 수 없다 (Global Constraints 참조).
Task 2 이후의 테스트 코드는 이 제약을 반영해 이미 리플렉션 방식으로 적혀 있다.

- [x] **Step 2: 테스트가 실패하는지 확인** — 완료

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "UserDataSaveTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "Test(s)? (run|Suite)|Failed|Passed|error CS"
```

기대: `SaveData_저장후_LoadData하면_덱이_복원된다`와 `LoadData는_LobbyData_인스턴스_참조를_유지한다`가 FAIL. `SaveData`가 아무것도 안 하므로 복원될 값이 없다. 세 번째 테스트는 이미 PASS다(빈 스텁이라 아무것도 안 건드림).

- [x] **Step 3: 구현** — 완료. 아래 코드 그대로 반영됨:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "UserData", menuName = "SpaceCaptain/UserData")]
public class UserDataSO : ScriptableObject
{
    private const string SAVE_KEY = "SpaceCaptain.LobbyData";

    [SerializeField] private LobbyDataDTO m_lobbyData = new LobbyDataDTO();
    [SerializeField] private StageProgressDTO m_stageProgress = new StageProgressDTO();

    public LobbyDataDTO LobbyData => m_lobbyData;
    public StageProgressDTO StageProgress => m_stageProgress;

    public void SaveData()
    {
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(m_lobbyData));
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, string.Empty);

        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        // 새 인스턴스를 대입하면 ViewModel이 들고 있는 참조가 끊기므로 덮어쓴다
        JsonUtility.FromJsonOverwrite(json, m_lobbyData);
    }
}
```

- [x] **Step 4: 테스트 통과 확인** — 완료. 독립 재실행으로 교차 검증함: `UserDataSaveTests` 3/3 Passed, 전체 스위트 26/26 Passed.

- [x] **Step 5: 커밋** — 완료 (`231e117`)

```bash
git add Assets/_Game/Scripts/Models/UserDataSO.cs \
        Assets/_Game/Tests/PlayMode/UserDataSaveTests.cs \
        Assets/_Game/Tests/PlayMode/UserDataSaveTests.cs.meta
git commit -m "UserDataSO 저장/로드를 PlayerPrefs로 구현"
```

---

### Task 2: PartyViewModel 골격과 덱 로드 ✅ 완료 (`3f43e63`)

편성 로직의 뼈대. 인터페이스를 먼저 확정하고, 저장된 덱을 5칸 배열로 읽어오는 것까지 만든다.

**Files:**
- Create: `Assets/_Game/Scripts/UI/Lobby/IPartyViewModel.cs`
- Create: `Assets/_Game/Scripts/UI/Lobby/PartyViewModel.cs`
- Create: `Assets/_Game/Tests/PlayMode/PartyViewModelTests.cs`

**Interfaces:**
- Consumes: Task 1의 `UserDataSO.SaveData()`
- Produces:
  - `IPartyViewModel` — 아래 Step 3의 시그니처 그대로. Task 6의 `PartyPopupView`가 소비한다.
  - `PartyViewModel.DECK_SIZE` (`public const int`, 값 5), `PartyViewModel.FIELD_SIZE` (`public const int`, 값 3)
  - `PartyViewModel.SetData(UserDataSO userData, CharacterDatabaseSO database)` — Task 7의 `LobbyInitializer`가 호출한다.
  - 테스트 헬퍼 — Task 3·4가 이어서 쓴다. 전부 리플렉션 기반이라 반환형이 `object`다:
    `MakeCharacter(string, int, int)`, `SetupDatabase(params object[])`, `MakeViewModel(List<string>)`,
    `DeckIds(object)` → `List<string>`, `SavedDeckIds()` → `List<string>`,
    `GetProp(object, string)`, `Invoke(object, string, params object[])`, `AddHandler(object, string, Action)`, `CountOf(object)`

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/_Game/Tests/PlayMode/PartyViewModelTests.cs`:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PartyViewModelTests
{
    private const string SAVE_KEY = "SpaceCaptain.LobbyData";

    private const BindingFlags ANY_INSTANCE =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private Type m_characterDataType;
    private Type m_statsType;
    private Type m_partyViewModelType;

    private object m_userData;   // UserDataSO
    private object m_database;   // CharacterDatabaseSO

    private readonly List<UnityEngine.Object> m_created = new List<UnityEngine.Object>();

    // ---------- 리플렉션 헬퍼 ----------
    // Game.Tests.asmdef는 Assembly-CSharp를 참조할 수 없다.
    // 게임 코드 타입은 전부 TestReflectionHelper로 얻어 리플렉션으로 다룬다.

    private static object GetProp(object target, string name)
    {
        PropertyInfo prop = target.GetType().GetProperty(name, ANY_INSTANCE);
        Assert.IsNotNull(prop, $"프로퍼티 {name}을 찾을 수 없다");
        return prop.GetValue(target);
    }

    private static object GetField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, ANY_INSTANCE);
        Assert.IsNotNull(field, $"필드 {name}을 찾을 수 없다");
        return field.GetValue(target);
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, ANY_INSTANCE);
        Assert.IsNotNull(field, $"필드 {name}을 찾을 수 없다");
        field.SetValue(target, value);
    }

    private static object Invoke(object target, string name, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(name, ANY_INSTANCE);
        Assert.IsNotNull(method, $"메서드 {name}을 찾을 수 없다");
        return method.Invoke(target, args);
    }

    private static void AddHandler(object target, string eventName, Action handler)
    {
        EventInfo evt = target.GetType().GetEvent(eventName, ANY_INSTANCE);
        Assert.IsNotNull(evt, $"이벤트 {eventName}을 찾을 수 없다");
        evt.AddEventHandler(target, handler);
    }

    private static int CountOf(object enumerable)
    {
        int count = 0;
        foreach (object unused in (IEnumerable)enumerable)
        {
            count++;
        }
        return count;
    }

    // ---------- 테스트 픽스처 ----------

    /// <summary>Deck을 CharacterID 리스트로 읽는다. 빈칸은 null.</summary>
    private static List<string> DeckIds(object viewModel)
    {
        var ids = new List<string>();

        foreach (object item in (IEnumerable)GetProp(viewModel, "Deck"))
        {
            ids.Add(item == null ? null : (string)GetProp(item, "CharacterID"));
        }

        return ids;
    }

    /// <summary>UserDataSO.LobbyData.DeckCharacters 실체.</summary>
    private List<string> SavedDeckIds()
    {
        return (List<string>)GetField(GetProp(m_userData, "LobbyData"), "DeckCharacters");
    }

    private object MakeCharacter(string id, int attack, int hp)
    {
        object data = ScriptableObject.CreateInstance(m_characterDataType);
        object stats = Activator.CreateInstance(m_statsType);

        SetField(stats, "ID", id);
        SetField(stats, "AttackDamage", attack);
        SetField(stats, "MaxHp", hp);
        SetField(stats, "CurrentHp", hp);

        SetField(data, "m_characterID", id);
        SetField(data, "m_characterName", id.ToUpper());
        SetField(data, "m_baseStats", stats);

        m_created.Add((UnityEngine.Object)data);
        return data;
    }

    private void SetupDatabase(params object[] characters)
    {
        Type listType = typeof(List<>).MakeGenericType(m_characterDataType);
        object list = Activator.CreateInstance(listType);
        MethodInfo add = listType.GetMethod("Add");

        for (int i = 0; i < characters.Length; i++)
        {
            add.Invoke(list, new[] { characters[i] });
        }

        SetField(m_database, "m_characters", list);
    }

    private object MakeViewModel(List<string> savedDeck)
    {
        List<string> deck = SavedDeckIds();
        deck.Clear();

        if (savedDeck != null)
        {
            deck.AddRange(savedDeck);
        }

        object viewModel = Activator.CreateInstance(m_partyViewModelType);
        Invoke(viewModel, "SetData", m_userData, m_database);
        return viewModel;
    }

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);

        m_characterDataType = TestReflectionHelper.GetGameType("CharacterDataSO");
        m_statsType = TestReflectionHelper.GetGameType("PlayerStatsDTO");
        m_partyViewModelType = TestReflectionHelper.GetGameType("PartyViewModel");

        Assert.IsNotNull(m_characterDataType, "CharacterDataSO 타입을 찾을 수 없다");
        Assert.IsNotNull(m_statsType, "PlayerStatsDTO 타입을 찾을 수 없다");
        Assert.IsNotNull(m_partyViewModelType, "PartyViewModel 타입을 찾을 수 없다");

        m_userData = ScriptableObject.CreateInstance(TestReflectionHelper.GetGameType("UserDataSO"));
        m_database = ScriptableObject.CreateInstance(TestReflectionHelper.GetGameType("CharacterDatabaseSO"));

        m_created.Add((UnityEngine.Object)m_userData);
        m_created.Add((UnityEngine.Object)m_database);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);

        for (int i = 0; i < m_created.Count; i++)
        {
            UnityEngine.Object.DestroyImmediate(m_created[i]);
        }

        m_created.Clear();
    }

    // ---------- 테스트 ----------

    [Test]
    public void SetData하면_덱은_항상_5칸이다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100), MakeCharacter("b", 20, 200));
        object viewModel = MakeViewModel(new List<string> { "a", "b" });

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual(5, deck.Count);
        Assert.AreEqual("a", deck[0]);
        Assert.AreEqual("b", deck[1]);
        Assert.IsNull(deck[2]);
        Assert.IsNull(deck[3]);
        Assert.IsNull(deck[4]);
    }

    [Test]
    public void SetData하면_DB_전체가_AllCharacters로_노출된다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100), MakeCharacter("b", 20, 200), MakeCharacter("c", 30, 300));
        object viewModel = MakeViewModel(null);

        Assert.AreEqual(3, CountOf(GetProp(viewModel, "AllCharacters")));
    }

    [Test]
    public void DB에_없는_ID는_덱에서_무시된다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(new List<string> { "없는놈", "a" });

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual("a", deck[0], "빈칸 없이 앞으로 당겨져야 한다");
        Assert.IsNull(deck[1]);
    }

    [Test]
    public void 저장된_덱이_5개를_넘으면_앞에서_5개만_읽는다()
    {
        SetupDatabase(
            MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1),
            MakeCharacter("d", 1, 1), MakeCharacter("e", 1, 1), MakeCharacter("f", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c", "d", "e", "f" });

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual(5, deck.Count);
        Assert.AreEqual("e", deck[4]);
    }

    [Test]
    public void PendingSlot_초기값은_음수1이다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(null);

        Assert.AreEqual(-1, (int)GetProp(viewModel, "PendingSlot"));
    }

    [Test]
    public void BeginSelect하면_PendingSlot이_바뀌고_이벤트가_발생한다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(null);

        int requested = 0;
        AddHandler(viewModel, "OnSelectRequested", () => requested++);

        Invoke(viewModel, "BeginSelect", 3);

        Assert.AreEqual(3, (int)GetProp(viewModel, "PendingSlot"));
        Assert.AreEqual(1, requested);
    }

    [Test]
    public void 범위를_벗어난_BeginSelect는_무시된다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(null);

        Invoke(viewModel, "BeginSelect", 5);
        Assert.AreEqual(-1, (int)GetProp(viewModel, "PendingSlot"));

        Invoke(viewModel, "BeginSelect", -2);
        Assert.AreEqual(-1, (int)GetProp(viewModel, "PendingSlot"));
    }

    [Test]
    public void CancelSelect하면_PendingSlot이_풀리고_닫힘이벤트가_발생한다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(null);

        int closed = 0;
        AddHandler(viewModel, "OnSelectClosed", () => closed++);

        Invoke(viewModel, "BeginSelect", 1);
        Invoke(viewModel, "CancelSelect");

        Assert.AreEqual(-1, (int)GetProp(viewModel, "PendingSlot"));
        Assert.AreEqual(1, closed);
    }
}
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "PartyViewModelTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "Test(s)? (run|Suite)|Failed|Passed|error CS"
```

기대: `error CS0246: The type or namespace name 'PartyViewModel' could not be found` — 컴파일 실패. 테스트가 아예 안 돈다.

- [ ] **Step 3: 인터페이스 작성**

`Assets/_Game/Scripts/UI/Lobby/IPartyViewModel.cs`:

```csharp
using System;
using System.Collections.Generic;

public interface IPartyViewModel
{
    /// <summary>길이 5 고정. 빈칸은 null이며 항상 뒤쪽에만 연속으로 존재한다.</summary>
    IReadOnlyList<CharacterDataSO> Deck { get; }

    /// <summary>선택 그리드에 뿌릴 전체 캐릭터.</summary>
    IReadOnlyList<CharacterDataSO> AllCharacters { get; }

    int CombatPower { get; }

    /// <summary>선택 화면이 채울 슬롯. 선택 중이 아니면 -1.</summary>
    int PendingSlot { get; }

    void BeginSelect(int slot);
    void PickCharacter(string characterID);
    void ClearSlot(int slot);
    void CancelSelect();
    void AutoArrange();

    /// <summary>덱을 LobbyData에 반영하고 저장한다.</summary>
    void Commit();

    event Action OnDeckChanged;
    event Action OnSelectRequested;
    event Action OnSelectClosed;
}
```

- [ ] **Step 4: PartyViewModel 골격 작성**

`Assets/_Game/Scripts/UI/Lobby/PartyViewModel.cs`:

```csharp
using System;
using System.Collections.Generic;

public class PartyViewModel : IPartyViewModel
{
    public const int DECK_SIZE = 5;
    public const int FIELD_SIZE = 3;

    private const int ATTACK_WEIGHT = 10;

    private readonly List<CharacterDataSO> m_deck =
        new List<CharacterDataSO> { null, null, null, null, null };
    private readonly List<CharacterDataSO> m_allCharacters = new List<CharacterDataSO>();

    private UserDataSO m_userData;
    private CharacterDatabaseSO m_database;
    private int m_pendingSlot = -1;

    public IReadOnlyList<CharacterDataSO> Deck => m_deck;
    public IReadOnlyList<CharacterDataSO> AllCharacters => m_allCharacters;
    public int PendingSlot => m_pendingSlot;

    public int CombatPower
    {
        get
        {
            int total = 0;

            for (int i = 0; i < m_deck.Count; i++)
            {
                total += GetPower(m_deck[i]);
            }

            return total;
        }
    }

    public event Action OnDeckChanged;
    public event Action OnSelectRequested;
    public event Action OnSelectClosed;

    public void SetData(UserDataSO userData, CharacterDatabaseSO database)
    {
        m_userData = userData;
        m_database = database;

        m_allCharacters.Clear();

        if (m_database != null)
        {
            List<CharacterDataSO> all = m_database.GetAllCharacters();

            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null)
                {
                    m_allCharacters.Add(all[i]);
                }
            }
        }

        for (int i = 0; i < DECK_SIZE; i++)
        {
            m_deck[i] = null;
        }

        List<string> saved = m_userData != null && m_userData.LobbyData != null
            ? m_userData.LobbyData.DeckCharacters
            : null;

        if (saved != null)
        {
            int slot = 0;

            for (int i = 0; i < saved.Count && slot < DECK_SIZE; i++)
            {
                CharacterDataSO data = m_database != null ? m_database.GetCharacter(saved[i]) : null;

                if (data != null)
                {
                    m_deck[slot] = data;
                    slot++;
                }
            }
        }

        m_pendingSlot = -1;
        OnDeckChanged?.Invoke();
    }

    public void BeginSelect(int slot)
    {
        if (slot < 0 || slot >= DECK_SIZE)
        {
            return;
        }

        m_pendingSlot = slot;
        OnSelectRequested?.Invoke();
    }

    public void CancelSelect()
    {
        m_pendingSlot = -1;
        OnSelectClosed?.Invoke();
    }

    public void PickCharacter(string characterID)
    {
    }

    public void ClearSlot(int slot)
    {
    }

    public void AutoArrange()
    {
    }

    public void Commit()
    {
    }

    private static int GetPower(CharacterDataSO data)
    {
        if (data == null || data.BaseStats == null)
        {
            return 0;
        }

        return data.BaseStats.AttackDamage * ATTACK_WEIGHT + data.BaseStats.MaxHp;
    }
}
```

`PickCharacter` / `ClearSlot` / `AutoArrange` / `Commit`은 Task 3·4에서 채운다. 지금은 컴파일만 되면 된다.

- [ ] **Step 5: 테스트 통과 확인**

Step 2와 같은 명령. 기대: 8개 모두 PASS.

- [ ] **Step 6: 커밋**

```bash
git add Assets/_Game/Scripts/UI/Lobby/IPartyViewModel.cs \
        Assets/_Game/Scripts/UI/Lobby/IPartyViewModel.cs.meta \
        Assets/_Game/Scripts/UI/Lobby/PartyViewModel.cs \
        Assets/_Game/Scripts/UI/Lobby/PartyViewModel.cs.meta \
        Assets/_Game/Tests/PlayMode/PartyViewModelTests.cs \
        Assets/_Game/Tests/PlayMode/PartyViewModelTests.cs.meta
git commit -m "PartyViewModel 골격과 덱 로드 구현"
```

---

### Task 3: 배치 · 교환 · 해제 · 당기기 ✅ 완료 (`18307d8`)

편성의 핵심. 중복 방지는 별도 검증 없이 **자리 교환**으로 흡수하고, 빈칸은 항상 앞으로 당긴다.

**Files:**
- Modify: `Assets/_Game/Scripts/UI/Lobby/PartyViewModel.cs` (Task 2에서 비워둔 `PickCharacter`, `ClearSlot`)
- Modify: `Assets/_Game/Tests/PlayMode/PartyViewModelTests.cs` (테스트 추가)

**Interfaces:**
- Consumes: Task 2의 `PartyViewModel`과 테스트 헬퍼 전부 (`MakeCharacter`, `SetupDatabase`, `MakeViewModel`, `DeckIds`, `GetProp`, `Invoke`, `AddHandler`)
- Produces: 동작이 확정된 `PickCharacter(string)` / `ClearSlot(int)`. Task 6의 뷰가 이 둘만 호출한다.

- [ ] **Step 1: 실패하는 테스트 추가**

`PartyViewModelTests.cs`의 마지막 `}` 바로 앞에 아래를 붙인다:

```csharp
    [Test]
    public void 빈슬롯에_미편성_캐릭터를_고르면_그_슬롯에_들어간다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100), MakeCharacter("b", 20, 200));
        object viewModel = MakeViewModel(new List<string> { "a" });

        Invoke(viewModel, "BeginSelect", 1);
        Invoke(viewModel, "PickCharacter", "b");

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual("a", deck[0]);
        Assert.AreEqual("b", deck[1]);
        Assert.AreEqual(-1, (int)GetProp(viewModel, "PendingSlot"));
    }

    [Test]
    public void 이미_다른_슬롯에_있는_캐릭터를_고르면_두_슬롯이_교환된다()
    {
        SetupDatabase(
            MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1),
            MakeCharacter("d", 1, 1), MakeCharacter("e", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c", "d", "e" });

        Invoke(viewModel, "BeginSelect", 0);
        Invoke(viewModel, "PickCharacter", "d");

        CollectionAssert.AreEqual(
            new List<string> { "d", "b", "c", "a", "e" },
            DeckIds(viewModel));
    }

    [Test]
    public void 교환후에도_덱에_중복이_없다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c" });

        Invoke(viewModel, "BeginSelect", 2);
        Invoke(viewModel, "PickCharacter", "a");

        List<string> deck = DeckIds(viewModel);
        var seen = new HashSet<string>();

        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i] != null)
            {
                Assert.IsTrue(seen.Add(deck[i]), "중복이 생겼다");
            }
        }
    }

    [Test]
    public void 같은_슬롯의_캐릭터를_다시_고르면_해제된다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b" });

        Invoke(viewModel, "BeginSelect", 0);
        Invoke(viewModel, "PickCharacter", "a");

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual("b", deck[0], "a가 빠지고 b가 당겨와야 한다");
        Assert.IsNull(deck[1]);
    }

    [Test]
    public void 가운데_슬롯을_비우면_뒤가_앞으로_당겨온다()
    {
        SetupDatabase(
            MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1),
            MakeCharacter("d", 1, 1), MakeCharacter("e", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c", "d", "e" });

        Invoke(viewModel, "ClearSlot", 1);

        CollectionAssert.AreEqual(
            new List<string> { "a", "c", "d", "e", null },
            DeckIds(viewModel));
    }

    [Test]
    public void 빈칸은_항상_뒤쪽에만_연속으로_존재한다()
    {
        SetupDatabase(
            MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c" });

        Invoke(viewModel, "ClearSlot", 0);

        List<string> deck = DeckIds(viewModel);
        bool sawNull = false;

        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i] == null)
            {
                sawNull = true;
            }
            else
            {
                Assert.IsFalse(sawNull, $"인덱스 {i}에 빈칸 뒤의 캐릭터가 있다");
            }
        }
    }

    [Test]
    public void 뒤쪽_빈슬롯을_골라도_앞의_빈칸부터_채워진다()
    {
        SetupDatabase(
            MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b" });

        Invoke(viewModel, "BeginSelect", 4);
        Invoke(viewModel, "PickCharacter", "c");

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual("c", deck[2], "빈칸이 앞으로 당겨져 슬롯2에 놓인다");
        Assert.IsNull(deck[3]);
        Assert.IsNull(deck[4]);
    }

    [Test]
    public void 선택중이_아니면_PickCharacter는_무시된다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a" });

        Invoke(viewModel, "PickCharacter", "b");

        Assert.IsNull(DeckIds(viewModel)[1]);
    }

    [Test]
    public void DB에_없는_ID를_고르면_무시된다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a" });

        Invoke(viewModel, "BeginSelect", 1);
        Invoke(viewModel, "PickCharacter", "없는놈");

        Assert.IsNull(DeckIds(viewModel)[1]);
        Assert.AreEqual(1, (int)GetProp(viewModel, "PendingSlot"), "실패했으므로 선택 상태가 유지된다");
    }

    [Test]
    public void PickCharacter_성공시_덱변경과_선택닫힘_이벤트가_모두_발생한다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a" });

        int changed = 0;
        int closed = 0;
        AddHandler(viewModel, "OnDeckChanged", () => changed++);
        AddHandler(viewModel, "OnSelectClosed", () => closed++);

        Invoke(viewModel, "BeginSelect", 1);
        Invoke(viewModel, "PickCharacter", "b");

        Assert.AreEqual(1, changed);
        Assert.AreEqual(1, closed);
    }
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "PartyViewModelTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "Test(s)? (run|Suite)|Failed|Passed|error CS"
```

기대: 새로 추가한 10개 중 `선택중이_아니면_PickCharacter는_무시된다`와 `DB에_없는_ID를_고르면_무시된다`를 뺀 8개가 FAIL. `PickCharacter`/`ClearSlot`이 빈 메서드다.

- [ ] **Step 3: 구현**

`PartyViewModel.cs`의 빈 `PickCharacter`와 `ClearSlot`을 아래로 교체하고, 파일 맨 아래 `GetPower` 위에 `Compact`를 추가한다:

```csharp
    public void PickCharacter(string characterID)
    {
        if (m_pendingSlot < 0 || m_pendingSlot >= DECK_SIZE)
        {
            return;
        }

        CharacterDataSO data = m_database != null ? m_database.GetCharacter(characterID) : null;

        if (data == null)
        {
            return;
        }

        int slot = m_pendingSlot;
        int existing = m_deck.IndexOf(data);

        m_pendingSlot = -1;
        OnSelectClosed?.Invoke();

        if (existing == slot)
        {
            // 같은 슬롯의 캐릭터를 다시 고르면 해제
            ClearSlot(slot);
            return;
        }

        if (existing >= 0)
        {
            // 이미 다른 슬롯에 있으면 두 슬롯을 교환한다. 중복이 생길 경로가 없다.
            m_deck[existing] = m_deck[slot];
        }

        m_deck[slot] = data;

        Compact();
        OnDeckChanged?.Invoke();
    }

    public void ClearSlot(int slot)
    {
        if (slot < 0 || slot >= DECK_SIZE)
        {
            return;
        }

        m_deck[slot] = null;

        Compact();
        OnDeckChanged?.Invoke();
    }
```

```csharp
    /// <summary>
    /// 덱 중간의 빈칸을 없앤다. 빈칸이 섞이면 BattleSceneInitializer가
    /// 조회 성공분만 담아서 필드/예비 역할이 한 칸씩 밀린다.
    /// </summary>
    private void Compact()
    {
        int write = 0;

        for (int read = 0; read < DECK_SIZE; read++)
        {
            if (m_deck[read] != null)
            {
                m_deck[write] = m_deck[read];
                write++;
            }
        }

        for (; write < DECK_SIZE; write++)
        {
            m_deck[write] = null;
        }
    }
```

- [ ] **Step 4: 테스트 통과 확인**

Step 2와 같은 명령. 기대: 18개 모두 PASS.

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Game/Scripts/UI/Lobby/PartyViewModel.cs \
        Assets/_Game/Tests/PlayMode/PartyViewModelTests.cs
git commit -m "파티 배치/교환/해제 및 빈칸 당기기 구현"
```

---

### Task 4: 전투력 · 자동편성 · Commit ✅ 완료 (`327ae38`)

**Files:**
- Modify: `Assets/_Game/Scripts/UI/Lobby/PartyViewModel.cs` (`AutoArrange`, `Commit`)
- Modify: `Assets/_Game/Tests/PlayMode/PartyViewModelTests.cs`

**Interfaces:**
- Consumes: Task 1의 `UserDataSO.SaveData()`, Task 3의 `Compact`
- Produces: `AutoArrange()`, `Commit()`. Task 6의 뷰가 호출한다.

- [ ] **Step 1: 실패하는 테스트 추가**

`PartyViewModelTests.cs`의 마지막 `}` 바로 앞에 붙인다:

```csharp
    [Test]
    public void 전투력은_공격력곱10더하기체력의_합이다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100), MakeCharacter("b", 20, 200));
        object viewModel = MakeViewModel(new List<string> { "a", "b" });

        // a: 10*10 + 100 = 200,  b: 20*10 + 200 = 400
        Assert.AreEqual(600, (int)GetProp(viewModel, "CombatPower"));
    }

    [Test]
    public void 빈칸은_전투력에_0으로_계산된다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(new List<string> { "a" });

        Assert.AreEqual(200, (int)GetProp(viewModel, "CombatPower"));
    }

    [Test]
    public void 자동편성은_전투력_상위5명을_내림차순으로_채운다()
    {
        SetupDatabase(
            MakeCharacter("low", 1, 10),
            MakeCharacter("top", 100, 1000),
            MakeCharacter("mid", 50, 500),
            MakeCharacter("a", 10, 100),
            MakeCharacter("b", 20, 200),
            MakeCharacter("c", 30, 300));
        object viewModel = MakeViewModel(null);

        Invoke(viewModel, "AutoArrange");

        CollectionAssert.AreEqual(
            new List<string> { "top", "mid", "c", "b", "a" },
            DeckIds(viewModel));
    }

    [Test]
    public void 보유가_5명_미만이면_자동편성후_나머지는_빈칸이다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100), MakeCharacter("b", 20, 200));
        object viewModel = MakeViewModel(null);

        Invoke(viewModel, "AutoArrange");

        CollectionAssert.AreEqual(
            new List<string> { "b", "a", null, null, null },
            DeckIds(viewModel));
    }

    [Test]
    public void 자동편성은_덱변경_이벤트를_발생시킨다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(null);

        int changed = 0;
        AddHandler(viewModel, "OnDeckChanged", () => changed++);

        Invoke(viewModel, "AutoArrange");

        Assert.AreEqual(1, changed);
    }

    [Test]
    public void Commit은_빈칸을_뺀_ID리스트를_DeckCharacters에_넣는다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c" });

        Invoke(viewModel, "ClearSlot", 1);
        Invoke(viewModel, "Commit");

        CollectionAssert.AreEqual(new List<string> { "a", "c" }, SavedDeckIds());
    }

    [Test]
    public void Commit은_PlayerPrefs에_저장한다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b" });

        Invoke(viewModel, "BeginSelect", 0);
        Invoke(viewModel, "PickCharacter", "b");
        Invoke(viewModel, "Commit");

        SavedDeckIds().Clear();
        Invoke(m_userData, "LoadData");

        CollectionAssert.AreEqual(new List<string> { "b", "a" }, SavedDeckIds());
    }
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "PartyViewModelTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "Test(s)? (run|Suite)|Failed|Passed|error CS"
```

기대: 전투력 테스트 2개는 이미 PASS(Task 2에서 `CombatPower` 구현 완료). 자동편성 3개와 Commit 2개, 총 5개 FAIL.

- [ ] **Step 3: 구현**

`PartyViewModel.cs` 맨 위 `using`에 아래를 추가:

```csharp
using System.Linq;
```

빈 `AutoArrange`와 `Commit`을 아래로 교체:

```csharp
    public void AutoArrange()
    {
        List<CharacterDataSO> top = m_allCharacters
            .OrderByDescending(GetPower)
            .Take(DECK_SIZE)
            .ToList();

        for (int i = 0; i < DECK_SIZE; i++)
        {
            m_deck[i] = i < top.Count ? top[i] : null;
        }

        OnDeckChanged?.Invoke();
    }

    public void Commit()
    {
        if (m_userData == null || m_userData.LobbyData == null)
        {
            return;
        }

        List<string> ids = m_userData.LobbyData.DeckCharacters;
        ids.Clear();

        for (int i = 0; i < m_deck.Count; i++)
        {
            if (m_deck[i] != null)
            {
                ids.Add(m_deck[i].CharacterID);
            }
        }

        m_userData.SaveData();
    }
```

- [ ] **Step 4: 테스트 통과 확인**

Step 2와 같은 명령. 기대: 25개 모두 PASS.

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Game/Scripts/UI/Lobby/PartyViewModel.cs \
        Assets/_Game/Tests/PlayMode/PartyViewModelTests.cs
git commit -m "전투력 계산, 자동편성, 덱 커밋 구현"
```

---

### Task 5: CharacterSlotView ✅ 완료 (`59641ff`)

편성 슬롯과 선택 그리드 칸에 **같은 프리팹**을 쓴다. 둘 다 "아이콘 + 테두리색 + 클릭"이라 나눌 이유가 없다.

MonoBehaviour라 단위 테스트를 쓰지 않는다. 검증은 컴파일이다.

**Files:**
- Create: `Assets/_Game/Scripts/UI/Lobby/CharacterSlotView.cs`

**Interfaces:**
- Consumes: 없음
- Produces: `CharacterSlotView.Bind(Sprite icon, Color frameColor, Action onClicked)`, `CharacterSlotView.SetLabel(string text)`. Task 6이 호출한다.

- [ ] **Step 1: 작성**

`Assets/_Game/Scripts/UI/Lobby/CharacterSlotView.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSlotView : MonoBehaviour
{
    [SerializeField] private Image m_iconImage;
    [SerializeField] private Image m_frameImage;
    [SerializeField] private Button m_button;
    [SerializeField] private TMP_Text m_nameText;

    private Action m_onClicked;

    private void Awake()
    {
        if (m_button != null)
        {
            m_button.onClick.AddListener(func_OnClicked);
        }
    }

    private void OnDestroy()
    {
        if (m_button != null)
        {
            m_button.onClick.RemoveAllListeners();
        }
    }

    /// <summary>아이콘이 null이면 빈 슬롯으로 그린다.</summary>
    public void Bind(Sprite icon, Color frameColor, Action onClicked)
    {
        m_onClicked = onClicked;

        if (m_iconImage != null)
        {
            m_iconImage.sprite = icon;
            m_iconImage.enabled = icon != null;
        }

        if (m_frameImage != null)
        {
            m_frameImage.color = frameColor;
        }
    }

    public void SetLabel(string text)
    {
        if (m_nameText != null)
        {
            m_nameText.text = text;
        }
    }

    private void func_OnClicked()
    {
        m_onClicked?.Invoke();
    }
}
```

- [ ] **Step 2: 컴파일 확인**

```bash
"$UNITY" -batchmode -quit -projectPath "$PROJ" -logFile - 2>&1 | grep -E "error CS"
```

기대: 출력 없음.

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Game/Scripts/UI/Lobby/CharacterSlotView.cs \
        Assets/_Game/Scripts/UI/Lobby/CharacterSlotView.cs.meta
git commit -m "CharacterSlotView 추가 - 편성 슬롯과 선택 그리드 공용 셀"
```

---

### Task 6: PartyPopupView ✅ 완료 (`d8ccf0e`)

편성 패널과 선택 패널을 한 스크립트가 가진다. 두 화면이 `PendingSlot` 하나를 공유하므로 나누면 이벤트만 늘어난다.

Show/Hide 애니메이션은 `UserProfilePopupView.cs:42-77`을 그대로 따른다.

**Files:**
- Create: `Assets/_Game/Scripts/UI/Lobby/PartyPopupView.cs`

**Interfaces:**
- Consumes: Task 2의 `IPartyViewModel`, Task 5의 `CharacterSlotView.Bind` / `SetLabel`
- Produces: `PartyPopupView.Initialize(IPartyViewModel viewModel)`, `PartyPopupView.Show()`, `PartyPopupView.Hide()`. Task 7의 `LobbyInitializer`가 호출한다.

- [ ] **Step 1: 작성**

`Assets/_Game/Scripts/UI/Lobby/PartyPopupView.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PartyPopupView : MonoBehaviour
{
    [Header("편성 패널")]
    [SerializeField] private CharacterSlotView[] m_slotViews; // 0~2 필드, 3~4 예비. 순서가 곧 역할이다
    [SerializeField] private TMP_Text m_combatPowerText;
    [SerializeField] private Button m_autoArrangeButton;
    [SerializeField] private Button m_closeButton;

    [Header("선택 패널")]
    [SerializeField] private GameObject m_selectPanel;
    [SerializeField] private Transform m_gridContainer;
    [SerializeField] private CharacterSlotView m_cellPrefab;
    [SerializeField] private Button m_selectCloseButton;

    [Header("테두리 색")]
    [SerializeField] private Color m_fieldColor = Color.red;
    [SerializeField] private Color m_reserveColor = Color.green;
    [SerializeField] private Color m_emptyColor = Color.white;

    private IPartyViewModel m_viewModel;
    private CanvasGroup m_canvasGroup;
    private RectTransform m_popupTransform;
    private readonly List<CharacterSlotView> m_cells = new List<CharacterSlotView>();

    private void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        m_popupTransform = GetComponent<RectTransform>();
    }

    public void Initialize(IPartyViewModel viewModel)
    {
        m_viewModel = viewModel;

        if (m_viewModel == null)
        {
            return;
        }

        m_viewModel.OnDeckChanged += Refresh;
        m_viewModel.OnSelectRequested += ShowSelectPanel;
        m_viewModel.OnSelectClosed += HideSelectPanel;

        if (m_autoArrangeButton != null)
        {
            m_autoArrangeButton.onClick.AddListener(func_OnAutoArrangeClicked);
        }

        if (m_closeButton != null)
        {
            m_closeButton.onClick.AddListener(func_OnCloseClicked);
        }

        if (m_selectCloseButton != null)
        {
            m_selectCloseButton.onClick.AddListener(func_OnSelectCloseClicked);
        }

        BuildGrid();
        Refresh();
        HideSelectPanel();
    }

    private void OnDestroy()
    {
        if (m_viewModel != null)
        {
            m_viewModel.OnDeckChanged -= Refresh;
            m_viewModel.OnSelectRequested -= ShowSelectPanel;
            m_viewModel.OnSelectClosed -= HideSelectPanel;
        }

        if (m_autoArrangeButton != null)
        {
            m_autoArrangeButton.onClick.RemoveAllListeners();
        }

        if (m_closeButton != null)
        {
            m_closeButton.onClick.RemoveAllListeners();
        }

        if (m_selectCloseButton != null)
        {
            m_selectCloseButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 선택 그리드는 보유 목록이 바뀌지 않으므로 한 번만 생성하고
    /// 이후에는 테두리 색만 다시 칠한다.
    /// </summary>
    private void BuildGrid()
    {
        if (m_gridContainer == null || m_cellPrefab == null)
        {
            return;
        }

        for (int i = 0; i < m_cells.Count; i++)
        {
            if (m_cells[i] != null)
            {
                Destroy(m_cells[i].gameObject);
            }
        }
        m_cells.Clear();

        IReadOnlyList<CharacterDataSO> all = m_viewModel.AllCharacters;

        for (int i = 0; i < all.Count; i++)
        {
            CharacterSlotView cell = Instantiate(m_cellPrefab, m_gridContainer);
            m_cells.Add(cell);
        }
    }

    private void Refresh()
    {
        RefreshSlots();
        RefreshGrid();
        RefreshCombatPower();
    }

    private void RefreshSlots()
    {
        if (m_slotViews == null)
        {
            return;
        }

        IReadOnlyList<CharacterDataSO> deck = m_viewModel.Deck;

        for (int i = 0; i < m_slotViews.Length; i++)
        {
            if (m_slotViews[i] == null)
            {
                continue;
            }

            CharacterDataSO data = i < deck.Count ? deck[i] : null;
            int slot = i;

            m_slotViews[i].Bind(
                data != null ? data.UI_Icon : null,
                GetSlotColor(slot),
                () => m_viewModel.BeginSelect(slot));

            m_slotViews[i].SetLabel(data != null ? data.CharacterName : string.Empty);
        }
    }

    private void RefreshGrid()
    {
        IReadOnlyList<CharacterDataSO> all = m_viewModel.AllCharacters;

        for (int i = 0; i < m_cells.Count && i < all.Count; i++)
        {
            if (m_cells[i] == null)
            {
                continue;
            }

            CharacterDataSO data = all[i];
            string id = data.CharacterID;

            m_cells[i].Bind(
                data.UI_Icon,
                GetDeckColor(data),
                () => m_viewModel.PickCharacter(id));

            m_cells[i].SetLabel(data.CharacterName);
        }
    }

    private void RefreshCombatPower()
    {
        if (m_combatPowerText != null)
        {
            m_combatPowerText.text = m_viewModel.CombatPower.ToString("N0");
        }
    }

    /// <summary>편성 슬롯 자체의 색. 0~2 필드, 3~4 예비.</summary>
    private Color GetSlotColor(int slot)
    {
        return slot < PartyViewModel.FIELD_SIZE ? m_fieldColor : m_reserveColor;
    }

    /// <summary>선택 그리드 칸의 색. 편성 상태에 따라 달라진다.</summary>
    private Color GetDeckColor(CharacterDataSO data)
    {
        IReadOnlyList<CharacterDataSO> deck = m_viewModel.Deck;

        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i] == data)
            {
                return i < PartyViewModel.FIELD_SIZE ? m_fieldColor : m_reserveColor;
            }
        }

        return m_emptyColor;
    }

    private void ShowSelectPanel()
    {
        if (m_selectPanel != null)
        {
            m_selectPanel.SetActive(true);
        }
    }

    private void HideSelectPanel()
    {
        if (m_selectPanel != null)
        {
            m_selectPanel.SetActive(false);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = 0;
            m_canvasGroup.DOFade(1, 0.3f).SetEase(Ease.OutQuad);
        }

        if (m_popupTransform != null)
        {
            m_popupTransform.localScale = Vector3.one * 0.8f;
            m_popupTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    public void Hide()
    {
        if (m_viewModel != null)
        {
            m_viewModel.Commit();
        }

        HideSelectPanel();

        if (m_canvasGroup != null)
        {
            m_canvasGroup.DOFade(0, 0.2f).SetEase(Ease.InQuad);
        }

        if (m_popupTransform != null)
        {
            m_popupTransform.DOScale(Vector3.one * 0.8f, 0.2f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void func_OnAutoArrangeClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.AutoArrange();
        }
    }

    private void func_OnCloseClicked()
    {
        Hide();
    }

    private void func_OnSelectCloseClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.CancelSelect();
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

```bash
"$UNITY" -batchmode -quit -projectPath "$PROJ" -logFile - 2>&1 | grep -E "error CS"
```

기대: 출력 없음.

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Game/Scripts/UI/Lobby/PartyPopupView.cs \
        Assets/_Game/Scripts/UI/Lobby/PartyPopupView.cs.meta
git commit -m "PartyPopupView 추가 - 편성 패널과 캐릭터 선택 패널"
```

---

### Task 7: 로비 배선 ✅ 완료 (`fb4fa8c`)

로비에 편성 버튼을 달고 팝업을 띄운다. 프로필 팝업 배선(`LobbyInitializer.cs:46-71`)을 그대로 따른다. 새 패턴을 만들지 않는다.

**Files:**
- Modify: `Assets/_Game/Scripts/UI/Lobby/ILobbyViewModel.cs:21-24`
- Modify: `Assets/_Game/Scripts/UI/Lobby/LobbyViewModel.cs:33`, `:69-72`
- Modify: `Assets/_Game/Scripts/UI/Lobby/LobbyView.cs:24`, `:41-44`, `:93-96`, `:158-164`
- Modify: `Assets/_Game/Scripts/UI/Lobby/LobbyInitializer.cs:11`, `:43-56`

**Interfaces:**
- Consumes: Task 2의 `PartyViewModel.SetData`, Task 6의 `PartyPopupView.Initialize` / `Show`
- Produces: `ILobbyViewModel.OpenParty()`, `ILobbyViewModel.OnPartyOpenRequested`

- [ ] **Step 1: ILobbyViewModel에 편성 진입점 추가**

`ILobbyViewModel.cs`에서 `event Action OnProfileOpenRequested;` 다음 줄에 추가:

```csharp
    event Action OnPartyOpenRequested;
```

`void OpenProfile();` 다음 줄에 추가:

```csharp
    void OpenParty();
```

- [ ] **Step 2: LobbyViewModel에 구현**

`LobbyViewModel.cs`에서 `public event Action OnProfileOpenRequested;` 다음 줄에 추가:

```csharp
    public event Action OnPartyOpenRequested;
```

`OpenProfile()` 메서드 다음에 추가:

```csharp
    public void OpenParty()
    {
        OnPartyOpenRequested?.Invoke();
    }
```

- [ ] **Step 3: LobbyView에 편성 버튼 추가**

`LobbyView.cs`의 `[SerializeField] private Button m_profileButton;` 다음 줄에 추가:

```csharp
    [SerializeField] private Button m_partyButton;
```

`Initialize`의 프로필 버튼 등록 블록 다음에 추가:

```csharp
            if (m_partyButton != null)
            {
                m_partyButton.onClick.AddListener(func_OnPartyClicked);
            }
```

`OnDestroy`의 프로필 버튼 해제 블록 다음에 추가:

```csharp
        if (m_partyButton != null)
        {
            m_partyButton.onClick.RemoveAllListeners();
        }
```

파일 맨 아래 `func_OnProfileClicked` 다음에 추가:

```csharp
    private void func_OnPartyClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.OpenParty();
        }
    }
```

- [ ] **Step 4: LobbyInitializer 배선**

`LobbyInitializer.cs`의 `[SerializeField] private UserProfilePopupView m_profilePopupView;` 다음 줄에 추가:

```csharp
    [SerializeField] private PartyPopupView m_partyPopupView;
    [SerializeField] private CharacterDatabaseSO m_characterDatabase;
```

`private UserProfileViewModel m_userProfileViewModel;` 다음 줄에 추가:

```csharp
    private PartyViewModel m_partyViewModel;
```

`InitializeAsync`에서 `m_userData = Resources.Load<UserDataSO>("UserData");` 다음, `if (m_userData == null)` 블록 뒤에 추가:

```csharp
        if (m_characterDatabase == null)
        {
            m_characterDatabase = Resources.Load<CharacterDatabaseSO>("CharacterDatabase");
        }

        // 저장된 편성을 먼저 읽어야 ViewModel이 최신 덱을 본다
        m_userData.LoadData();
```

`m_profilePopupView` 초기화 블록 다음에 추가:

```csharp
        // 파티 편성 팝업 초기화
        m_partyViewModel = new PartyViewModel();
        m_partyViewModel.SetData(m_userData, m_characterDatabase);

        if (m_partyPopupView != null)
        {
            m_partyPopupView.Initialize(m_partyViewModel);
            m_partyPopupView.gameObject.SetActive(false);
        }
```

`lobbyViewModel.OnProfileOpenRequested += ...` 블록 다음에 추가:

```csharp
        lobbyViewModel.OnPartyOpenRequested += () =>
        {
            if (m_partyPopupView != null)
            {
                m_partyPopupView.Show();
            }
        };
```

- [ ] **Step 5: 컴파일 확인**

```bash
"$UNITY" -batchmode -quit -projectPath "$PROJ" -logFile - 2>&1 | grep -E "error CS"
```

기대: 출력 없음.

- [ ] **Step 6: 기존 테스트가 안 깨졌는지 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "Test(s)? (run|Suite)|Failed|Passed|error CS"
```

기대: 전체 테스트 PASS. `CharacterSystemTests` / `SkillSystemTests` / `ShipSkillSystemTests`도 통과해야 한다.

- [ ] **Step 7: 커밋**

```bash
git add Assets/_Game/Scripts/UI/Lobby/ILobbyViewModel.cs \
        Assets/_Game/Scripts/UI/Lobby/LobbyViewModel.cs \
        Assets/_Game/Scripts/UI/Lobby/LobbyView.cs \
        Assets/_Game/Scripts/UI/Lobby/LobbyInitializer.cs
git commit -m "로비에 파티 편성 진입점 배선"
```

---

### Task 8: 씬·프리팹 배선 ✅ 완료 (`73213b4`)

**수동 작업으로 계획했으나 에디터 스크립트로 자동화했다.**

`Assets/Editor/PartyUIBuilder.cs`가 `CharacterSlot` 프리팹과 `PartyPopup` 계층을
만들고 직렬화 필드를 전부 연결한다. 멱등이라 여러 번 돌려도 결과가 같다.

```bash
"$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod PartyUIBuilder.Build -logFile -
```

에디터 메뉴 `SpaceCaptain/파티 편성 UI 배선`으로도 실행할 수 있고,
`SpaceCaptain/파티 편성 UI 배선 검증`이 미연결 항목을 잡아낸다.

레이아웃은 기능 확인용 기본형이다. 아트 적용은 별도 작업.

Task 7까지 끝나면 코드는 전부 있고 씬 오브젝트만 없는 상태다.

**Files:**
- Create: `Assets/_Game/Prefabs/UI/CharacterSlot.prefab`
- Modify: `Assets/_Game/Scenes/Main.unity`

- [ ] **Step 1: CharacterSlot 프리팹 만들기**

1. Hierarchy에서 UI > Button 생성, 이름을 `CharacterSlot`으로 변경
2. 자식으로 Image 두 개 추가: `Frame`(테두리, 뒤), `Icon`(아이콘, 앞)
3. 자식으로 TextMeshPro - Text (UI) 추가: `NameText`
4. 루트에 `CharacterSlotView` 컴포넌트 추가
5. 인스펙터에서 연결:
   - `m_iconImage` → `Icon`
   - `m_frameImage` → `Frame`
   - `m_button` → 루트의 Button
   - `m_nameText` → `NameText`
6. `Assets/_Game/Prefabs/UI/`로 드래그해 프리팹화, 씬에서 삭제

- [ ] **Step 2: PartyPopup 오브젝트 만들기**

`Main` 씬 Canvas 아래에 `PartyPopup` 생성.

1. **루트에 `CanvasGroup` 컴포넌트를 반드시 추가한다.** `Show`/`Hide`의 DOTween 페이드가 이걸 전제한다
2. 루트에 `PartyPopupView` 컴포넌트 추가
3. 자식 구성:
   - `FieldSlots` — `CharacterSlot` 프리팹 3개 (가로 배치)
   - `ReserveSlots` — `CharacterSlot` 프리팹 2개 (가로 배치)
   - `CombatPowerText` — TMP Text
   - `AutoArrangeButton` — Button
   - `CloseButton` — Button
   - `SelectPanel` — 전체를 덮는 패널. 아래 둘을 자식으로
     - `GridContainer` — Grid Layout Group 붙인 빈 오브젝트. **셀은 런타임 생성이므로 비워둔다**
     - `SelectCloseButton` — Button

- [ ] **Step 3: PartyPopupView 인스펙터 연결**

**`m_slotViews` 배열은 순서가 곧 역할이다. 반드시 이 순서로 넣는다:**

| 인덱스 | 넣을 오브젝트 | 인게임 역할 |
|---|---|---|
| 0 | 필드 슬롯 1번 | Active — 전투 시작 시 투입 |
| 1 | 필드 슬롯 2번 | Standby |
| 2 | 필드 슬롯 3번 | Standby |
| 3 | 예비 슬롯 1번 | Reserve |
| 4 | 예비 슬롯 2번 | Reserve |

나머지:
- `m_combatPowerText` → `CombatPowerText`
- `m_autoArrangeButton` → `AutoArrangeButton`
- `m_closeButton` → `CloseButton`
- `m_selectPanel` → `SelectPanel`
- `m_gridContainer` → `GridContainer`
- `m_cellPrefab` → `Assets/_Game/Prefabs/UI/CharacterSlot.prefab`
- `m_selectCloseButton` → `SelectCloseButton`
- 색은 기본값(빨강/초록/흰색) 그대로 두거나 아트에 맞게 조정

- [ ] **Step 4: LobbyView / LobbyInitializer 연결**

1. 로비에 편성 버튼 UI를 만들고 `LobbyView`의 `m_partyButton`에 연결
2. `LobbyInitializer`의 `m_partyPopupView`에 `PartyPopup` 연결
3. `LobbyInitializer`의 `m_characterDatabase`에 `Resources/CharacterDatabase.asset` 연결
   (비워두면 `Resources.Load`로 자동 로드되지만 명시하는 편이 낫다)

- [ ] **Step 5: 플레이 확인**

`Main` 씬에서 Play. 아래를 순서대로 확인한다:

| 확인 항목 | 기대 결과 |
|---|---|
| 편성 버튼 탭 | 팝업이 페이드+스케일로 열린다 |
| 슬롯 5칸 | `a`~`e` 5명이 채워져 있다 (`UserData.asset` 초기값) |
| 전투력 | 0이 아닌 숫자가 보인다 |
| 필드 슬롯 탭 | 선택 패널이 열리고 캐릭터 5개가 그리드에 뜬다 |
| 그리드 테두리 | 덱 0~2는 빨강, 3~4는 초록 |
| 미편성 캐릭터 탭 | 해당 슬롯에 들어가고 선택 패널이 닫힌다 |
| 편성된 캐릭터 탭 | 두 슬롯이 자리를 바꾼다 |
| 자동편성 탭 | 전투력 높은 순으로 재배치된다 |
| 팝업 닫기 → 다시 열기 | 편성이 유지된다 |
| 플레이 정지 → 재생 | 편성이 유지된다 (PlayerPrefs 저장 확인) |
| 전투 시작 | 편성한 순서대로 캐릭터가 스폰된다. 슬롯 0이 Active |

- [ ] **Step 6: 커밋**

```bash
git add Assets/_Game/Prefabs/UI/CharacterSlot.prefab \
        Assets/_Game/Prefabs/UI/CharacterSlot.prefab.meta \
        Assets/_Game/Scenes/Main.unity
git commit -m "파티 편성 팝업 씬 배선"
```

---

## Self-Review 결과

**스펙 커버리지**

| 스펙 항목 | 담당 태스크 |
|---|---|
| §3.1 보유 판정 안 만듦, DB 전체 사용 | Task 2 `SetData` |
| §3.2 빈칸 앞으로 당기기 | Task 3 `Compact` |
| §3.3 중복은 교환으로 흡수 | Task 3 `PickCharacter` |
| §3.4 전투력 = Σ(공×10 + 체) | Task 2 `GetPower`, Task 4 테스트 |
| §3.5 자동편성 상위 5명 | Task 4 `AutoArrange` |
| §3.6 PlayerPrefs 저장 2줄 | Task 1 |
| §4 조작 흐름 | Task 6 `PartyPopupView` |
| §5.1 신규 4파일 | Task 2(2개), 5, 6 |
| §5.2 수정 5파일 | Task 1(UserDataSO), Task 7(나머지 4개) |
| §6 검증 | Task 1·2·3·4 테스트 |
| §7 씬 작업 | Task 8 |

**스펙에서 추가된 것**

스펙 §5.3의 `ClearSlot`은 목업에 해제 UI가 없어 호출자가 없을 뻔했다. Task 3에서 **"선택 패널에서 그 슬롯에 이미 있는 캐릭터를 다시 고르면 해제"**로 진입점을 만들었다. 새 UI 요소가 필요 없다.

**알려진 동작 특성**

뒤쪽 빈 슬롯을 탭하고 캐릭터를 골라도 앞의 빈칸부터 채워진다(Task 3 테스트 `뒤쪽_빈슬롯을_골라도_앞의_빈칸부터_채워진다`). §3.2의 no-gaps 규칙에서 따라오는 결과이며 의도된 동작이다.


---

## 완료 후 반영 사항

코드 리뷰(`28edad9`)에서 나온 수정:

- **저장 대상을 덱으로 한정.** `LobbyData` 전체를 저장하면 회복 로직이 없는
  스태미나가 0으로 굳어 전투 시작이 영구히 막혔다. 저장 키도 `SpaceCaptain.Deck`으로 변경.
- **`BattleSceneInitializer`가 덱을 읽기 전 `LoadData()` 호출.** 계획의
  "이 파일을 수정하지 않는다" 제약을 깼다. 지키면 저장이 로비 안에서만 돌아
  기능이 반쪽이 된다.
- `SetData`가 중복 ID를 걸러낸다.
- 팝업 Show/Hide 트윈 경쟁 해소 (`UserProfilePopupView`도 같은 버그였다).
- `Hide`에서 `CancelSelect` 호출.
- 덱 리스트를 `DECK_SIZE` 기준으로 초기화.

최종: **PlayMode 58/58, EditMode 2/2 통과.**
