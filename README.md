# space_captain

우주함장. 세로형 모바일 슈팅 게임.

로비에서 파티 5명을 편성하고, 전투에서 캐릭터를 교대(스왑)하며 무기군별 공격 패턴으로
적을 처리한다.

---

## 시작하기

**Unity 6000.3.19f1** `ProjectSettings/ProjectVersion.txt`가 기준

```bash
git clone <repo> && cd space_captain
# Unity Hub에서 프로젝트 폴더를 추가하고 열면 끝. 별도 패키지 설치 단계는 없다.
```


| 씬 | 역할 | 진입 스크립트 |
|---|---|---|
| `Assets/_Game/Scenes/Main.unity` | 로비(아웃게임) | `LobbyInitializer` |
| `Assets/_Game/Scenes/InGame.unity` | 전투 | `BattleSceneInitializer` |

`Main`에서 시작해 전투 시작 버튼을 누르면 `InGame`으로 넘어간다.

---

## 온보딩 문서

이 README는 **프로젝트 전체에 걸친 규칙**을 다룬다. 특정 씬이나 기능을 실제로 만질
때는 아래 문서를 본다.

### 씬별 가이드

씬에 기능을 하나 붙이려는 사람을 위한 것이다. 구조, 부팅 순서, 배선 지점
| 문서 | 대상 | 상태 |
|---|---|---|
| [로비 씬](docs/onboarding/lobby-scene.md) | `Main.unity` · `Scripts/OutGame/` 11개 파일 | 작성됨 |
| [전투 씬](docs/onboarding/ingame-scene.md) | `InGame.unity` · `Scripts/InGame/` 59개 파일 | 작성됨 |

### 기능별 설계와 계획

기능을 **왜 그렇게 만들었는지**가 필요할 때 본다. 스펙은 결정과 그 근거를, 계획은
단계별 구현 기록을 담는다.

| 기능 | 설계 | 계획 |
|---|---|---|
| 파티 편성 UI | [스펙](docs/superpowers/specs/2026-08-17-lobby-party-ui-design.md) | [계획](docs/superpowers/plans/2026-08-17-lobby-party-ui.md) |
| 무기군별 공격 패턴 | [스펙](docs/superpowers/specs/2026-08-17-weapon-attack-patterns-design.md) | [계획](docs/superpowers/plans/2026-08-17-weapon-attack-patterns.md) |
| 무기 검수 보완 | — | [계획](docs/superpowers/plans/2026-08-17-weapon-attack-patterns-review-remediation.md) |


무기군 계획서는 **실제 구현과 설계가 다르다.** 계획은 `WeaponGroupSO` + `IAttackPattern`을
전제하지만 실제 코드는 `WeaponDataSO` + `IWeaponBehaviour`로 들어갔다. 코드를 기준으로
읽고, 계획서는 검토 근거로만 참고한다.

### 어디부터 읽을까

