# space_captain

우주함장. 세로형 모바일 슈팅 게임.

로비에서 파티 5명을 편성하고, 전투에서 캐릭터를 교대(스왑)하며 무기군별 공격 패턴으로
적을 처리한다.

---

## 시작하기

**Unity 6000.3.19f1** — 정확히 이 버전을 쓴다. `ProjectSettings/ProjectVersion.txt`가 기준이다.

```bash
git clone <repo> && cd space_captain
# Unity Hub에서 프로젝트 폴더를 추가하고 열면 끝. 별도 패키지 설치 단계는 없다.
```

두 개의 씬이 전부다. 둘 다 Build Settings에 등록돼 있다.

| 씬 | 역할 | 진입 스크립트 |
|---|---|---|
| `Assets/_Game/Scenes/Main.unity` | 로비(아웃게임) | `LobbyInitializer` |
| `Assets/_Game/Scenes/InGame.unity` | 전투 | `BattleSceneInitializer` |

`Main`에서 시작해 전투 시작 버튼을 누르면 `InGame`으로 넘어간다.

---

## 폴더 구조

스크립트는 **씬 소속**으로 나뉜다. 기술 레이어가 아니라 "어느 씬에서 도는가"가 기준이다.

```
Assets/_Game/Scripts/
├── InGame/     59개  전투 씬에서만 도는 것
│   ├── Core/          Barrier, MasterShip, GameProgressController, IAttackTarget
│   ├── Player/        캐릭터 본체, 공격, HP바
│   │   ├── Swap/      교대 전략 (ISwapStrategy 구현들)
│   │   └── Weapons/   무기 거동 (IWeaponBehaviour 구현들)
│   ├── Enemy/         적, 보스, 스포너
│   ├── Background/    배경 스크롤
│   ├── Pooling/       ObjectPoolManager
│   ├── Tests/         인게임 수동 테스트 도구
│   └── UI/            전투 HUD, 결과창, 업그레이드
│       ├── ViewModels/
│       ├── Interfaces/
│       └── Components/
├── OutGame/    11개  로비 씬에서만 도는 것
│   └── Lobby/         로비 화면, 파티 편성, 프로필
└── Shared/     20개  양쪽에서 쓰는 것
    ├── Models/        DTO와 ScriptableObject
    ├── Systems/       씬 로더, 로컬라이징
    ├── UI/            LocalizedTextView
    └── Editor/        에셋 빌더
```

**새 스크립트를 어디에 둘지 판단하는 법:** 전투 씬에서만 쓰이면 `InGame/`, 로비에서만
쓰이면 `OutGame/`, 둘 다면 `Shared/`. 애매하면 실제 사용처를 grep해서 정한다.
`ObjectPoolManager`가 `UI/`에 있던 것처럼, 이름이 아니라 소비처가 기준이다.

`Assets/Editor/`는 Unity가 특별 취급하는 경로다. 빌드 파이프라인과 에셋 빌더가 들어간다.

---

## 코드 컨벤션

프로젝트 전체가 아래 규칙을 지킨다. 숫자는 실제 코드에서 센 것이다.

### 네이밍

| 대상 | 규칙 | 실측 |
|---|---|---|
| 인스턴스 private 필드 | `m_` 접두사 | 378곳 준수, 2곳 예외 |
| static private 필드 | `s_` 접두사 | 4곳 |
| 인터페이스 | `I` 접두사 | 16개 |
| 인스펙터 노출 | `[SerializeField] private` — `public` 필드를 쓰지 않는다 | |

```csharp
[SerializeField] private LobbyView m_lobbyView;
private UserDataSO m_userData;
private static Assembly s_gameAssembly;
```

### 접미사가 역할을 말한다

| 접미사 | 뜻 | 예 |
|---|---|---|
| `SO` | ScriptableObject 에셋 | `CharacterDataSO`, `UserDataSO` |
| `DTO` | 순수 데이터. 로직 없음 | `PlayerStatsDTO`, `LobbyDataDTO` |
| `View` | MonoBehaviour. 그리기와 입력만 | `LobbyView`, `PartyPopupView` |
| `ViewModel` | MonoBehaviour 아님. 로직과 상태 | `PartyViewModel` |
| `Strategy` | 교체 가능한 알고리즘 | `ReserveSwapStrategy` |
| `Initializer` | 씬 조립과 의존성 주입 | `LobbyInitializer` |

### 중괄호는 항상 새 줄, 항상 사용

Allman 스타일 1416곳, K&R 0곳. 본문이 한 줄이어도 중괄호를 생략하지 않는다.

```csharp
if (m_viewModel == null)
{
    return;
}
```

### 주석은 한국어

무엇을 하는지가 아니라 **왜 그렇게 했는지**를 쓴다.

```csharp
// 새 인스턴스를 대입하면 ViewModel이 들고 있는 참조가 끊기므로 제자리에서 채운다
List<string> deck = m_lobbyData.DeckCharacters;
```

### UI 버튼 핸들러는 `func_` 접두사

인스펙터의 OnClick에 연결하거나 코드에서 `AddListener`로 붙이는 메서드다. 22개 있다.

