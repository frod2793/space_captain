# 무기 공격 패턴 검수 보완 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sol 검수에서 확인된 기본 무기 연결, 카드 수치, 풀 재사용 상태, 무기 주입 경계를 최소 변경으로 보완한다.

**Architecture:** 기존 `WeaponDataSO`·`BulletProjectile`·`PlayerAttackComponent` 구조를 유지한다. 새 행동 타입·빌더·UI는 만들지 않고, 에셋과 기존 경로의 초기화만 바로잡는다.

**Tech Stack:** Unity 6000.3.19f1, C#, NUnit PlayMode reflection tests.

## Global Constraints

- 기존 사용자 dirty 변경을 보존한다. 커밋·푸시·패키지 변경 금지.
- 테스트는 `TestReflectionHelper`와 reflection만 사용하며 production 타입을 직접 참조하지 않는다.
- Unity Editor/MCP가 없으면 batch Unity를 실행하지 않고 PlayMode/컴파일을 `not_run`으로 기록한다.
- `git diff --check`와 대상 에셋/참조 정적 검증은 반드시 실행한다.

---

### Task 1: 기본 무기 연결과 카드 수치 교정

**Files:**
- Modify: `Assets/_Game/Resources/d_CharacterData.asset`
- Modify: `Assets/_Game/Resources/e_CharacterData.asset`
- Modify: `Assets/_Game/Resources/Weapons/{rifle,laser,sword,staff}.asset`
- Test: `Assets/_Game/Tests/PlayMode/PlayerAttackComponentTests.cs`

- [ ] reflection 테스트로 `d_CharacterData`의 WeaponID가 `rifle`, `e_CharacterData`의 WeaponID가 `shotgun`임을 검증한다.
- [ ] 해당 테스트를 실행해 현재 GUID 교차 연결로 실패함을 확인한다. Unity 실행 불가 시 `not_run`으로 기록한다.
- [ ] d/e의 `m_defaultWeapon` GUID만 서로 교환하고 YAML 형식을 보존한다.
- [ ] 설계 표 값으로 rifle `FireRate=0.15`, laser `FireRate=0.8/BeamWidth=1.5`, sword `FireRate=1.5/MaxTargets=-1/ProjectileSpeed=6`, staff `FireRate=0.7/ChainCount=3/ChainRange=4`를 교정한다.
- [ ] 대상 WeaponID·필드 값과 GUID 9/9를 정적으로 확인한다.

### Task 2: 풀 재사용과 legacy 발사 초기화

**Files:**
- Modify: `Assets/_Game/Scripts/Player/BulletProjectile.cs`
- Modify: `Assets/_Game/Scripts/Player/PlayerAttackComponent.cs`
- Test: `Assets/_Game/Tests/PlayMode/BulletProjectileTests.cs`

- [ ] reflection 테스트로 관통 무기로 사용 후 풀 반환된 탄환이 legacy 발사 시 legacy 속도·기본 사거리·스케일을 갖는지 검증한다.
- [ ] 테스트를 실행해 이전 무기 상태가 남아 실패함을 확인한다. Unity 실행 불가 시 `not_run`으로 기록한다.
- [ ] `BulletProjectile`이 직렬화된 기본 속도·사거리를 복원하는 최소 public 초기화 메서드를 제공하고, `OnDespawn`에서 무기 전용 상태와 함께 복원한다.
- [ ] `FireLegacy`가 해당 초기화를 호출하고 기존과 동일한 active/inactive 스케일 및 `m_bulletSpeed`를 명시 적용한다.
- [ ] 기존 weapon 발사 경로는 자신의 speed/range/scale을 계속 덮어쓰는지 정적으로 확인한다.

### Task 3: 스탯 누락 캐릭터의 무기 주입 보장

**Files:**
- Modify: `Assets/_Game/Scripts/UI/BattleSceneInitializer.cs`
- Test: `Assets/_Game/Tests/PlayMode/PlayerAttackComponentTests.cs`

- [ ] reflection 테스트로 `BaseStats=null`인 CharacterData의 `DefaultWeapon`이 `PlayerAttackComponent.SetWeapon`에 전달됨을 검증한다.
- [ ] 테스트를 실행해 현재 `BaseStats` 조건 안의 주입으로 실패함을 확인한다. Unity 실행 불가 시 `not_run`으로 기록한다.
- [ ] `SetWeapon` 호출을 초기 스탯 주입 조건 밖으로 옮기되, 기존 `SetIdentity`와 사용자 변경 hunk를 보존한다.
- [ ] `git diff --check`, `m_defaultWeapon` GUID 9개, 대상 수치, legacy 초기화 호출을 정적으로 검증한다.