| 상황 | 순서 |
|---|---|
| 처음 합류했다 | 이 README → 폴더 구조 → 코드 컨벤션 → 만질 씬의 가이드 |
| 로비에 기능을 붙인다 | [로비 가이드](docs/onboarding/lobby-scene.md)의 "새 기능을 붙이는 순서" |
| 테스트가 안 돈다 | 이 README의 [테스트](#테스트) 절 — 제약 세 가지부터 |
| 뭔가 이상하다 | 이 README의 [자주 밟는 지뢰](#자주-밟는-지뢰), 그다음 씬 가이드의 같은 절 |

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

## 도메인

코드에 나오는 용어들이다. 이 표만 알면 어느 파일을 열어도 무엇을 하는지 읽힌다.

### 아웃게임

| 용어 | 코드 | 뜻 |
|---|---|---|
| **덱 / 편성** | `LobbyDataDTO.DeckCharacters` | 전투에 데려갈 캐릭터 5명. `List<string>` 하나이고 **순서가 곧 역할** |
| **필드** | 덱 인덱스 0~2 | 전투에 즉시 나가는 3명 |
| **예비** | 덱 인덱스 3~4 | 대기 2명. 쿨다운을 기다려 교체 투입 |
| **전투력** | `PartyViewModel.CombatPower` | 편성 강함을 나타내는 단일 수치 |
| **스태미나** | `LobbyDataDTO.CurrentStamina` | 전투 진입 비용. **회복 로직이 아직 없다** |
| **스테이지 난이도** | `StageDifficulty` | 일반 / 정예 |

### 인게임

| 용어 | 코드 | 뜻 |
|---|---|---|
| **모함** | `MasterShip` (HP 1000) | 지켜야 할 본체. 파괴 시 패배 처리는 [아직 미연결](docs/onboarding/ingame-scene.md#6-승패-판정) |
| **배리어** | `Barrier` (500) | 모함 앞의 방어막. 피해를 먼저 흡수하고 깨진다 |
| **웨이브** | `WaveConfigDTO` | 적이 몰려오는 한 묶음. 진행할수록 수는 늘고 간격은 줄어든다 |
| **보스** | `BossController` | 지정 웨이브(기본 3)에 등장 |
| **스왑** | `PlayerSwapManager` | 필드 캐릭터를 교대하는 조작. 이 게임의 핵심 |
| **액티브 스킬** | `ActiveSkill` | 캐릭터별 궁극기. 쿨다운과 컷인 연출 |
| **무기군** | `WeaponDataSO` | 공격 방식의 종류. 9종 |

### 캐릭터 상태 (`CharacterSwapState`)

```
Active   활성. 조작 가능. 항상 1명
Standby  대기. 필드에 있으나 조작 불가
Reserve  예비. 필드 밖
Dead     사망
```

덱 순서가 초기 상태를 정한다. 0번이 `Active`, 1~2번이 `Standby`, 3~4번이 `Reserve`다.

### 무기군 9종

`Assets/_Game/Resources/Weapons/`에 에셋으로 있고 `WeaponCatalog`가 읽는다.

| 무기군 | 거동 | 실제 설정값 |
|---|---|---|
| 권총 `pistol` | Straight | `FireRate 0.5` — 기본형. 특수 수치 없음 |
| 소총 `rifle` | Straight | `FireRate 0.15` — 연사만 빠름 |
| 기관총 `machine-gun` | Straight | `FireRate 0.3` → `WarmupTime 2`초에 걸쳐 `MaxFireRate 0.06`까지 가속 |
| 샷건 `shotgun` | Straight | `BulletCount 5`, `SpreadAngle 60`, `DamageFalloffRate 0.6` |
| 저격총 `sniper-rifle` | Straight | `MaxTargets 3`, `PierceDamageRate 0.8`, `FireRate 1.5` |
| 검 `sword` | Straight | `MaxTargets -1`(무제한 관통), `ProjectileScale 3`, `FireRate 1.5` |
| 레이저 `laser` | Beam | `BeamWidth 1.5`, `BeamRange 20` |
| 유탄 발사기 `grenade-launcher` | Explosive | `ExplosionRadius 2.5`, `FireRate 1.2` |
| 지팡이 `staff` | Chain | `ChainCount 3`, `ChainRange 4`, `ChainDamageRate 0.7`, `DamageMultiplier 0.6` |

거동은 4종뿐이고 나머지 차이는 전부 `WeaponDataSO`의 수치다. `Straight` 하나가 9종 중
6종을 덮는다 — 단발·연사·산탄·관통·검기가 전부 같은 코드다.

**새 무기군을 만들 때 코드를 먼저 건드리지 않는다.** 기존 거동 + 수치 조합으로 되는지
부터 본다.

### 무기 수치 필드가 어디에 쓰이는지

거동마다 읽는 필드가 다르다. 엉뚱한 필드를 채워도 아무 일도 일어나지 않는다.

| 거동 | 읽는 필드 |
|---|---|
| 공통 | `FireRate`, `BulletCount`, `SpreadAngle`, `DamageMultiplier`, `WarmupTime`, `MaxFireRate` |
| `Straight` | `ProjectilePrefab`, `ProjectileSpeed`, `ProjectileScale`, `Range`, `MaxTargets`, `PierceDamageRate`, `DamageFalloffRate` |
| `Beam` | `BeamWidth`, `BeamRange`, `BeamVisualPrefab`, `Range` |
| `Explosive` | `ProjectilePrefab`, `ProjectileSpeed`, `ProjectileScale`, `Range`, `ExplosionRadius` |
| `Chain` | `ProjectilePrefab`, `ChainCount`, `ChainRange`, `ChainDamageRate` |

`MaxTargets`는 **관통 수**다. `-1`이면 무제한이다. 연쇄 횟수는 `ChainCount`로 따로
있으니 헷갈리지 않는다.

### 데이터 자산

| 에셋 | 내용 |
|---|---|
| `Resources/UserData.asset` | 닉네임, 재화, 스태미나, 덱 |
| `Resources/CharacterDatabase.asset` | 캐릭터 9종 목록 |
| `Resources/{a~i}_CharacterData.asset` | ID · 이름 · 프리팹 · 아이콘 · 기본 스탯 · 기본 무기 |
| `Resources/Weapons/*.asset` | 무기군 9종 |
| `Resources/ItemDatabase.asset` | 보상 아이템 |

---

## 코드 컨벤션

프로젝트 전체가 아래 규칙을 지킨다. 숫자는 실제 코드에서 센 것이다.

### 케이싱

**공개된 것은 파스칼, 지역적인 것은 카멜, 상수는 대문자 스네이크.** 아래는 전부 실제
코드를 센 수치다. 예외가 거의 없으니 그대로 따르면 된다.

| 대상 | 케이싱 | 예 | 실측 |
|---|---|---|---|
| 클래스 · 인터페이스 · enum · struct | **PascalCase** | `PartyViewModel` | 90 / 0 |
| public 프로퍼티 | **PascalCase** | `CharacterID`, `CombatPower` | 70 / 0 |
| public 필드 (DTO) | **PascalCase** | `MaxHp`, `AttackDamage` | 전부 |
| 메서드 (public·private 모두) | **PascalCase** | `SetData`, `Compact` | 201 / 0 |
| enum 멤버 | **PascalCase** | `Single`, `Spread`, `Beam` | 전부 |
| private 인스턴스 필드 | `m_` + **camelCase** | `m_lobbyView` | 302 / 1 |
| private static 필드 | `s_` + **camelCase** | `s_gameAssembly` | 4 |
| 메서드 파라미터 | **camelCase** | `characterID`, `slot` | 171 / 0 |
| 지역 변수 | **camelCase** | `viewModel`, `deck` | 66 / 0 |
| 상수 (`const`) | **UPPER_SNAKE_CASE** | `DECK_SIZE`, `SAVE_KEY` | 10 / 1 |

DTO의 public 필드가 파스칼인 점에 주의한다. C# 관례상 필드는 카멜인 경우도 있지만
이 프로젝트는 **프로퍼티와 필드를 같은 케이싱으로 통일**했다. `JsonUtility` 직렬화
키가 그대로 노출되므로 저장 포맷과도 직결된다.

```csharp
public class PlayerStatsDTO   // 클래스: Pascal
{
    public int MaxHp = 100;   // public 필드: Pascal
}
```

### 접두사

| 대상 | 규칙 | 실측 |
|---|---|---|
| private 인스턴스 필드 | `m_` | 378곳 준수, 2곳 예외 |
| private static 필드 | `s_` | 4곳 |
| 인터페이스 | `I` | 16개 |
| UI 버튼 핸들러 | `func_` | 22개 |

인스펙터 노출은 **`[SerializeField] private` + 읽기 전용 프로퍼티**를 쓴다.
`CharacterDataSO`, `UserDataSO`, `CharacterDatabaseSO`가 이 형태다.

```csharp
[SerializeField] private string m_characterID;
public string CharacterID => m_characterID;
```

**예외 하나:** `WeaponDataSO`는 `public` 필드를 그대로 노출한다. 나중에 들어온 코드라
형태가 다르다. 통일하려면 소비처를 함께 고쳐야 하므로 그대로 두고 있다. **새 SO를
만들 때는 위쪽 형태를 따른다.**

`DTO`는 예외가 아니다. 순수 데이터 컨테이너라 처음부터 `public` 필드를 쓴다.

```csharp
[SerializeField] private LobbyView m_lobbyView;
private UserDataSO m_userData;
private static Assembly s_gameAssembly;
private const int DECK_SIZE = 5;
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
접두사 뒤는 메서드 규칙대로 PascalCase다 — `func_` + `OnPartyClicked`.

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

## 디자인 패턴

이 프로젝트가 반복해서 쓰는 여섯 가지다. 새 코드를 짤 때는 **새 구조를 발명하기 전에
여기 있는 것으로 되는지 먼저 본다.**

| 패턴 | 쓰는 곳 | 해결하는 문제 |
|---|---|---|
| MVVM | 모든 UI | 씬 없이 로직을 테스트 |
| DTO + Logic + MonoBehaviour | 게임플레이 6곳 | 씬 없이 게임 규칙을 테스트 |
| 전략 | 스왑 4종, 무기 4종 | 조건 분기 대신 교체 가능한 거동 |
| Initializer | 씬마다 1개 | DI 컨테이너 없이 의존성 주입 |
| 오브젝트 풀 | 발사체 전부 | 전투 중 GC 스파이크 제거 |
| 카탈로그 | `WeaponCatalog` | ID로 에셋 조회 |

### 관통하는 원칙 하나

**로직은 `MonoBehaviour` 바깥에 둔다.** MVVM도, DTO+Logic도 같은 이유다.
`MonoBehaviour`는 씬과 생명주기에 묶여 있어 테스트하려면 씬을 띄워야 하고, 그러면
느리고 잘 깨진다. 실제로 순수 클래스 테스트는 즉시 돌지만 씬을 띄우는 테스트는
5~8분이 걸린다.

### DTO + Logic + MonoBehaviour 3분할

게임플레이 쪽의 기본형이다. 한 파일에 세 클래스가 같이 있다.

```csharp
public class BarrierDTO      // 상태만. 로직 없음
{
    public int MaxBarrier = 500;
    public int CurrentBarrier = 500;
    public bool IsBroken = false;
}

public class BarrierLogic    // 규칙. MonoBehaviour 아님 → 테스트 가능
{
    public int ResolveDamage(int damage) { ... }
    public float GetBarrierRatio() { ... }
}

public class Barrier : MonoBehaviour   // 씬 연결. 입력을 받아 Logic에 넘기고 결과를 그린다
{
}
```

6곳에서 쓴다: `Barrier`, `MasterShip`, `EnemyController`, `BossController`,
`EnemySpawner`, `TopScrollContrl`.

**새 게임플레이 규칙을 만들 때 이 형태를 따른다.** 수치는 DTO에, 계산은 Logic에,
`Update`와 충돌 판정만 `MonoBehaviour`에 둔다.

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

교체 가능한 거동은 인터페이스 + 구현체로 분리한다. `switch`를 늘리지 않는다.

```
ISwapStrategy      FieldSwap, CircularSwap, ReserveSwap, DeathSwap
IWeaponBehaviour   StraightWeapon, BeamWeapon, ChainWeapon, ExplosiveWeapon
```

둘 다 **컨텍스트 구조체 하나를 받는다.** 파라미터가 늘어도 시그니처가 흔들리지 않는다.

```csharp
public struct WeaponFireContext
{
    public Vector3 Origin;
    public float BaseAngle;
    public int Damage;
    public string OwnerID;
    public Transform[] FirePoints;
    public IAttackTarget Target;
    public ObjectPoolManager Pool;
    public WeaponDataSO Data;
    // ...
}
```

### 오브젝트 풀

발사체는 전투 중 초당 수십 개가 생겼다 사라진다. `Instantiate`/`Destroy`를 쓰면 GC가
튄다. `ObjectPoolManager`가 재사용하고, `IPoolable`이 재사용 시점의 초기화 훅을 준다.

```csharp
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}
```

**풀에서 꺼낸 오브젝트를 `Destroy`하면 안 된다.** 풀이 죽은 참조를 들고 있게 된다.
반드시 `ReturnToPool`로 돌려준다.

### 카탈로그

에셋을 ID로 찾는다. `Resources.LoadAll`을 한 번만 하고 캐시한다.

```csharp
WeaponCatalog.Get("shotgun");   // WeaponDataSO
WeaponCatalog.All;              // 전체 목록
```

`CharacterDatabaseSO.GetCharacter(id)`도 같은 역할을 한다.

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

에디터 메뉴에서 실행한다. CLI로 돌리려면 Unity 실행 파일 경로를 직접 지정한다.

```bash
<Unity 실행 파일> -batchmode -quit -projectPath <프로젝트 경로> \
  -executeMethod PartyUIBuilder.Build -logFile -
```

`PartyUIBuilder`에는 검증 메서드가 따로 있어 미연결 직렬화 필드를 잡아낸다.

---

## 데이터

런타임 데이터는 `Assets/_Game/Resources/`에 있고 `Resources.Load`로 읽는다.
에셋 목록은 [도메인](#도메인) 절에 있다. 여기서는 **다루는 규칙**만 적는다.

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

기능 작업은 문서를 먼저 남긴다. 이름 규칙은 아래를 따르고, 쌓인 문서는
[온보딩 문서](#온보딩-문서) 절에서 찾는다.

```
docs/onboarding/<씬 또는 영역>.md                    씬 가이드
docs/superpowers/specs/YYYY-MM-DD-<주제>-design.md   설계와 결정 근거
docs/superpowers/plans/YYYY-MM-DD-<주제>.md          단계별 구현 계획
```

문서를 새로 만들면 **[온보딩 문서](#온보딩-문서) 절의 표에 한 줄 추가한다.** 목록에
없는 문서는 없는 것이나 마찬가지다.

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
