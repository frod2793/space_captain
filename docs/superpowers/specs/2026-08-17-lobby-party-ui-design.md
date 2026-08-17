# 로비 파티 편성 UI 설계

작성일: 2026-08-17
브랜치: `shield5012/space_captain-LobbyParty-2`

## 1. 목적

로비에서 파티 5명을 편성하는 UI를 만든다. 현재 `LobbyDataDTO.DeckCharacters`는
데이터만 존재하고 이를 편집하는 화면이 없다. 덱을 고치려면 `UserData.asset`을
직접 열어야 한다.

이 문서의 범위는 **편성 화면과 캐릭터 선택 화면, 그리고 편성 결과의 저장**까지다.

## 2. 현황

| 요소 | 위치 | 상태 |
|---|---|---|
| 덱 데이터 | `LobbyDataDTO.cs:17` `DeckCharacters` | `UserData.asset`에 `a,b,c,d,e` 5명 하드코딩 |
| 보유 데이터 | `LobbyDataDTO.cs:16` `OwnedCharacters` | 빈 리스트. 읽는 곳도 쓰는 곳도 없음 |
| 캐릭터 DB | `CharacterDatabaseSO.cs` | 정상 동작. ID 조회는 대소문자 무시 |
| 캐릭터 에셋 | `Resources/{a~e}_CharacterData.asset` | 5종. `c`/`d`/`e`는 이름이 "기어서드"로 중복 |
| 덱 소비 | `BattleSceneInitializer.cs:91-132` | 덱 순회 → 프리팹 생성 → `SetCharacters` |
| 슬롯 배치 | `PlayerSwapManager.cs:158-176` | idx0=Active, idx1~2=Standby, idx3+=Reserve |
| 씬 슬롯 수 | `InGame.unity:11260-11265` | `m_standbyPositions` 2, `m_reservePositions` 2 |
| 저장 | `UserDataSO.cs:12-20` | `SaveData`/`LoadData` 둘 다 빈 스텁 |

인게임은 Active 1 + Standby 2 = **필드 3명**, Reserve **예비 2명**으로 총 5명 구성이다.
목업의 빨강 3칸 / 초록 2칸과 정확히 일치한다.

### 덱 인덱스가 곧 역할

| 인덱스 | 역할 | UI |
|---|---|---|
| 0 | Active — 전투 시작 시 투입 | 필드 1번 (빨강) |
| 1, 2 | Standby — 필드 대기, 드래그 스왑 | 필드 2·3번 (빨강) |
| 3, 4 | Reserve — 예비, 쿨다운 10초 | 예비 1·2번 (초록) |

별도 자료구조가 필요 없다. 리스트 하나의 순서가 전부를 표현한다.

## 3. 결정 사항

### 3.1 보유 판정은 이번에 만들지 않는다

선택 그리드의 원본은 `CharacterDatabaseSO.GetAllCharacters()`다.
`OwnedCharacters`는 빈 리스트이므로 참조하면 그리드가 비어버린다.

획득/가챠 시스템이 생기면 이 자리에 `.Where(c => owned.Contains(c.CharacterID))`
한 줄을 끼운다. 그 전까지는 전체 캐릭터를 보유로 간주한다.

### 3.2 빈칸은 앞으로 당긴다 (compact)

`BattleSceneInitializer.cs:99-127`은 덱을 순회하며 **조회에 성공한 것만** 리스트에
추가한다. 따라서 덱에 빈칸이 섞이면 역할 매핑이 밀린다.

```
덱   [a][ ][c][d][e]
스폰 [a][c][d][e]          ← 4개
결과 a=Active, c=Standby1, d=Standby2, e=Reserve1
     의도했던 c=Standby2, d=Reserve1, e=Reserve2 에서 한 칸씩 밀림
```

배틀 쪽을 고치는 대신 **덱 중간에 빈칸이 생기지 않게 한다.** 슬롯을 비우면 뒤
슬롯이 앞으로 당겨오므로, 빈칸은 항상 뒤쪽에만 연속으로 존재한다.

```
[a][b][c]   b 제거   [a][c][d]
[d][e]      ------>  [e][  ]
```

덱은 항상 앞에서부터 연속으로 채워지고, `BattleSceneInitializer`는 한 줄도
고치지 않는다.

**포기하는 것:** "필드를 비우고 예비만 채우기". 성립하지 않는 편성이라 손실이 없다.

**3명만 편성한 경우:** 필드 3 / 예비 0이 된다. 인게임에서 예비 스왑이 불가능할 뿐
정상 동작한다.

### 3.3 중복은 교환으로 흡수한다

이미 다른 슬롯에 있는 캐릭터를 고르면 **두 슬롯이 자리를 바꾼다.** 중복이 생길
경로 자체가 없으므로 별도 검증 코드를 두지 않는다.

```
현재  [a][b][c][d][e],  슬롯0을 탭하고 d를 선택
결과  [d][b][c][a][e]
```

### 3.4 전투력 = Σ(AttackDamage × 10 + MaxHp)

