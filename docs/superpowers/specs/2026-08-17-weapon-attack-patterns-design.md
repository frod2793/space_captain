# 무기군별 공격 패턴 설계

작성일: 2026-08-17
브랜치: `shield5012/space_captain-LobbyParty-2`

## 1. 목적

무기군 카드 9종이 정의한 공격 패턴을 인게임에 구현한다. 현재는 3종만, 그것도
무기군과 연결되지 않은 채 프리팹에 박혀 있다.

## 2. 현황

`PlayerAttackComponent.cs:192`의 enum이 전부다.

```csharp
public enum PlayerAttackType { Single, Double, Spread }
```

| 무기군 | 카드가 정의한 패턴 | 고유 옵션 | 현재 |
|---|---|---|---|
| 권총 | 단발 | 없음 | 있음 (`Single`) |
| 소총 | 연사 | 공격 속도 | 부분 (`fireRate`뿐) |
| 기관총 | 고속 연사 | 예열 속도 / 최대 연사 속도 | 없음 |
| 샷건 | 산탄 | 탄환 수 / 산탄 각도 | 있음 (`Spread`) |
| 저격총 | 관통 | 관통 횟수 / 관통 피해율 | 없음 |
| 레이저 | 직선 관통 | 레이저 폭 / 관통 거리 | 없음 |
| 검 | 검기 관통 | 검기 크기 / 검기 속도 | 없음 |
| 지팡이 | 연쇄 | 연쇄 횟수 / 범위 / 피해율 | 없음 |
| 유탄 발사기 | 폭발 | 폭발 범위 | 없음 |

enum의 `Double`은 어느 카드에도 없다.

프리팹 5종의 실제 설정은 `Player_1=Double`, `Player_2=Spread`, 나머지 셋은
`Single`이다. 캐릭터 이름(소총·샷건·…)과 전혀 대응하지 않는다. `f`~`i`는
`a`의 프리팹을 공유하므로 넷 다 같은 방식으로 쏜다.

## 3. 핵심 제약: 피해 적용의 주인이 잘못돼 있다

지금은 **적이** 탄환을 읽어 자기 피해를 계산하고 탄환까지 파괴한다.

```csharp
// EnemyController.cs:144-153
else if (other.TryGetComponent<BulletProjectile>(out var bullet))
{
    if (m_enemyData.IsDead) { return; }
    TakeDamage(bullet.Damage, bullet.OwnerID);
    Destroy(bullet.gameObject);
}
```

`BossController.cs:296-303`도 같다. 문제가 둘이다.

1. **관통이 원천 차단된다.** 첫 명중에서 적이 탄환을 파괴한다.
2. **풀에서 꺼낸 오브젝트를 `Destroy`한다.** `ObjectPoolManager`가 돌려받지
   못하므로 풀이 샌다. 관통과 무관하게 이미 버그다.

관통 피해 감쇠를 넣으려 해도, 탄환이 자기 `Damage`를 낮추는 시점과 적이 그
값을 읽는 시점이 같은 물리 스텝 안에서 경합한다. 트리거 호출 순서는 보장되지
않는다.

### 결정: 피해 적용을 탄환으로 옮긴다

`EnemyController` / `BossController`의 `BulletProjectile` 분기를 삭제하고,
`BulletProjectile.OnTriggerEnter2D`가 `TakeDamage`를 호출한 뒤 관통 여부에 따라
통과하거나 풀에 반환한다.

- 판정 주체가 하나가 되어 순서가 결정적이 된다
- 풀 반환을 탄환이 책임진다
- 관통·감쇠·폭발·연쇄가 전부 이 한 지점에서 갈라진다

`EnemyBullet`(적 탄환)은 건드리지 않는다. 이 변경은 플레이어 탄환 경로에만
적용된다.

## 4. `WeaponGroupSO`

무기군마다 에셋 하나. 고유 옵션이 군마다 달라 필드는 평면으로 두고 헤더로
묶는다. 6개 서브클래스를 만드는 것보다 읽기 쉽고 에디터에서 다루기 쉽다.

