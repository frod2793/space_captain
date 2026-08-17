# 무기군별 공격 패턴 — 구현 기록

> **이 문서는 계획서가 아니라 기록이다.** 무기 시스템은 이미 구현됐다.
> 원래의 단계별 계획서(`WeaponGroupSO` + `IAttackPattern` 전제)는 실행되지 않았고
> 다른 설계로 구현이 들어갔다. 그 계획서는 git 히스토리에 남아 있다.
> **새로 무기를 다룰 사람은 이 문서와 코드를 기준으로 읽는다.**

**설계 문서:** [`2026-08-17-weapon-attack-patterns-design.md`](../specs/2026-08-17-weapon-attack-patterns-design.md)
— 실제 구현과 이름·구조가 다르다. 결정의 배경을 볼 때만 참고한다.

---

## 1. 무엇이 만들어졌나

`Assets/_Game/Scripts/InGame/Player/Weapons/`

| 파일 | 역할 |
|---|---|
| `WeaponDataSO` | 무기군 데이터. 거동 종류 + 수치 전부 |
| `IWeaponBehaviour` | 발사 인터페이스. `WeaponFireContext` struct를 `in`으로 받는다 |
| `StraightWeapon` | 직선 발사체. 단발·연사·산탄·관통·검기를 전부 담당 |
| `BeamWeapon` | 히트스캔 빔 |
| `ExplosiveWeapon` | 착탄 범위 피해 |
| `ChainWeapon` | 명중 후 인접 적으로 연쇄 |
| `ProjectileLauncher` | 발사체 생성 공통 절차 (정적) |
| `WeaponTargetQuery` | 물리 질의. NonAlloc 버퍼 재사용 (정적) |
| `WeaponCatalog` | ID로 무기 에셋 조회 (정적, 캐시) |
| `ExplosiveProjectile` | 폭발 발사체 |

데이터는 `Assets/_Game/Resources/Weapons/`에 9개 에셋으로 있다.

---

## 2. 설계와 달라진 점

| 설계 문서 | 실제 구현 | 어느 쪽이 맞나 |
|---|---|---|
| `WeaponGroupSO` | `WeaponDataSO` | 이름만 다름 |
| `WeaponAttackPattern` 6종 | `WeaponBehaviourType` 4종 | **구현이 맞다.** 단발·연사·산탄·관통은 같은 직선 발사라 수치로 갈리면 충분하다 |
| `IAttackPattern` | `IWeaponBehaviour` | 이름만 다름 |
| `AttackContextDTO` (class) | `WeaponFireContext` (struct, `in`) | **구현이 맞다.** 발사마다 힙 할당이 사라진다 |
| `AreaDamage` (`OverlapCircleAll`) | `WeaponTargetQuery` (`*NonAlloc`) | **구현이 맞다.** 질의마다 배열 할당이 사라진다 |
| `PierceCount` | `MaxTargets` | 이름만 다름. `-1`이면 무제한 |
| — | `WeaponCatalog`, `ExplosiveProjectile` | 구현이 추가 |

설계에서 살아남은 핵심 결정은 셋이다.

- **피해 적용의 주인을 탄환으로 옮긴다** — 적·보스에서 탄환 분기를 지웠다
- **관통은 명중 카운트로 센다** — 발사체가 스스로 세고 반환한다
- **검은 새 패턴이 아니라 크고 느린 관통체다**

---

## 3. 성능 원칙

전투 중 발사가 초당 수십 회다. 기관총은 `MaxFireRate 0.06`이라 캐릭터당 초당 약 17회다.
**발사 경로에서 힙 할당을 만들지 않는다.**

| 지점 | 방식 |
|---|---|
| 컨텍스트 전달 | `struct` + `in` — 복사도 할당도 없다 |
| 물리 질의 | `WeaponTargetQuery`의 정적 버퍼 + `Physics2D.*NonAlloc` |
| 중복 판정 | 거동의 `readonly HashSet<int>` 인스턴스 필드 재사용. `Clear()`만 한다 |
| 거동 인스턴스 | 무기 주입 시 1회 생성해 `PlayerAttackComponent.m_behaviour`에 보관 |
| 발사체 | `ObjectPoolManager` 재사용 |
| 공통 절차 | `ProjectileLauncher` 정적 메서드 |