`PlayerStatsDTO`에는 `MaxHp`, `AttackDamage`, `MoveSpeed`, `Level`만 있다.
필드/예비 가중치 차등은 두지 않는다.

계수 `10`은 `PartyViewModel`의 `const int ATTACK_WEIGHT = 10;`으로 뺀다.
밸런스 조정 시 이 숫자만 고친다.

### 3.5 자동편성 = 개별 전투력 상위 5명

보유 캐릭터를 개별 전투력 내림차순 정렬해 앞에서부터 5칸을 채운다.
동점 처리는 정의하지 않는다 (`OrderByDescending`의 안정 정렬에 맡긴다).

### 3.6 저장은 PlayerPrefs 2줄

편성 결과가 앱 재시작 후 남지 않으면 기능이 반쪽이므로 이번 범위에 포함한다.

```csharp
public void SaveData()
{
    PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(m_lobbyData));
}

public void LoadData()
{
    string json = PlayerPrefs.GetString(SAVE_KEY, string.Empty);
    if (!string.IsNullOrEmpty(json))
    {
        JsonUtility.FromJsonOverwrite(json, m_lobbyData);
    }
}
```

`FromJsonOverwrite`를 쓰는 이유: `LobbyData`는 `UserDataSO`가 소유한 인스턴스이고
`LobbyViewModel`이 이미 그 참조를 들고 있다. 새 인스턴스를 대입하면 참조가 끊긴다.

서버 저장이 붙으면 이 두 메서드만 교체한다.

## 4. 조작 흐름

```
로비
 └ [편성] 버튼 탭
    └ 편성 팝업 (필드 3 + 예비 2 + 전투력 + 자동편성)
       ├ 슬롯 탭 ──────> 선택 패널 (전체 캐릭터 그리드)
       │                  ├ 캐릭터 탭 ─> 해당 슬롯에 배치, 선택 패널 닫힘
       │                  └ 닫기 ──────> 선택 패널만 닫힘
       ├ [자동편성] 탭 ─> 상위 5명으로 덮어씀
       └ 닫기 ─────────> DeckCharacters 반영 + SaveData, 팝업 닫힘
```

선택 패널의 그리드 셀은 현재 편성 상태를 테두리 색으로 표시한다.

| 상태 | 테두리 |
|---|---|
| 필드 편성 중 (덱 0~2) | 빨강 |
| 예비 편성 중 (덱 3~4) | 초록 |
| 미편성 | 기본 |

## 5. 구성

### 5.1 신규 파일

모두 `Assets/_Game/Scripts/UI/Lobby/` 아래.

| 파일 | 역할 |
|---|---|
| `IPartyViewModel.cs` | 뷰가 의존하는 표면 |
| `PartyViewModel.cs` | 편성 로직 전부. MonoBehaviour 아님 |
| `PartyPopupView.cs` | 편성 패널 + 선택 패널을 한 스크립트가 보유 |
| `CharacterSlotView.cs` | 셀 프리팹 컴포넌트. 편성 슬롯과 그리드 칸에 공용 |

**`PartyPopupView`가 두 패널을 겸하는 이유:** 두 화면이 하나의 상태
(`PendingSlot`)를 공유한다. 스크립트를 나누면 그 상태를 주고받는 이벤트가 늘어날
뿐 경계가 깨끗해지지 않는다. 합쳐도 150줄 안쪽이다.

**`CharacterSlotView`를 공용으로 쓰는 이유:** 편성 슬롯과 그리드 칸은 둘 다
"아이콘 + 테두리색 + 클릭 콜백"이다. 프리팹 1종이면 에디터 배선도 절반이 된다.

### 5.2 수정 파일

| 파일 | 변경 |
|---|---|
| `ILobbyViewModel.cs` | `event Action OnPartyOpenRequested`, `void OpenParty()` 추가 |
| `LobbyViewModel.cs` | 위 둘 구현. `OpenProfile` 패턴과 동일 |
| `LobbyView.cs` | `m_partyButton` 직렬화 필드 + 리스너 등록/해제 |
| `LobbyInitializer.cs` | `PartyViewModel` 생성·주입, 팝업 Show/Hide 배선 |
| `UserDataSO.cs` | `SaveData`/`LoadData` 구현 (§3.6) |

`LobbyInitializer`의 배선은 프로필 팝업(`LobbyInitializer.cs:46-71`)을 그대로
따른다. 새 패턴을 만들지 않는다.

### 5.3 `IPartyViewModel`

```csharp
public interface IPartyViewModel
{
    IReadOnlyList<CharacterDataSO> Deck { get; }            // 길이 5 고정, 빈칸은 뒤쪽 null
    IReadOnlyList<CharacterDataSO> AllCharacters { get; }
    int CombatPower { get; }
    int PendingSlot { get; }                      // 선택 패널이 채울 슬롯, 없으면 -1

    void BeginSelect(int slot);
    void PickCharacter(string characterID);
    void ClearSlot(int slot);
    void CancelSelect();
    void AutoArrange();
    void Commit();                                // DeckCharacters 반영 + SaveData

    event Action OnDeckChanged;
    event Action OnSelectRequested;
    event Action OnSelectClosed;
}
```

