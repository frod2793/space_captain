# 전투 씬 온보딩 가이드

`Assets/_Game/Scenes/InGame.unity` — 게임플레이 전체를 담당하는 씬이다.

스크립트 60개, 7447줄로 프로젝트의 대부분이 여기 있다. 이 문서는 **어느 파일을 열어야
하는지**와 **건드리면 안 되는 지점**을 알려준다. 프로젝트 전반의 컨벤션은
[README](../../README.md)에, 로비는 [로비 가이드](lobby-scene.md)에 있다.

---

## 1. 큰 그림

```
로비에서 편성한 5명이 스폰된다
   └ 1명은 Active(조작 가능), 2명은 Standby(필드 대기), 2명은 Reserve(필드 밖)

배경이 스크롤되며 거리가 쌓인다 ── 목표 거리 도달이 승리 조건
적이 웨이브 단위로 내려온다   ── 3웨이브에 보스
캐릭터는 자동으로 사격한다     ── 무기군이 공격 방식을 정한다
플레이어는 스왑과 스킬만 조작한다

모함(HP 1000)을 지킨다. 앞에 배리어(500)가 있다
```

**조작은 스왑과 스킬뿐이다.** 이동과 사격은 자동이다.

---

## 2. 씬 계층

```
====[InGame]=====        게임플레이 오브젝트
├── MasterShip           모함
├── Barrier              방어막
├── Pos_Group            캐릭터가 설 자리
│   ├── Play_Pos         Active 1자리
│   ├── Hold_Pos × 2     Standby 2자리
│   └── Reserve_Pos × 2  Reserve 2자리
└── ScrollBackGroup      배경 3장 순환

====[Scripts]======      로직 컴포넌트. UI에 붙이지 않는다
├── BattleSceneInitializer   ← 여기서 시작한다
├── SwapManager              PlayerSwapManager
├── EnemySpawn               EnemySpawner (+ BossPawnpoint)
├── GameProgressController   거리 누적과 승리 판정
├── ObjectPoolManager        발사체 재사용
├── ScrollController         배경 스크롤
├── GameResultTestSystem     결과창 수동 테스트 도구
└── [- Views -]
    ├── BattleHUDView
    └── GameFlowPanelView

====[UI]=========
└── Canvas
    ├── Info_UI               HP·배리어·웨이브·진행도
    ├── ActiveSkill_Group     캐릭터 스킬 슬롯
    ├── MasterShipSkill_Group 모함 스킬
    ├── Skill_CutSin          스킬 컷인 연출
    ├── Panel_Group           업그레이드·결과 패널
    └── Text_Group
```

`Pos_Group`의 자리 개수가 **덱 5칸과 정확히 대응한다.** 자리를 늘리려면 덱 크기와
`PlayerSwapManager`의 인덱스 매핑을 같이 고쳐야 한다.

---

## 3. 부팅 순서

`BattleSceneInitializer.Awake()` → `Time.timeScale = 0` → `InitializeSceneAsync()`.

```
Awake  Time.timeScale = 0  ← 전투는 정지 상태로 시작한다        :30

 1  로컬라이징 로드, LocalizedTextView 주입
 2  PlayerSwapManager 찾아 Barrier·PlayerHpBar 연결
 3  UserData.LoadData()  ← 로비에서 저장한 편성 복원            :92
 4  덱을 돌며 캐릭터 프리팹 Instantiate
      · SetIdentity(이름, 아이콘)   ← CharacterDataSO 기준
      · Initialize(스탯)            · IsActive = (i == 0)
      · PlayerAttackComponent에 DefaultWeapon 주입
 5  swapManager.SetCharacters(스폰 목록)
 6  BattleHUDViewModel 생성, MasterShip·Barrier 이벤트 연결
 7  GameProgressViewModel 생성, OnGameCleared → ShowResultClear
 8  BattleHUDView에 ViewModel 주입, 스킬 슬롯을 캐릭터와 1:1 연결
 9  GameFlowPanelView·UpgradePanelView 초기화
10  GameProgressController.Init()  ← 목표 거리 설정
11  EnemySpawner 웨이브 이벤트 연결
12  EnemyController.OnEnemyDead / OnDamageDealt 정적 이벤트 구독
```