```csharp
private void func_OnPartyClicked()
{
    if (m_viewModel != null)
    {
        m_viewModel.OpenParty();
    }
}
```

### 네임스페이스는 선택적

6개만 쓴다(`SpaceCaptain.Player`, `SpaceCaptain.Player.Swap`, `SpaceCaptain.Models`,
`SpaceCaptain.Systems.Localization`, `SpaceCaptain.UI.Components`, `SpaceCaptain.Editor`).
나머지는 전역 네임스페이스다. **기존 파일 옆에 새 파일을 만들 때는 그 파일을 따른다.**

### null 가드를 먼저 두고 빠져나간다

중첩 대신 조기 반환을 쓴다. `Initialize`류 메서드는 의존성이 없으면 조용히 넘어간다.

---

## 아키텍처

### MVVM — View는 얇게, ViewModel은 씬 없이 테스트 가능하게

```
View (MonoBehaviour)  ──참조──▶  IViewModel (인터페이스)
      ▲                                 ▲
      │ 이벤트 구독                      │ 구현
      └──────────────────────  ViewModel (순수 C# 클래스)
```

- **View**는 `MonoBehaviour`다. 직렬화 필드를 들고, ViewModel의 이벤트를 구독해 다시 그린다.
  로직을 담지 않는다.
- **ViewModel**은 `MonoBehaviour`가 아니다. 그래서 씬 없이 단위 테스트가 된다.
- **인터페이스**로 둘을 끊는다. View는 구현이 아니라 `IPartyViewModel`을 본다.

`PartyViewModel`이 좋은 본보기다. 편성 로직 전부를 들고 있으면서 `GameObject`를 모른다.

### Initializer가 조립한다

Unity에는 DI 컨테이너가 없으므로 씬마다 Initializer 하나가 전부를 엮는다.

```csharp
// LobbyInitializer
m_partyViewModel = new PartyViewModel();
m_partyViewModel.SetData(m_userData, m_characterDatabase);
m_partyPopupView.Initialize(m_partyViewModel);
```

새 화면을 붙일 때는 여기에 배선 코드를 추가한다. `View`가 스스로 의존성을 찾아 나서지
않게 한다.

### 전략 패턴 — 교대와 무기

교체 가능한 거동은 인터페이스 + 구현체로 분리한다. 조건 분기를 늘리지 않는다.

```
ISwapStrategy      FieldSwap, CircularSwap, ReserveSwap, DeathSwap
IWeaponBehaviour   StraightWeapon, BeamWeapon, ChainWeapon, ExplosiveWeapon
```

### 파티 편성: 인덱스가 곧 역할

덱은 `List<string>` 하나이고 **순서가 역할을 정한다.** 별도 자료구조가 없다.

| 인덱스 | 역할 | 인게임 |
|---|---|---|
| 0 | Active | 전투 시작 시 투입 |
| 1, 2 | Standby | 필드 대기, 드래그로 교대 |
| 3, 4 | Reserve | 예비, 쿨다운 10초 |

**덱 중간에 빈칸이 있으면 안 된다.** `BattleSceneInitializer`가 조회 성공한 것만 담기
때문에 빈칸이 섞이면 역할이 한 칸씩 밀린다. `PartyViewModel.Compact()`가 항상 앞으로
당겨 이걸 막는다.

### 라이브러리

| 용도 | 라이브러리 | 사용 |
|---|---|---|
| 비동기 | UniTask (`async UniTask`, `.Forget()`) | 15개 파일 |
| 트윈 | DOTween | 21개 파일 |
| 텍스트 | TextMeshPro | UI 전반 |
| 씬 전환 | EasyTransition (`ISceneLoader`로 감쌈) | |

코루틴 대신 UniTask를 쓴다. **새 의존성을 추가하지 않는다.**

---

## 테스트

PlayMode 테스트 11개 파일에 98개 케이스가 있다.

### 반드시 알아야 할 제약 세 가지

**1. 게임 타입을 직접 참조할 수 없다.**

`Game.Tests.asmdef`는 `Assembly-CSharp`를 참조할 수 없다(Unity에서 asmdef → 기본
어셈블리 참조는 불가능). 그래서 모든 테스트가 리플렉션을 쓴다.

```csharp
Type type = TestReflectionHelper.GetGameType("PartyViewModel");
object viewModel = Activator.CreateInstance(type);
```

`TestReflectionHelper`가 그 다리다. 새 테스트도 이 방식을 따른다.

**2. `WaitForSeconds`를 쓰지 않는다.**

`BattleSceneInitializer.Awake`가 `Time.timeScale = 0`으로 시작한다. 스케일 시간 대기는
영원히 만료되지 않아 테스트 실행 전체가 멈춘다.

```csharp
yield return new WaitForSecondsRealtime(0.5f);   // 이렇게
Time.timeScale = 1f;                             // SetUp에서 되돌리기
```

**3. 씬에 배치된 오브젝트가 아니라 목록을 본다.**

전투 씬에는 미리 배치된 캐릭터가 있다. `FindObjectsByType`으로 찾으면 편성으로 스폰된
것과 섞인다. `PlayerSwapManager.Characters` 목록을 봐야 한다.