`Deck`이 `CharacterDataSO`를 담는 이유: 뷰가 아이콘과 이름을 쓰므로 ID만 넘기면
뷰가 다시 DB를 조회해야 한다. VM이 조회를 끝낸 상태로 넘긴다.

### 5.4 `PartyViewModel` 내부

```csharp
private const int DECK_SIZE = 5;
private const int FIELD_SIZE = 3;
private const int ATTACK_WEIGHT = 10;

private readonly List<CharacterDataSO> m_deck;  // 길이 5 고정, 빈칸 null
private LobbyDataDTO m_lobbyData;
private UserDataSO m_userData;
private CharacterDatabaseSO m_database;
private int m_pendingSlot = -1;
```

주요 동작:

- **`PickCharacter`** — 고른 캐릭터가 이미 덱에 있으면 그 인덱스와 `m_pendingSlot`을
  교환한다. 없으면 `m_pendingSlot`에 대입한다. 그다음 compact.
- **`ClearSlot`** — `m_deck[slot] = null` 후 compact.
- **compact** — null을 제거하고 뒤를 당긴 뒤 길이 5까지 null로 채운다.
- **`AutoArrange`** — `AllCharacters`를 개별 전투력 내림차순 정렬해 앞 5개를 대입.
- **`Commit`** — `m_lobbyData.DeckCharacters`를 null이 아닌 ID 리스트로 교체하고
  `m_userData.SaveData()` 호출.

`Commit`이 빈칸을 뺀 리스트를 넣으므로 덱의 실제 길이는 편성 인원 수와 같다.
`BattleSceneInitializer`가 기대하는 형태 그대로다.

## 6. 검증

`Assets/_Game/Tests/PlayMode/PartyViewModelTests.cs` 하나.

`PartyViewModel`은 MonoBehaviour가 아니라 씬 없이 돈다. 다만 `Game.Tests`
asmdef가 `Assembly-CSharp`를 참조하지 않으므로 기존 `TestReflectionHelper`를
거쳐 인스턴스화한다. `CharacterSystemTests`가 쓰는 방식과 동일하다.

`CharacterDataSO`는 `ScriptableObject.CreateInstance`로 만들고 비공개 필드를
리플렉션으로 채운다.

| 테스트 | 검증 내용 |
|---|---|
| 배치 | 빈 슬롯에 미편성 캐릭터를 넣으면 그 슬롯에 들어간다 |
| 교환 | 이미 다른 슬롯에 있는 캐릭터를 고르면 두 슬롯이 바뀌고 중복이 없다 |
| 당기기 | 가운데 슬롯을 비우면 뒤가 앞으로 오고 마지막이 null이 된다 |
| 자동편성 | 전투력 상위 5명이 내림차순으로 채워진다 |
| 전투력 | 합계가 Σ(공×10 + 체) 와 일치한다 |

## 7. 씬 작업 (에디터에서 수동)

스크립트로는 처리할 수 없다. 배선 지점을 최소로 잡았다.

1. **`CharacterSlot` 프리팹** — `Image`(아이콘) + `Image`(테두리) + `Button`,
   `CharacterSlotView` 부착. `Assets/_Game/Prefabs/UI/`에 저장.
2. **`PartyPopup` 오브젝트** — `Main` 씬 캔버스 아래. `CanvasGroup` 필요
   (`UserProfilePopupView`의 DOTween 페이드가 이를 전제로 한다).
   - 편성 슬롯 5개: 프리팹 5개 배치 후 `m_slotViews` 배열에 순서대로 연결
     (0~2 필드, 3~4 예비 — **순서가 곧 역할이므로 주의**)
   - 전투력 `TMP_Text`, 자동편성 `Button`, 닫기 `Button`
   - 선택 패널: 그리드 컨테이너 `Transform` 하나 + 닫기 `Button`.
     셀은 런타임에 프리팹으로 생성한다
3. **`LobbyView`** — `m_partyButton`에 편성 버튼 연결
4. **`LobbyInitializer`** — `m_partyPopupView`에 팝업 연결

## 8. 이번 범위 밖

| 항목 | 이유 |
|---|---|
| 무기 슬롯 | 목업의 초록칸은 무기가 아니라 예비 캐릭터로 확인됨. 무기 시스템은 코드에 없음 |
| 보유/획득 판정 | `OwnedCharacters`를 채우는 시스템이 없음. §3.1 |
| 캐릭터 등급·레벨·장비 | 데이터 모델 자체가 없음 |
| 편성 프리셋 | 요청 없음 |
| 드래그 앤 드롭 정렬 | 슬롯 탭 방식으로 충분 |
| 서버 저장 | PlayerPrefs로 대체. §3.6 |
| `BattleSceneInitializer.cs:95` 하드코딩 폴백 제거 | 덱이 비는 경로가 사라지면 죽은 코드가 된다. 별도 정리 |