**`Physics2D.OverlapCircleAll` / `OverlapBoxAll`을 쓰지 않는다.** 호출마다 배열을 새로
할당한다. `WeaponTargetQuery`를 거친다.

**거동 안에서 다른 거동을 `new` 하지 않는다.** `ChainWeapon`이 `StraightWeapon`을
쓰지만 정적 필드로 공유한다. 거동은 상태가 없으므로 안전하다.

---

## 4. 발사 흐름

```
PlayerAttackComponent.Update()
   └ CurrentFireRate 마다
        └ WeaponFireContext 구성 (스택)
             └ m_behaviour.Fire(in ctx)
                  ├ Straight  → ProjectileLauncher.Spawn → BulletProjectile 필드 주입
                  ├ Explosive → ProjectileLauncher.Spawn → ExplosiveProjectile.Initialize
                  ├ Chain     → StraightWeapon으로 1발, OnProjectileHit에서 연쇄
                  └ Beam      → 발사체 없이 WeaponTargetQuery.BoxCast로 즉시 판정
```

`ProjectileLauncher`가 맡는 공통 절차는 넷이다 — 부채꼴 각도 계산, 발사 위치 결정,
풀에서 꺼내 활성화·스케일, 못 쓰는 오브젝트 반환. **어떤 컴포넌트를 어떻게 초기화할지는
거동이 각자 정한다.**

---

## 5. 무기군 9종과 거동 매핑

| 무기군 | 에셋 | 거동 | 실제 설정값 |
|---|---|---|---|
| 권총 | `pistol` | Straight | `FireRate 0.5` |
| 소총 | `rifle` | Straight | `FireRate 0.15` |
| 기관총 | `machine-gun` | Straight | `FireRate 0.3` → `WarmupTime 2`s → `MaxFireRate 0.06` |
| 샷건 | `shotgun` | Straight | `BulletCount 5`, `SpreadAngle 60`, `DamageFalloffRate 0.6` |
| 저격총 | `sniper-rifle` | Straight | `MaxTargets 3`, `PierceDamageRate 0.8`, `FireRate 1.5` |
| 검 | `sword` | Straight | `MaxTargets -1`, `ProjectileScale 3`, `FireRate 1.5` |
| 레이저 | `laser` | Beam | `BeamWidth 1.5`, `BeamRange 20` |
| 유탄 발사기 | `grenade-launcher` | Explosive | `ExplosionRadius 2.5`, `FireRate 1.2` |
| 지팡이 | `staff` | Chain | `ChainCount 3`, `ChainRange 4`, `ChainDamageRate 0.7`, `DamageMultiplier 0.6` |

**거동 4종이 무기군 9종을 덮는다.** `Straight` 하나가 6종을 맡는다.

거동마다 읽는 필드가 다르다. 엉뚱한 필드를 채워도 아무 일도 일어나지 않는다 —
[README 도메인](../../../README.md#도메인) 절의 표를 본다.

---

## 6. 새 무기군을 추가하려면

1. **에셋만 추가해본다.** `Resources/Weapons/`에 `WeaponDataSO`를 만들고 기존 거동
   + 수치 조합으로 되는지 확인한다. 9종 중 6종이 이렇게 만들어졌다.
2. 정말 새 거동이 필요하면 `IWeaponBehaviour` 구현을 추가하고
   `PlayerAttackComponent.CreateBehaviour()`의 `switch`에 한 줄을 넣는다.
3. 발사체를 쓰는 거동이면 **생성은 `ProjectileLauncher`를 거친다.** 직접
   `GetFromPool`을 부르지 않는다.
4. 상태를 들고 있다면 인스턴스 필드로 재사용한다. 발사마다 `new` 하지 않는다.
5. `CharacterDataSO.DefaultWeapon`에 연결하면 스폰 시 자동 주입된다.

---

## 7. 남은 것

| 항목 | 상태 |
|---|---|
| 무기 등급·옵션·인벤토리 | 없음. 캐릭터↔무기 1:1 |
| 장착 전환 UI | 없음 |
| 무기군별 아트 | 빔·연쇄는 `SkillLaser` 비주얼을 재사용 중 |
| 밸런스 튜닝 | 초기값 상태 |
| 검수 보완 | [별도 문서](2026-08-17-weapon-attack-patterns-review-remediation.md) |