### 실행

에디터 GUI가 프로젝트를 열고 있으면 `Library` 락 때문에 실패한다. **먼저 닫는다.**

`UNITY`는 에디터 설치 경로다. Hub 기본값은 `/Applications/Unity/Hub/Editor/...`지만
설치 위치를 바꿨다면 `~/Library/Application Support/UnityHub/secondaryInstallPath.json`을
확인한다.

```bash
export UNITY="/Applications/Unity/Hub/Editor/6000.3.19f1/Unity.app/Contents/MacOS/Unity"
export PROJ="$(pwd)"

# PlayMode 전체 (전투 씬 로드가 있어 5~8분)
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testResults "$PROJ/Logs/results.xml" -logFile - 2>&1 | grep -E "error CS|Exiting with code"

# 특정 클래스만
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "PartyViewModelTests" -testResults "$PROJ/Logs/results.xml" -logFile -

# 컴파일만 확인 (출력이 없으면 성공)
"$UNITY" -batchmode -quit -projectPath "$PROJ" -logFile - 2>&1 | grep -E "error CS"
```

10분을 넘기면 통과가 느린 게 아니라 멈춘 것이다. 스케일 시간 대기를 의심한다.

### 테스트 이름

한국어로 쓰되 **숫자로 시작하지 않는다** — C# 식별자 규칙 위반이다.

```csharp
public void 세명만_편성하면_세명만_스폰된다()   // O
public void 3명만_편성하면_3명만_스폰된다()     // X, 컴파일 실패
```

---

## 에디터 자동화

씬과 프리팹 조립을 손으로 하지 않고 스크립트로 만든다. 전부 **멱등**이라 다시 돌려도
결과가 같다. 메뉴에서도, CLI에서도 실행된다.

| 스크립트 | 하는 일 | 메뉴 |
|---|---|---|
| `PartyUIBuilder` | 편성 팝업과 슬롯 프리팹 생성·배선 | `SpaceCaptain/파티 편성 UI 배선` |
| `CharacterRosterBuilder` | 캐릭터 로스터를 무기군 수에 맞춤 | `SpaceCaptain/캐릭터를 무기군 수에 맞추기` |
| `BuildPipelineManager` | 빌드 | |

```bash
"$UNITY" -batchmode -quit -projectPath "$PROJ" \
  -executeMethod PartyUIBuilder.Build -logFile -
```

`PartyUIBuilder`에는 검증 메서드가 따로 있어 미연결 직렬화 필드를 잡아낸다.

---

## 데이터

런타임 데이터는 `Assets/_Game/Resources/`에 있다. `Resources.Load`로 읽는다.

| 에셋 | 내용 |
|---|---|
| `UserData.asset` | 닉네임, 재화, 스태미나, 편성(`DeckCharacters`) |
| `CharacterDatabase.asset` | 캐릭터 9종 목록 |
| `{a~i}_CharacterData.asset` | 캐릭터별 ID·이름·프리팹·아이콘·기본 스탯·무기 |
| `Weapons/` | 무기군 데이터 |
| `ItemDatabase.asset` | 보상 아이템 |

### 저장은 편성만 한다

```csharp
PlayerPrefs.SetString("SpaceCaptain.Deck", ...);
```

`LobbyData` 전체를 저장하면 안 된다. **스태미나는 소모만 되고 회복 로직이 없어서**,
0인 상태가 영구 저장되면 전투 시작이 영영 막힌다. 실제로 한 번 발생했던 버그다.

### 에디터에서 Play하면 에셋이 더러워진다

`Resources.Load`가 돌려주는 것은 에셋 그 자체다. 런타임 변경이 디스크에 남아 git에
잡힌다. 알려진 한계이며, 편성의 진실은 PlayerPrefs 쪽이라 기능상 문제는 없다.

---

## 작업 흐름

기능 작업은 문서를 먼저 남긴다.

```
docs/superpowers/specs/YYYY-MM-DD-<주제>-design.md   설계와 결정 근거
docs/superpowers/plans/YYYY-MM-DD-<주제>.md          단계별 구현 계획
```

커밋 메시지는 한국어로, **무엇을 했는지보다 왜 그렇게 했는지**를 남긴다.

---

## 자주 밟는 지뢰

| 증상 | 원인 |
|---|---|
| 테스트가 10분 넘게 안 끝남 | `WaitForSeconds` + `Time.timeScale = 0` |
| batchmode가 즉시 실패 | 에디터 GUI가 `Library` 락을 쥐고 있음 |
| 테스트에서 `CS0246` 타입 없음 | 게임 타입 직접 참조. `TestReflectionHelper`를 써야 함 |
| 편성이 전투에 반영 안 됨 | `LoadData()` 호출 누락, 또는 덱 순서를 뒤엎는 정렬 |
| 씬 참조가 끊김 | `.cs`만 옮기고 `.cs.meta`를 안 옮김. 항상 `git mv`로 함께 |
| 캐릭터 아이콘이 다 같음 | 여러 캐릭터가 프리팹을 공유. 정체성은 `SetIdentity`로 주입 |
