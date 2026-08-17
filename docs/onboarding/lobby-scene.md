# 로비 씬 온보딩 가이드

`Assets/_Game/Scenes/Main.unity` — 아웃게임 전체를 담당하는 유일한 씬이다.

이 문서는 로비에 기능을 하나 붙이려는 사람이 **어디를 건드려야 하는지**를 알려준다.
프로젝트 전반의 컨벤션은 [README](../../README.md)에 있다.

---

## 1. 이 씬이 하는 일

```
유저 정보 표시 ── 재화, 레벨, 스태미나
스테이지 선택 ── 일반 / 정예
파티 편성    ── 필드 3 + 예비 2, 총 5명
프로필 조회
전투 진입    ── InGame 씬으로 전환
```

스크립트는 `Assets/_Game/Scripts/OutGame/Lobby/` 11개 파일이 전부다.

| 파일 | 줄 수 | 역할 |
|---|---|---|
| `LobbyInitializer.cs` | 117 | **여기서 시작한다.** 씬 전체를 조립 |
| `LobbyView.cs` | 184 | 로비 화면. 재화·스테이지·버튼 |
| `LobbyViewModel.cs` | 88 | 로비 상태와 전투 진입 |
| `PartyPopupView.cs` | 314 | 편성 팝업 + 캐릭터 선택 패널 |
| `PartyViewModel.cs` | 238 | 편성 로직 전부 |
| `CharacterSlotView.cs` | 60 | 슬롯·그리드 칸 공용 셀 |
| `UserProfilePopupView.cs` | 106 | 프로필 팝업 |
| `UserProfileViewModel.cs` | 23 | 프로필 상태 |
| `I*ViewModel.cs` × 3 | 67 | View가 보는 인터페이스 |

---

## 2. 씬 계층

```
===[- Scripts -]===
├── LobbyView            ← LobbyView 컴포넌트
└── LobbyInitializer     ← LobbyInitializer 컴포넌트. 씬의 진입점

Canvas
├── Top_info             재화, 레벨, 설정 버튼, 프로필 버튼
├── Stage_Info           맵 이름, 최고 기록, 난이도 버튼 2개
├── Play_Button          전투 시작 + 스태미나 소모 표시
├── B_Btn_group          하단 탭 4개
│   └── Button_2         ← 파티 편성 진입점
├── Panel_Group
│   └── UserProfilePopupView_Panel
├── PartyPopup           ← 편성 팝업 (기본 비활성)
│   ├── FieldSlot_0~2    CharacterSlot 프리팹 인스턴스
│   ├── ReserveSlot_0~1  CharacterSlot 프리팹 인스턴스
│   ├── CombatPowerText
│   ├── AutoArrangeButton
│   ├── CloseButton
│   └── SelectPanel
│       ├── GridContainer  ← 셀은 런타임 생성. 에디터에서는 비어 있음
│       └── SelectCloseButton
└── sample               캐릭터 일러스트 자리
```

로직을 담은 컴포넌트는 `===[- Scripts -]===` 아래에 모아 둔다. UI 오브젝트에 붙이지
않는다.

---

## 3. 부팅 순서

`LobbyInitializer.Start()` → `InitializeAsync()`. 순서에 의미가 있다.

```
1  LocalizationManager 생성, 번역 데이터 비동기 로드      :29-30
2  씬의 모든 LocalizedTextView에 매니저 주입              :33-37
3  Resources에서 UserData 로드 (없으면 조용히 종료)       :39-44
4  CharacterDatabase 로드 (인스펙터가 비었을 때만)        :46-49
5  UserData.LoadData()  ← 저장된 편성 복원                :52
6  LobbyViewModel 생성 + SetData                          :54-55
7  UserProfileViewModel 생성 + 팝업 Initialize, 비활성화  :58-65
8  PartyViewModel 생성 + 팝업 Initialize, 비활성화        :68-75
9  팝업 열기/닫기 이벤트 바인딩                            :78-100
10 SceneLoader 주입 (전환 연출)                           :102-105
11 LobbyView.Initialize(lobbyViewModel)                   :107-115
```

**5번이 6·8번보다 먼저여야 한다.** ViewModel들이 `LobbyData`를 참조로 들고 가므로,
나중에 로드하면 이미 만들어진 ViewModel이 옛 데이터를 본다.

**11번이 마지막이다.** `LobbyView.Initialize`가 버튼 리스너를 붙이고 즉시 `UpdateUI()`를
호출하므로, 그 전에 데이터가 모두 준비돼 있어야 한다.

`.Forget()`으로 던지는 async라 **예외가 나면 조용히 죽는다.** 로비가 아무 반응이 없으면
콘솔부터 본다.

---

## 4. 누가 누구를 아는가