```csharp
public enum WeaponAttackPattern { Single, Spread, Piercing, Beam, Explosive, Chain }

[CreateAssetMenu(fileName = "WeaponGroup", menuName = "SpaceCaptain/WeaponGroup")]
public class WeaponGroupSO : ScriptableObject
{
    [SerializeField] private string m_weaponGroupID;
    [SerializeField] private string m_displayName;
    [SerializeField] private WeaponAttackPattern m_pattern;

    [Header("공통")]
    [SerializeField] private GameObject m_projectilePrefab;
    [SerializeField] private float m_fireRate = 0.5f;
    [SerializeField] private float m_projectileSpeed = 15f;
    [SerializeField] private float m_projectileScale = 1f;

    [Header("산탄")]
    [SerializeField] private int m_bulletCount = 1;
    [SerializeField] private float m_spreadAngle = 0f;

    [Header("관통")]
    [SerializeField] private int m_pierceCount = 0;
    [SerializeField] private float m_pierceDamageFalloff = 0f;

    [Header("폭발")]
    [SerializeField] private float m_explosionRadius = 0f;

    [Header("연쇄")]
    [SerializeField] private int m_chainCount = 0;
    [SerializeField] private float m_chainRadius = 0f;
    [SerializeField] private float m_chainDamageFalloff = 0f;

    [Header("연사 가속")]
    [SerializeField] private float m_windupTime = 0f;
    [SerializeField] private float m_minFireRate = 0f;

    [Header("빔")]
    [SerializeField] private float m_beamWidth = 0f;
    [SerializeField] private float m_beamRange = 0f;
}
```

`m_pierceCount`의 의미: `0`이면 첫 명중에서 소멸, `n`이면 n체까지 관통,
`-1`이면 무제한.

`m_pierceDamageFalloff`는 명중 1회당 곱해지는 감쇠율이다. `0.2`면 두 번째
적은 80%, 세 번째는 64%를 받는다.

`CharacterDataSO`에 `m_weaponGroup` 참조를 추가한다. 지금은 캐릭터↔무기군이
1:1이다. 장착 전환이 생기면 참조를 읽는 출처만 바뀌고 실행부는 그대로다.

## 5. 패턴은 6종이면 9개 무기군을 덮는다

**검이 관통으로 흡수된다.** 카드의 "검기 관통 / 검기 크기 / 검기 속도"는 크고
느린 관통 발사체다. 소총·기관총은 단발의 연사 속도 차이이고, 기관총만 예열
가속이 붙는다.

| 패턴 | 무기군 | 동작 |
|---|---|---|
| `Single` | 권총, 소총, 기관총 | 조준 방향으로 1발 |
| `Spread` | 샷건 | 부채꼴로 `bulletCount`발 |
| `Piercing` | 저격총, 검 | 관통 카운트만큼 통과하며 감쇠 |
| `Beam` | 레이저 | 히트스캔. 폭×사거리 직사각형 안의 적 전부 |
| `Explosive` | 유탄 발사기 | 명중 지점 반경 안 전부 |
| `Chain` | 지팡이 | 명중 후 인접 적으로 `chainCount`회 전파, 감쇠 |

기관총의 예열은 패턴이 아니라 발사 주기 계산에 넣는다. 연속 발사가 이어지면
`fireRate`가 `windupTime`에 걸쳐 `minFireRate`까지 줄고, 발사가 끊기면 되돌아온다.

## 6. 실행부

`Player/Swap/`의 `ISwapStrategy` 구조가 이미 자리잡았으므로 `Player/Attack/`에
같은 모양으로 둔다.

```csharp
public interface IAttackPattern
{
    void Fire(AttackContextDTO context);
}

public class AttackContextDTO
{
    public PlayerCharacterController Owner;
    public WeaponGroupSO Weapon;
    public Transform[] FirePoints;
    public IAttackTarget Target;
    public float BaseAngle;
    public int Damage;
    public float DamageMultiplier;   // 비활성 캐릭터 0.5배
    public ObjectPoolManager Pool;
}
```

구현 6개: `SingleAttackPattern`, `SpreadAttackPattern`, `PiercingAttackPattern`,
`BeamAttackPattern`, `ExplosiveAttackPattern`, `ChainAttackPattern`.