**`Time.timeScale = 0`으로 시작한다.** `GameFlowPanelView`의 시작 버튼을 눌러
`func_OnStartButtonClicked()` → `StartGameTime()`이 불려야 시간이 흐른다.
테스트를 짤 때 이것 때문에 멈춘다 ([9절](#9-트러블슈팅) 참조).

**3번이 4번보다 먼저다.** 씬 전환으로 `UserDataSO`가 언로드/재로드되면 로비에서 저장한
편성이 날아간다. `LoadData()`가 PlayerPrefs에서 되살린다.

### 덱이 비었을 때의 폴백

`BattleSceneInitializer`는 덱이 비면 `{"Player_1", "Player_2", "Player"}`로 대체한다.
로비를 거치면 항상 덱이 차 있으므로 **평소에는 타지 않는 경로다.** InGame 씬을 직접
띄워 테스트할 때만 동작한다.

---

## 4. 스왑 — 이 게임의 핵심

`PlayerSwapManager` 676줄. 전투 씬에서 가장 큰 파일이고 가장 조심할 곳이다.

### 자리와 인덱스

`SetCharacters`가 받은 **리스트 순서가 곧 자리**다.

| 인덱스 | 상태 | 자리 |
|---|---|---|
| 0 | `Active` | `Play_Pos` |
| 1, 2 | `Standby` | `Hold_Pos` |
| 3, 4 | `Reserve` | `Reserve_Pos` |

`AlignCharactersToPositions()`가 인덱스로 상태와 위치를 정한다. **`SwapState`는 순서의
결과이지 입력이 아니다.** 전에 `SetCharacters`가 `SwapState`로 정렬해 편성이 무작위로
섞이는 버그가 있었다. 정렬을 다시 넣지 않는다.

### 전략 4종

교체 상황마다 다른 `ISwapStrategy`가 붙는다.

| 전략 | 언제 | 특징 |
|---|---|---|
| `FieldSwapStrategy` | 필드 안에서 교대 | 기본 |
| `CircularSwapStrategy` | 필드 교대, `m_useCircularSwap`이 켜졌을 때 | 원형 배치 연출 |
| `ReserveSwapStrategy` | 대상이 비활성(필드 밖) | 교체 후 나간 쪽에 쿨다운 |
| `DeathSwapStrategy` | 사망으로 인한 자동 교체 | |

선택 조건은 한 줄이다 — **대상이 필드 밖이면 예비 스왑, 아니면 필드 스왑.**

```csharp
bool isReserve = !targetCharacter.gameObject.activeSelf;
```

각 전략은 `PrepareAsync` → `AnimateAsync` → `FinalizeAsync` 3단계다. 새 전략을 만들면
세 개를 다 채운다.

### 쿨다운이 둘이다

| 필드 | 코드 기본값 | 대상 |
|---|---|---|
| `m_swapCooldownDuration` | 2초 | 전체 스왑 |
| `m_reserveSwapCooldownDuration` | 10초 | 예비에서 나온 캐릭터 개별 |

**씬의 실제 값은 인스펙터를 본다.** 코드 기본값과 다르게 설정돼 있다.

---

## 5. 공격과 피해

### 캐릭터 → 적

```
PlayerAttackComponent (Update에서 발사 주기 관리)
        │  WeaponDataSO.Behaviour로 거동 선택
        ▼
IWeaponBehaviour   Straight · Beam · Explosive · Chain
        │
        ├─ Straight/Explosive/Chain → 발사체를 풀에서 꺼냄
        └─ Beam → 발사체 없이 즉시 판정
        ▼
BulletProjectile.OnTriggerEnter2D → enemy.TakeDamage(피해, OwnerID)
```

**피해 적용의 주인은 발사체다.** 전에는 적이 발사체를 읽어 자기 피해를 계산하고
발사체를 `Destroy`까지 했다. 그러면 관통이 불가능하고 풀이 샌다. 적 쪽으로 되돌리지
않는다.

무기군별 수치의 뜻은 [README 도메인](../../README.md#도메인) 절에 있다.

### 적 → 플레이어 / 모함

```
EnemyController → player.TakeDamage(피해)
                     └ Barrier.ResolveDamage()로 먼저 흡수
                        · 배리어가 충분하면 0을 돌려주고 캐릭터는 무사
                        · 모자라면 초과분만 캐릭터에게 간다
                        · 0이 되면 IsBroken = true

EnemyController → masterShip.TakeDamage(피해)
```

배리어는 **캐릭터 앞의 공용 방패**다. 캐릭터마다 있는 게 아니다.

### 데미지 집계

`EnemyController.OnDamageDealt`가 `(damagerID, amount)` 정적 이벤트를 쏜다.
`BattleSceneInitializer`가 받아 `BattleHUDViewModel`에 누적하고, 전투 결과창의
캐릭터별 기여도와 MVP 계산에 쓴다. **테스트에서 피해를 세는 데도 이 이벤트를 쓴다.**

---

## 6. 승패 판정

### 승리 — 연결돼 있다

```
GameProgressController.Update()
   └ 매 프레임 distanceStep 누적 (m_scrollSpeedMultiplier × deltaTime)
        └ GameProgressViewModel.UpdateProgress()
             └ CurrentDistance >= TargetDistance → OnGameCleared
                  └ BattleSceneInitializer.ShowResultClear()
```

목표 거리는 `GameProgressController.m_targetDistance`(기본 2000)다. **적을 몇 마리
죽였는지는 승리와 무관하다.** 거리만 채우면 이긴다.

### 패배 — 연결돼 있지 않다

현재 코드에서 **패배 화면이 뜨는 경로가 없다.** 세 갈래가 전부 끊겨 있다.

| 신호 | 상태 |
|---|---|
| `MasterShip.OnMasterShipDestroyed` | 발행만 하고 **구독자 없음** |
| `PlayerSwapManager.OnAllPlayersDead` | 발행만 하고 **구독자 없음** |
| `BattleHUDViewModel.RequestGameOver()` | **호출자 없음** |

`ShowResultFail`은 `OnShowGameOver`에 연결돼 있고, 그 이벤트는 `RequestGameOver()`
안에서만 발행된다. 그런데 그 메서드를 부르는 곳이 없다. 즉 **모함이 파괴되거나 전원이
사망해도 아무 일도 일어나지 않는다.**

패배를 붙이려면 위 두 이벤트 중 하나를 `RequestGameOver()`에 잇는다. 배선 지점은
`BattleSceneInitializer.InitializeSceneAsync`의 이벤트 연결 구간이다.

---

## 7. 웨이브와 보스

`EnemySpawner` 300줄. `WaveConfigDTO`가 수치를, `EnemySpawnLogic`이 계산을 맡는다.

| 필드 | 뜻 |
|---|---|
| `BaseEnemyCount` | 1웨이브 적 수 |
| `CountGrowthRate` | 웨이브마다 곱해지는 증가율 |
| `BaseSpawnInterval` / `IntervalReductionRate` / `MinimumInterval` | 스폰 간격과 하한 |
| `RestDuration` | 웨이브 사이 휴식 |
| `BossArrivalWave` | 보스 등장 웨이브 |

진행할수록 **수는 늘고 간격은 줄어든다.** 하한이 있어 무한히 빨라지지는 않는다.

---

## 8. 오브젝트 풀

발사체는 초당 수십 개가 생겼다 사라진다. `ObjectPoolManager`가 재사용한다.

```csharp
pool.GetFromPool(prefab, position, rotation);   // 꺼내기
pool.ReturnToPool(gameObject);                  // 돌려주기
```

**풀에서 꺼낸 오브젝트를 `Destroy`하면 안 된다.** 풀이 죽은 참조를 들고 있게 된다.
`IPoolable.OnSpawn()` / `OnDespawn()`이 재사용 시점의 초기화 훅이다. 발사체에 상태를
들고 있다면 **반드시 `OnSpawn`에서 초기화한다.** 이전 발사의 값이 남는다.

---

## 9. 트러블슈팅

| 증상 | 원인 |
|---|---|
| 테스트가 10분 넘게 안 끝남 | `Awake`가 `timeScale = 0`으로 시작한다. `WaitForSeconds`는 만료되지 않는다. `WaitForSecondsRealtime`을 쓰고 `SetUp`에서 `timeScale = 1f` |
| 편성 순서가 안 맞음 | `SetCharacters`에 정렬을 넣었는지 확인. 순서가 곧 자리다 |
| 편성이 반영 안 됨 | `LoadData()` 호출 누락, 또는 InGame을 직접 띄워 폴백 덱을 탐 |
| 모함이 죽어도 아무 일 없음 | 패배 경로 미연결. [6절](#6-승패-판정) 참조 |
| 무기 수치를 바꿔도 안 변함 | 거동마다 읽는 필드가 다르다. [README 도메인](../../README.md#도메인) 절의 표 확인 |
| 발사체가 이전 상태를 들고 있음 | 풀 재사용. `OnSpawn`에서 초기화 |
| 발사체가 사라지지 않음 | `ReturnToPool` 대신 `Destroy`를 썼거나, 관통 카운트가 안 줄어듦 |
| 캐릭터 아이콘·이름이 전부 같음 | 여러 캐릭터가 프리팹을 공유한다. `SetIdentity` 주입 확인 |
| 씬에 배치된 캐릭터가 테스트에 걸림 | `FindObjectsByType` 대신 `PlayerSwapManager.Characters`를 본다 |
| 죽은 적에 접근해 `MissingReferenceException` | `IAttackTarget` 같은 **인터페이스 참조는 `== null`이 Unity의 파괴 검사를 타지 않는다.** `WeaponTargetQuery.IsAlive()`를 쓴다 |
| 시간이 안 흐름 | 시작 버튼을 안 눌렀다. `GameFlowPanelView.func_OnStartButtonClicked` |

---

### 인터페이스로 담은 Unity 객체는 파괴를 못 알아챈다

`UnityEngine.Object`는 파괴되면 `== null`이 `true`가 되도록 연산자가 오버로드돼 있다.
**인터페이스 타입으로 담으면 그 오버로드를 타지 않고 평범한 참조 비교가 된다.**

```csharp
IAttackTarget target = enemyController;   // EnemyController를 인터페이스로 담음
Destroy(enemyController.gameObject);

target == null                 // false — 파괴됐는데 통과한다
target.IsActiveTarget          // MissingReferenceException
WeaponTargetQuery.IsAlive(t)   // false — 이걸 써야 한다
```

전투에서 적은 계속 죽는다. 조준 중이던 적이 파괴된 프레임에 이 검사가 새면 예외가 난다.
`IAttackTarget`을 들고 있는 코드는 **반드시 `IsAlive()`로 확인한다.**

---

## 10. 무엇을 어디서 고치나

| 하고 싶은 것 | 파일 |
|---|---|
| 새 무기군 추가 | 먼저 `Resources/Weapons/`에 에셋만. 거동이 부족하면 `Player/Weapons/` |
| 공격 주기·조준 | `PlayerAttackComponent` |
| 스왑 규칙·연출 | `PlayerSwapManager`, `Player/Swap/` |
| 캐릭터 스킬 | `ActiveSkill`, `SkillCutInUI` |
| 적 행동 | `Enemy/EnemyController`, `Enemy/BossController` |
| 웨이브 난이도 | `EnemySpawner`의 `WaveConfigDTO` (인스펙터) |
| 승리 조건 | `GameProgressController.m_targetDistance`, `GameProgressViewModel` |
| 패배 조건 | 아직 없다. [6절](#6-승패-판정) |
| 전투 HUD | `BattleHUDView` + `BattleHUDViewModel` |
| 결과창 | `GameResultPanelView` + `GameResultViewModel` |
| 업그레이드 | `UpgradePanelView`, `BattleHUDViewModel.SelectUpgrade` |
| 모함·배리어 수치 | `MasterShipDTO`, `BarrierDTO` |

---

## 11. 관련 문서

- [README](../../README.md) — 컨벤션, 도메인, 디자인 패턴
- [로비 씬 가이드](lobby-scene.md) — 편성이 어떻게 만들어지는지
- [무기군별 공격 패턴 설계](../superpowers/specs/2026-08-17-weapon-attack-patterns-design.md) — **실제 구현과 다르다.** 코드를 기준으로 읽는다