```
        LobbyInitializer  ── 모두를 알고 조립한다. 유일한 접착점
                 │
     ┌───────────┼────────────┐
     ▼           ▼            ▼
 LobbyView  ProfilePopup  PartyPopup     (MonoBehaviour, 그리기만)
     │           │            │
     ▼           ▼            ▼
ILobbyVM   IUserProfileVM  IPartyVM      (인터페이스)
     │           │            │
     ▼           ▼            ▼
LobbyViewModel  ...      PartyViewModel  (순수 C#, 로직)
                                │
                                ▼
                  UserDataSO · CharacterDatabaseSO
```

규칙 셋:

- **View는 ViewModel의 구현이 아니라 인터페이스를 본다.**
- **ViewModel은 `MonoBehaviour`가 아니다.** 그래서 씬 없이 테스트된다
  (`PartyViewModelTests` 27케이스가 씬을 안 띄운다).
- **View끼리 직접 대화하지 않는다.** 로비에서 팝업을 여는 것도
  `LobbyViewModel.OnPartyOpenRequested` 이벤트를 Initializer가 받아 처리한다.

---

## 5. 데이터 흐름

```
Resources/UserData.asset
        │  Resources.Load
        ▼
   UserDataSO.LobbyData ──참조──▶ LobbyViewModel   (재화, 스태미나)
        │              └─참조──▶ PartyViewModel   (편성)
        │
        │  SaveData() / LoadData()
        ▼
PlayerPrefs["SpaceCaptain.Deck"]   ← 편성만 저장한다
```

**저장 대상은 편성뿐이다.** `LobbyData` 전체를 저장하면 안 된다 — 스태미나는 소모만
되고 회복 로직이 없어서, 0인 상태가 영구 저장되면 전투 시작이 영영 막힌다. 실제로
발생했던 버그라 `UserDataSaveTests`에 회귀 테스트가 있다.

`Resources.Load`가 돌려주는 것은 **에셋 그 자체**다. 에디터에서 Play하면 런타임 변경이
디스크에 남아 git에 잡힌다. 알려진 한계이고, 편성의 진실은 PlayerPrefs 쪽이라 기능상
문제는 없다.

---

## 6. 파티 편성

로비에서 가장 큰 기능이라 따로 본다.

### 덱은 리스트 하나이고 순서가 곧 역할이다

```csharp
LobbyDataDTO.DeckCharacters   // List<string>, 캐릭터 ID
```

| 인덱스 | 역할 | 인게임 동작 |
|---|---|---|
| 0 | Active | 전투 시작 시 투입 |
| 1, 2 | Standby | 필드 대기, 드래그로 교대 |
| 3, 4 | Reserve | 예비. 쿨다운 후 교체 투입 |

`PartyPopupView.m_slotViews` 배열도 **같은 순서**로 연결돼 있다. 0~2가 필드, 3~4가
예비다. 순서를 잘못 꽂으면 편성한 선두가 예비로 들어간다.

### 빈칸은 항상 뒤쪽에만 있다

`PartyViewModel.Compact()`가 슬롯을 비울 때마다 뒤를 앞으로 당긴다. 덱 중간에 빈칸이
생기면 `BattleSceneInitializer`가 조회에 성공한 것만 담기 때문에 역할이 한 칸씩 밀린다.

```
[a][b][c]   b 제거   [a][c][d]
[d][e]      ──────▶  [e][  ]
```

### 조작

```
슬롯 탭 ──▶ BeginSelect(slot) ──▶ 선택 패널 열림
                                      │
        캐릭터 탭 ──▶ PickCharacter(id)
                          ├─ 미편성이면 그 슬롯에 배치
                          ├─ 다른 슬롯에 있으면 두 슬롯 교환
                          └─ 같은 슬롯이면 해제
                                      │
        팝업 닫기 ──▶ Commit() ──▶ DeckCharacters 갱신 + SaveData()
```

**중복 검사 코드가 없다.** 이미 편성된 캐릭터를 고르면 두 슬롯이 자리를 바꾸므로
중복이 생길 경로 자체가 없다.

### 선택 그리드의 원본

`CharacterDatabaseSO.GetAllCharacters()` 전체다. `LobbyDataDTO.OwnedCharacters`는
아직 채우는 시스템이 없어 쓰지 않는다. 획득/가챠가 생기면
`PartyViewModel.SetData`에 `.Where(보유)` 한 줄을 끼우면 된다.

---

## 7. 씬 배선은 손으로 하지 않는다

`PartyPopup`과 `CharacterSlot` 프리팹은 에디터 스크립트가 만든다. 멱등이라 다시 돌려도
결과가 같다.

에디터 메뉴에서 실행한다. CLI로 돌리려면 Unity 실행 파일 경로를 직접 지정한다.

```bash
<Unity 실행 파일> -batchmode -quit -projectPath <프로젝트 경로> \
  -executeMethod PartyUIBuilder.Build -logFile -
```

메뉴에서도 된다.