`PlayerAttackComponent`는 조준·발사 주기·컨텍스트 구성만 맡고 발사 자체를 패턴에
위임한다.

### 하위 호환

무기군이 주입되지 않으면 기존 직렬화 필드(`m_attackType`, `m_bulletPrefab`,
`m_fireRate`)로 동작한다. 씬에 미리 배치된 캐릭터와 기존 프리팹 5종이 깨지지
않는다. `PlayerAttackType`은 남겨두되 `Double`은 어느 무기군도 쓰지 않는다.

## 7. 주입 경로

`BattleSceneInitializer`가 스폰 직후 `SetIdentity`와 나란히 호출한다.

```csharp
controller.SetIdentity(charData.CharacterName, charData.UI_Icon);

if (controller.TryGetComponent<PlayerAttackComponent>(out var attack))
{
    attack.SetWeapon(charData.WeaponGroup);
}
```

## 8. 에셋 생성

`WeaponGroupBuilder` 에디터 스크립트가 9개 `WeaponGroupSO`를 카드 수치로
만들고 `CharacterDataSO`에 연결한다. `CharacterRosterBuilder`와 같은 방식으로
멱등하게 동작한다. 이미 있는 에셋은 덮어쓰지 않는다.

초기 수치는 카드의 정성 서술을 옮긴 것이다. 밸런스 튜닝은 별도 작업이다.

| 무기군 | 패턴 | 주요 수치 |
|---|---|---|
| 권총 | Single | fireRate 0.5 |
| 소총 | Single | fireRate 0.15 |
| 기관총 | Single | fireRate 0.2, windup 3, minFireRate 0.05 |
| 샷건 | Spread | bulletCount 5, spread 60 |
| 저격총 | Piercing | fireRate 1.2, pierce 3, falloff 0.2 |
| 검 | Piercing | fireRate 1.5, pierce -1, scale 3, speed 6 |
| 레이저 | Beam | fireRate 0.8, beamWidth 1.5, beamRange 20 |
| 유탄 발사기 | Explosive | fireRate 1.0, explosionRadius 3 |
| 지팡이 | Chain | fireRate 0.7, chainCount 3, chainRadius 4, falloff 0.3 |

## 9. 검증

`EnemyController.OnDamageDealt`가 `(damagerID, amount)` 정적 이벤트라 패턴의
계약을 실제로 측정할 수 있다. 더미 적을 배치하고 발사한 뒤 이벤트를 센다.

| 테스트 | 검증 |
|---|---|
| 관통 통과 | 일렬 3기 → 피해 3회 (`pierceCount=3`) |
| 관통 한계 | 일렬 4기, `pierceCount=2` → 피해 3회에서 멈춤 |
| 관통 감쇠 | 두 번째 피해 < 첫 번째 |
| 비관통 소멸 | `pierceCount=0` → 첫 적만 피격 |
| 폭발 범위 | 반경 안 2기 피격, 밖 1기 미피격 |
| 연쇄 전파 | 명중 1기 + 인접 `chainCount`기 피격, 감쇠 적용 |
| 빔 직선 | 직선상 적만 피격, 폭 밖은 미피격 |
| 산탄 수 | 발사된 탄환 수 == `bulletCount` |
| 풀 반환 | 관통 종료 후 탄환이 풀로 반환됨 (`Destroy` 아님) |
| 하위 호환 | 무기군 미주입 시 기존 `m_attackType`대로 동작 |

## 10. 이번 범위 밖

| 항목 | 이유 |
|---|---|
| 무기 등급·옵션·인벤토리 | 데이터 모델 없음 |
| 장착 전환 UI | 캐릭터↔무기군 1:1로 충분 |
| 무기군별 아트 | 빔·검기·폭발은 기존 `Skill_Laser` / `HomingMissile` 이펙트를 플레이스홀더로 재사용 |
| 밸런스 튜닝 | 카드 서술을 옮긴 초기값으로 시작 |
| `EnemyBullet` 정리 | 적 탄환은 이 변경과 무관 |