| 메뉴 | 하는 일 |
|---|---|
| `SpaceCaptain/파티 편성 UI 배선` | 프리팹과 팝업 생성, 직렬화 필드 연결 |
| `SpaceCaptain/파티 편성 UI 배선 검증` | 미연결 항목 찾기 |
| `SpaceCaptain/캐릭터를 무기군 수에 맞추기` | 캐릭터 로스터 구성 |

**아트를 적용한 뒤에는 `Build`를 다시 돌리지 않는다.** 팝업을 지우고 새로 만든다.
검증 메서드는 언제 돌려도 안전하다.

### 인스펙터 연결 지점

| 컴포넌트 | 필드 | 비고 |
|---|---|---|
| `LobbyInitializer` | `m_lobbyView`, `m_profilePopupView`, `m_partyPopupView`, `m_characterDatabase`, `m_transitionSettings` | DB는 비우면 `Resources.Load`로 자동 |
| `LobbyView` | 텍스트 5 + 스테이지 4 + 버튼 4 + 일러스트 | `m_partyButton`은 `B_Btn_group/Button_2` |
| `PartyPopupView` | `m_slotViews`(5), 텍스트·버튼 3, 선택 패널 4, 색 3 | **슬롯 순서 주의** |

`PartyPopup` 루트에는 **`CanvasGroup`이 반드시 있어야 한다.** `Show`/`Hide`의 DOTween
페이드가 이를 전제한다.

---

## 8. 새 기능을 붙이는 순서

우편함을 예로 든다.

1. **`IMailboxViewModel`** — View가 볼 표면을 먼저 정한다. 프로퍼티, 메서드, 이벤트.
2. **`MailboxViewModel`** — 로직. `MonoBehaviour`를 상속하지 않는다. 이래야 테스트된다.
3. **테스트** — `Assets/_Game/Tests/PlayMode/MailboxViewModelTests.cs`.
   게임 타입은 `TestReflectionHelper.GetGameType(...)`으로 얻는다. 직접 참조하면
   컴파일되지 않는다.
4. **`MailboxPopupView`** — `MonoBehaviour`. 직렬화 필드를 들고, ViewModel 이벤트를
   구독해 다시 그린다. `UserProfilePopupView`의 Show/Hide를 그대로 베낀다.
5. **진입점** — `ILobbyViewModel`에 `OpenMailbox()`와 `OnMailboxOpenRequested`를 추가하고
   `LobbyViewModel`에 구현. `LobbyView`에 버튼 필드와 `func_OnMailboxClicked`를 추가.
6. **조립** — `LobbyInitializer`에 ViewModel 생성·주입·이벤트 바인딩을 추가. 프로필
   팝업 블록(`:57-65`, `:78-84`)을 그대로 따른다.
7. **씬** — 에디터에서 오브젝트를 만들고 필드를 연결한다. 반복될 것 같으면
   `PartyUIBuilder`처럼 빌더 스크립트로 만든다.

**`ILobbyViewModel`을 고치면 `Assets/Editor/ButtonEventSystemTests.cs`의
`MockLobbyViewModel`도 같이 고쳐야 한다.** 인터페이스 미구현으로 컴파일이 깨진다.

---

## 9. 자주 밟는 지뢰

| 증상 | 원인 |
|---|---|
| 로비가 아무 반응 없음 | `InitializeAsync`가 `.Forget()`이라 예외가 조용히 죽는다. 콘솔 확인 |
| 팝업이 안 열림 | `LobbyInitializer`의 팝업 필드 미연결. null이면 조용히 넘어간다 |
| 팝업이 열렸다 바로 사라짐 | Show/Hide 트윈 충돌. `KillTweens()`가 빠졌는지 확인 |
| 페이드가 안 먹음 | 팝업 루트에 `CanvasGroup` 없음 |
| 편성한 선두가 예비로 들어감 | `m_slotViews` 배열 순서가 0~2 필드, 3~4 예비가 아님 |
| 편성이 전투에 반영 안 됨 | `BattleSceneInitializer`의 `LoadData()` 호출 확인 |
| 편성이 재시작 후 사라짐 | `Commit()`이 안 불렸다. 팝업 `Hide()`가 호출하는 구조다 |
| 그리드가 비어 있음 | `CharacterDatabase` 미연결, 또는 `m_cellPrefab` 미연결 |
| 인터페이스 수정 후 컴파일 실패 | `ButtonEventSystemTests.MockLobbyViewModel` 미구현 |

---

## 10. 관련 문서

- [README](../../README.md) — 프로젝트 전반 컨벤션
- [파티 편성 설계](../superpowers/specs/2026-08-17-lobby-party-ui-design.md) — 왜 이렇게 만들었는지
- [파티 편성 구현 계획](../superpowers/plans/2026-08-17-lobby-party-ui.md) — 단계별 기록
