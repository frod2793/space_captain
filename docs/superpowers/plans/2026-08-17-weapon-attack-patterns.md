# 무기군별 공격 패턴 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 무기군 카드 9종이 정의한 공격 패턴을 인게임에 구현한다.

**Architecture:** 피해 적용의 주인을 적에서 탄환으로 옮겨 관통·감쇠·폭발·연쇄가 갈라질 단일 지점을 만든다. 무기군 데이터는 `WeaponGroupSO` 에셋으로 빼고 `CharacterDataSO`가 참조한다. 발사 자체는 `Player/Swap/`의 `ISwapStrategy` 구조를 그대로 따라 `Player/Attack/`의 `IAttackPattern` 6종에 위임하며, 무기군이 주입되지 않으면 기존 직렬화 필드로 동작해 하위 호환을 유지한다.

**Tech Stack:** Unity 6000.3.19f1, C#, Physics2D (OverlapCircle/OverlapBox), NUnit PlayMode 테스트

**설계 문서:** `docs/superpowers/specs/2026-08-17-weapon-attack-patterns-design.md`

## Global Constraints

- 새 패키지·새 의존성 추가 금지.
- 코딩 컨벤션은 기존 코드를 따른다: private 필드 `m_` 접두사, 중괄호는 항상 새 줄, `if` 본문 한 줄이어도 중괄호 사용, 주석은 한국어.
- **테스트에서 게임 코드 타입을 직접 참조하지 않는다.** `Game.Tests.asmdef`는 `Assembly-CSharp`를 참조할 수 없다. 타입은 `TestReflectionHelper.GetGameType(...)`으로 얻어 리플렉션으로 다룬다.
- **테스트 메서드 이름은 숫자로 시작할 수 없다.** `3명만_...`은 컴파일 오류다. `세명만_...`처럼 쓴다.
- **PlayMode 테스트에서 `WaitForSeconds`를 쓰지 않는다.** 전투 씬이 `Time.timeScale = 0`으로 시작하므로 영원히 만료되지 않는다. `WaitForSecondsRealtime`을 쓰고, `SetUp`에서 `Time.timeScale = 1f`로 되돌린다.
- `EnemyBullet`(적 탄환)은 건드리지 않는다. 이 작업은 플레이어 탄환 경로에만 적용된다.
- 적/보스의 태그는 `"Enemy"`, `"Boss"`다. `TakeDamage`의 시그니처는 양쪽 모두 `public void TakeDamage(int amount, string damagerID = null)`이다.
- 무기군 9종의 표시 이름은 정확히 이 문자열을 쓴다: `소총`, `샷건`, `권총`, `레이저`, `저격총`, `기관총`, `검`, `지팡이`, `유탄 발사기`.
- 새 `.cs`/`.asset` 파일을 만들면 Unity가 `.meta`를 생성한다. 커밋할 때 `.meta`도 같이 `git add` 한다.

## 실행 환경

```bash
export UNITY="/Users/woodenshield/Desktop/UNITY/Editor/6000.3.19f1/Unity.app/Contents/MacOS/Unity"
export PROJ="/Users/woodenshield/Desktop/UNITY/Projects/space_captain/space_captain-LobbyParty-2"
```

**중요:** batchmode는 Unity 에디터 GUI가 이 프로젝트를 열고 있으면 `Library` 락 때문에 실패한다. 실행 전 에디터를 닫는다.

**테스트 실행:**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "<클래스명>" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "error CS|Exiting with code"
```

**결과 확인** (통과/실패 수와 실패 사유):

```bash
python3 -c "
import re
s=open('$PROJ/Logs/test-results.xml',encoding='utf-8').read()
print(re.search(r'total=\"\d+\" passed=\"\d+\" failed=\"\d+\"',s).group(0))
for m in re.finditer(r'<test-case([^>]*result=\"Failed\"[^>]*)>(.*?)</test-case>', s, re.S):
    n=re.search(r'name=\"([^\"]+)\"',m.group(1))
    t=re.search(r'<message>(?:<!\[CDATA\[)?(.*?)(?:\]\]>)?</message>', m.group(2), re.S)
    print(' -', n.group(1) if n else '?', '::', (t.group(1).strip()[:300] if t else ''))
"
```

**컴파일만 확인:**

```bash
"$UNITY" -batchmode -quit -projectPath "$PROJ" -logFile - 2>&1 | grep -E "error CS"
```

출력이 없으면 성공이다.

**주의:** 전투 씬을 띄우는 테스트는 느리다. 전체 스위트가 5~8분 걸린다. 10분을 넘기면 멈춘 것이므로 `Time.timeScale` 관련 대기를 의심한다.

---

### Task 1: 피해 적용을 탄환으로 이전

관통의 전제 조건이자 풀링 버그 수정. 다른 모든 태스크가 이 위에 얹힌다.

**Files:**
- Modify: `Assets/_Game/Scripts/Player/BulletProjectile.cs`
- Modify: `Assets/_Game/Scripts/Enemy/EnemyController.cs:144-153`
- Modify: `Assets/_Game/Scripts/Enemy/BossController.cs:296-303`
- Create: `Assets/_Game/Tests/PlayMode/BulletDamageTests.cs`

**Interfaces:**
- Consumes: 없음
- Produces:
  - `BulletProjectile.Damage` (`int`, get/set) — 기존 유지
  - `BulletProjectile.OwnerID` (`string`, get/set) — 기존 유지
  - `BulletProjectile.PierceCount` (`int`, get/set) — `0`이면 첫 명중에 소멸, `n`이면 n체 관통, `-1`이면 무제한
  - `BulletProjectile.PierceDamageFalloff` (`float`, get/set) — 명중 1회당 곱해지는 감쇠율. `0.2f`면 다음 적은 80%
  - `BulletProjectile.OnHitTarget` (`static event Action<string, int>`) — 테스트용. `(ownerID, appliedDamage)`

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/_Game/Tests/PlayMode/BulletDamageTests.cs`:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BulletDamageTests
{
    private const BindingFlags ANY_INSTANCE =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private GameObject m_stage;
    private readonly List<int> m_damages = new List<int>();
    private Delegate m_handler;
    private EventInfo m_hitEvent;

    private static Type T(string name)
    {
        Type type = TestReflectionHelper.GetGameType(name);
        Assert.IsNotNull(type, $"{name} 타입을 찾을 수 없다");
        return type;
    }

    private static void SetProp(object target, string name, object value)
    {
        PropertyInfo prop = target.GetType().GetProperty(name, ANY_INSTANCE);
        Assert.IsNotNull(prop, $"프로퍼티 {name}을 찾을 수 없다");
        prop.SetValue(target, value);
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, ANY_INSTANCE);
        Assert.IsNotNull(field, $"필드 {name}을 찾을 수 없다");
        field.SetValue(target, value);
    }

    /// <summary>피격만 세는 최소 표적. 실제 EnemyController 대신 쓴다.</summary>
    private GameObject MakeTarget(Vector3 position)
    {
        var go = new GameObject("Target");
        go.tag = "Enemy";
        go.transform.SetParent(m_stage.transform);
        go.transform.position = position;

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.5f, 0.5f);

        return go;
    }

    private object MakeBullet(Vector3 position, int damage, int pierceCount, float falloff)
    {
        var go = new GameObject("Bullet");
        go.transform.SetParent(m_stage.transform);
        go.transform.position = position;

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.2f, 0.2f);

        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        object bullet = go.AddComponent(T("BulletProjectile"));
        SetProp(bullet, "Damage", damage);
        SetProp(bullet, "OwnerID", "tester");
        SetProp(bullet, "PierceCount", pierceCount);
        SetProp(bullet, "PierceDamageFalloff", falloff);
        SetField(bullet, "m_speed", 20f);
        SetField(bullet, "m_lifeTime", 5f);
        SetField(bullet, "m_maxRange", 50f);

        return bullet;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        m_damages.Clear();
        m_stage = new GameObject("BulletTestStage");

        m_hitEvent = T("BulletProjectile").GetEvent("OnHitTarget",
            BindingFlags.Static | BindingFlags.Public);
        Assert.IsNotNull(m_hitEvent, "BulletProjectile.OnHitTarget 정적 이벤트가 없다");

        Action<string, int> record = (ownerID, amount) => m_damages.Add(amount);
        m_handler = record;
        m_hitEvent.AddEventHandler(null, m_handler);

        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        if (m_hitEvent != null && m_handler != null)
        {
            m_hitEvent.RemoveEventHandler(null, m_handler);
        }

        if (m_stage != null)
        {
            UnityEngine.Object.DestroyImmediate(m_stage);
        }
    }

    [UnityTest]
    public IEnumerator 관통이_0이면_첫_적만_맞춘다()
    {
        MakeTarget(new Vector3(0f, 1f, 0f));
        MakeTarget(new Vector3(0f, 2f, 0f));
        MakeBullet(Vector3.zero, 100, 0, 0f);

        yield return new WaitForSecondsRealtime(0.5f);

        Assert.AreEqual(1, m_damages.Count, "관통 0인데 두 번 이상 맞췄다");
    }

    [UnityTest]
    public IEnumerator 관통_횟수만큼_통과한다()
    {
        MakeTarget(new Vector3(0f, 1f, 0f));
        MakeTarget(new Vector3(0f, 2f, 0f));
        MakeTarget(new Vector3(0f, 3f, 0f));
        MakeTarget(new Vector3(0f, 4f, 0f));
        MakeBullet(Vector3.zero, 100, 2, 0f);

        yield return new WaitForSecondsRealtime(0.6f);

        Assert.AreEqual(3, m_damages.Count, "관통 2면 3기까지 맞아야 한다");
    }

    [UnityTest]
    public IEnumerator 관통_피해가_감쇠한다()
    {
        MakeTarget(new Vector3(0f, 1f, 0f));
        MakeTarget(new Vector3(0f, 2f, 0f));
        MakeBullet(Vector3.zero, 100, 2, 0.2f);

        yield return new WaitForSecondsRealtime(0.6f);

        Assert.AreEqual(2, m_damages.Count);
        Assert.AreEqual(100, m_damages[0], "첫 명중은 감쇠가 없어야 한다");
        Assert.AreEqual(80, m_damages[1], "두 번째는 80%여야 한다");
    }
}
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "BulletDamageTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "error CS|Exiting with code"
```

기대: `BulletProjectile.OnHitTarget 정적 이벤트가 없다`로 3개 모두 FAIL.

- [ ] **Step 3: BulletProjectile 구현**

`Assets/_Game/Scripts/Player/BulletProjectile.cs` 전체를 교체:

```csharp
using System;
using UnityEngine;

public class BulletProjectile : MonoBehaviour
{
    [SerializeField] private float m_speed = 15f;
    [SerializeField] private float m_lifeTime = 3f;
    [SerializeField] private float m_maxRange = 10f;

    private float m_timer;
    private ObjectPoolManager m_pool;
    private Vector3 m_startPosition;
    private int m_hitCount;

    public int Damage { get; set; }
    public string OwnerID { get; set; }

    /// <summary>0이면 첫 명중에 소멸, n이면 n체 관통, -1이면 무제한.</summary>
    public int PierceCount { get; set; }

    /// <summary>명중 1회당 곱해지는 감쇠율. 0.2f면 다음 적은 80%를 받는다.</summary>
    public float PierceDamageFalloff { get; set; }

    /// <summary>(OwnerID, 실제로 적용된 피해). 검증용.</summary>
    public static event Action<string, int> OnHitTarget;

    private void OnEnable()
    {
        m_timer = 0f;
        m_hitCount = 0;
        m_startPosition = transform.position;

        if (m_pool == null)
        {
            m_pool = FindAnyObjectByType<ObjectPoolManager>();
        }
    }

    private void Update()
    {
        transform.Translate(Vector3.up * m_speed * Time.deltaTime);

        m_timer += Time.deltaTime;

        if (m_timer >= m_lifeTime || Vector3.Distance(m_startPosition, transform.position) >= m_maxRange)
        {
            Release();
        }
    }

    public void SetSpeed(float speed)
    {
        m_speed = speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy") && !collision.CompareTag("Boss"))
        {
            return;
        }

        // 피해 적용의 주인은 탄환이다. 적이 적용하면 관통 시 감쇠 시점이
        // 트리거 호출 순서에 좌우되고, 적이 풀 오브젝트를 Destroy해버린다.
        int applied = CurrentDamage();
        ApplyDamage(collision, applied);

        OnHitTarget?.Invoke(OwnerID, applied);
        m_hitCount++;

        if (PierceCount >= 0 && m_hitCount > PierceCount)
        {
            Release();
        }
    }

    private int CurrentDamage()
    {
        if (m_hitCount == 0 || PierceDamageFalloff <= 0f)
        {
            return Damage;
        }

        float scale = Mathf.Pow(1f - PierceDamageFalloff, m_hitCount);
        return Mathf.RoundToInt(Damage * scale);
    }

    private void ApplyDamage(Collider2D collision, int amount)
    {
        if (collision.TryGetComponent<EnemyController>(out var enemy))
        {
            enemy.TakeDamage(amount, OwnerID);
            return;
        }

        if (collision.TryGetComponent<BossController>(out var boss))
        {
            boss.TakeDamage(amount, OwnerID);
        }
    }

    private void Release()
    {
        if (m_pool != null)
        {
            m_pool.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
```

- [ ] **Step 4: EnemyController에서 탄환 분기 제거**

`Assets/_Game/Scripts/Enemy/EnemyController.cs`에서 아래 블록을 통째로 삭제한다:

```csharp
        else if (other.TryGetComponent<BulletProjectile>(out var bullet))
        {
            if (m_enemyData.IsDead)
            {
                return;
            }

            TakeDamage(bullet.Damage, bullet.OwnerID);
            Destroy(bullet.gameObject);
        }
```

- [ ] **Step 5: BossController에서 탄환 분기 제거**

`Assets/_Game/Scripts/Enemy/BossController.cs`에서 아래 블록을 통째로 삭제한다:

```csharp
        else if (other.TryGetComponent<BulletProjectile>(out var bullet))
        {
            TakeDamage(bullet.Damage, bullet.OwnerID);
            if (bullet.gameObject != null)
            {
                Destroy(bullet.gameObject);
            }
        }
```

- [ ] **Step 6: 테스트 통과 확인**

Step 2와 같은 명령. 기대: 3개 모두 PASS.

- [ ] **Step 7: 전체 회귀 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 | grep -E "error CS|Exiting with code"
```

기대: 기존 테스트 전부 PASS. 전투 씬 테스트가 있어 5~8분 걸린다.

- [ ] **Step 8: 커밋**

```bash
git add Assets/_Game/Scripts/Player/BulletProjectile.cs \
        Assets/_Game/Scripts/Enemy/EnemyController.cs \
        Assets/_Game/Scripts/Enemy/BossController.cs \
        Assets/_Game/Tests/PlayMode/BulletDamageTests.cs \
        Assets/_Game/Tests/PlayMode/BulletDamageTests.cs.meta
git commit -m "피해 적용의 주인을 적에서 탄환으로 이전, 관통 지원 추가"
```

---

### Task 2: WeaponGroupSO

무기군 데이터 그릇. 값이 없어도 컴파일되고 다음 태스크들이 참조한다.

**Files:**
- Create: `Assets/_Game/Scripts/Models/WeaponGroupSO.cs`
- Modify: `Assets/_Game/Scripts/Models/CharacterDataSO.cs`
- Create: `Assets/_Game/Tests/PlayMode/WeaponGroupTests.cs`

**Interfaces:**
- Consumes: 없음
- Produces:
  - `enum WeaponAttackPattern { Single, Spread, Piercing, Beam, Explosive, Chain }`
  - `WeaponGroupSO` 공개 프로퍼티: `WeaponGroupID`(string), `DisplayName`(string), `Pattern`(WeaponAttackPattern), `ProjectilePrefab`(GameObject), `FireRate`(float), `ProjectileSpeed`(float), `ProjectileScale`(float), `BulletCount`(int), `SpreadAngle`(float), `PierceCount`(int), `PierceDamageFalloff`(float), `ExplosionRadius`(float), `ChainCount`(int), `ChainRadius`(float), `ChainDamageFalloff`(float), `WindupTime`(float), `MinFireRate`(float), `BeamWidth`(float), `BeamRange`(float)
  - `CharacterDataSO.WeaponGroup` (`WeaponGroupSO`, 읽기 전용 프로퍼티), 뒤에 있는 필드는 `m_weaponGroup`

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/_Game/Tests/PlayMode/WeaponGroupTests.cs`:

```csharp
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WeaponGroupTests
{
    private const BindingFlags ANY_INSTANCE =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static Type T(string name)
    {
        Type type = TestReflectionHelper.GetGameType(name);
        Assert.IsNotNull(type, $"{name} 타입을 찾을 수 없다");
        return type;
    }

    private static object GetProp(object target, string name)
    {
        PropertyInfo prop = target.GetType().GetProperty(name, ANY_INSTANCE);
        Assert.IsNotNull(prop, $"프로퍼티 {name}을 찾을 수 없다");
        return prop.GetValue(target);
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, ANY_INSTANCE);
        Assert.IsNotNull(field, $"필드 {name}을 찾을 수 없다");
        field.SetValue(target, value);
    }

    [Test]
    public void 패턴_enum은_6종이다()
    {
        Type pattern = T("WeaponAttackPattern");
        string[] names = Enum.GetNames(pattern);

        CollectionAssert.AreEquivalent(
            new[] { "Single", "Spread", "Piercing", "Beam", "Explosive", "Chain" },
            names);
    }

    [Test]
    public void WeaponGroupSO의_값이_프로퍼티로_노출된다()
    {
        var weapon = ScriptableObject.CreateInstance(T("WeaponGroupSO"));

        try
        {
            SetField(weapon, "m_weaponGroupID", "sniper");
            SetField(weapon, "m_displayName", "저격총");
            SetField(weapon, "m_pattern", Enum.Parse(T("WeaponAttackPattern"), "Piercing"));
            SetField(weapon, "m_fireRate", 1.2f);
            SetField(weapon, "m_pierceCount", 3);
            SetField(weapon, "m_pierceDamageFalloff", 0.2f);

            Assert.AreEqual("sniper", GetProp(weapon, "WeaponGroupID"));
            Assert.AreEqual("저격총", GetProp(weapon, "DisplayName"));
            Assert.AreEqual("Piercing", GetProp(weapon, "Pattern").ToString());
            Assert.AreEqual(1.2f, (float)GetProp(weapon, "FireRate"), 0.001f);
            Assert.AreEqual(3, GetProp(weapon, "PierceCount"));
            Assert.AreEqual(0.2f, (float)GetProp(weapon, "PierceDamageFalloff"), 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(weapon);
        }
    }

    [Test]
    public void CharacterDataSO가_무기군을_참조한다()
    {
        var character = ScriptableObject.CreateInstance(T("CharacterDataSO"));
        var weapon = ScriptableObject.CreateInstance(T("WeaponGroupSO"));

        try
        {
            SetField(weapon, "m_displayName", "샷건");
            SetField(character, "m_weaponGroup", weapon);

            object linked = GetProp(character, "WeaponGroup");

            Assert.IsNotNull(linked, "CharacterDataSO.WeaponGroup이 null이다");
            Assert.AreEqual("샷건", GetProp(linked, "DisplayName"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(character);
            UnityEngine.Object.DestroyImmediate(weapon);
        }
    }
}
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "WeaponGroupTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "error CS|Exiting with code"
```

기대: `WeaponAttackPattern 타입을 찾을 수 없다`로 3개 모두 FAIL.

- [ ] **Step 3: WeaponGroupSO 작성**

`Assets/_Game/Scripts/Models/WeaponGroupSO.cs`:

```csharp
using UnityEngine;

public enum WeaponAttackPattern
{
    Single,
    Spread,
    Piercing,
    Beam,
    Explosive,
    Chain,
}

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

    public string WeaponGroupID => m_weaponGroupID;
    public string DisplayName => m_displayName;
    public WeaponAttackPattern Pattern => m_pattern;

    public GameObject ProjectilePrefab => m_projectilePrefab;
    public float FireRate => m_fireRate;
    public float ProjectileSpeed => m_projectileSpeed;
    public float ProjectileScale => m_projectileScale;

    public int BulletCount => m_bulletCount;
    public float SpreadAngle => m_spreadAngle;

    public int PierceCount => m_pierceCount;
    public float PierceDamageFalloff => m_pierceDamageFalloff;

    public float ExplosionRadius => m_explosionRadius;

    public int ChainCount => m_chainCount;
    public float ChainRadius => m_chainRadius;
    public float ChainDamageFalloff => m_chainDamageFalloff;

    public float WindupTime => m_windupTime;
    public float MinFireRate => m_minFireRate;

    public float BeamWidth => m_beamWidth;
    public float BeamRange => m_beamRange;
}
```

- [ ] **Step 4: CharacterDataSO에 참조 추가**

`Assets/_Game/Scripts/Models/CharacterDataSO.cs`의 `m_baseStats` 선언 다음 줄에 추가:

```csharp
    [SerializeField] private WeaponGroupSO m_weaponGroup;
```

`BaseStats` 프로퍼티 다음 줄에 추가:

```csharp
    public WeaponGroupSO WeaponGroup => m_weaponGroup;
```

- [ ] **Step 5: 테스트 통과 확인**

Step 2와 같은 명령. 기대: 3개 모두 PASS.

- [ ] **Step 6: 커밋**

```bash
git add Assets/_Game/Scripts/Models/WeaponGroupSO.cs \
        Assets/_Game/Scripts/Models/WeaponGroupSO.cs.meta \
        Assets/_Game/Scripts/Models/CharacterDataSO.cs \
        Assets/_Game/Tests/PlayMode/WeaponGroupTests.cs \
        Assets/_Game/Tests/PlayMode/WeaponGroupTests.cs.meta
git commit -m "WeaponGroupSO 추가, CharacterDataSO가 무기군을 참조"
```

---

### Task 3: 발사 패턴 골격과 단발·산탄

`Player/Swap/`의 전략 구조를 그대로 따른다. 기존 두 패턴을 먼저 옮겨 회귀를 잡는다.

**Files:**
- Create: `Assets/_Game/Scripts/Player/Attack/IAttackPattern.cs`
- Create: `Assets/_Game/Scripts/Player/Attack/AttackContextDTO.cs`
- Create: `Assets/_Game/Scripts/Player/Attack/ProjectileLauncher.cs`
- Create: `Assets/_Game/Scripts/Player/Attack/SingleAttackPattern.cs`
- Create: `Assets/_Game/Scripts/Player/Attack/SpreadAttackPattern.cs`
- Create: `Assets/_Game/Tests/PlayMode/AttackPatternTests.cs`

**Interfaces:**
- Consumes: Task 1의 `BulletProjectile.PierceCount` / `PierceDamageFalloff` / `OnHitTarget`, Task 2의 `WeaponGroupSO`
- Produces:
  - `IAttackPattern.Fire(AttackContextDTO context)`
  - `AttackContextDTO` 공개 필드: `Owner`(PlayerCharacterController), `Weapon`(WeaponGroupSO), `FirePoints`(Transform[]), `Origin`(Vector3), `BaseAngle`(float), `Damage`(int), `DamageMultiplier`(float), `Pool`(ObjectPoolManager)
  - `ProjectileLauncher.Launch(AttackContextDTO context, Vector3 position, float angle)` — 발사체 1발 생성. Task 4가 재사용한다.
  - `SingleAttackPattern`, `SpreadAttackPattern` — 기본 생성자

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/_Game/Tests/PlayMode/AttackPatternTests.cs`:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AttackPatternTests
{
    private const BindingFlags ANY_INSTANCE =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private GameObject m_stage;
    private GameObject m_bulletPrefab;
    private readonly List<UnityEngine.Object> m_created = new List<UnityEngine.Object>();

    private static Type T(string name)
    {
        Type type = TestReflectionHelper.GetGameType(name);
        Assert.IsNotNull(type, $"{name} 타입을 찾을 수 없다");
        return type;
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, ANY_INSTANCE);
        Assert.IsNotNull(field, $"필드 {name}을 찾을 수 없다");
        field.SetValue(target, value);
    }

    private static object GetField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, ANY_INSTANCE);
        Assert.IsNotNull(field, $"필드 {name}을 찾을 수 없다");
        return field.GetValue(target);
    }

    /// <summary>씬에 살아 있는 BulletProjectile 수를 센다.</summary>
    private static int LiveBulletCount()
    {
        return UnityEngine.Object.FindObjectsByType(
            T("BulletProjectile"), FindObjectsSortMode.None).Length;
    }

    private object MakeWeapon(string pattern, int bulletCount, float spreadAngle)
    {
        var weapon = ScriptableObject.CreateInstance(T("WeaponGroupSO"));
        m_created.Add(weapon);

        SetField(weapon, "m_pattern", Enum.Parse(T("WeaponAttackPattern"), pattern));
        SetField(weapon, "m_projectilePrefab", m_bulletPrefab);
        SetField(weapon, "m_bulletCount", bulletCount);
        SetField(weapon, "m_spreadAngle", spreadAngle);
        SetField(weapon, "m_projectileSpeed", 10f);
        SetField(weapon, "m_projectileScale", 1f);

        return weapon;
    }

    private object MakeContext(object weapon)
    {
        object context = Activator.CreateInstance(T("AttackContextDTO"));

        var firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(m_stage.transform);
        firePoint.transform.position = Vector3.zero;

        Type transformArray = typeof(Transform[]);
        var points = (Transform[])Activator.CreateInstance(transformArray, 1);
        points[0] = firePoint.transform;

        SetField(context, "Weapon", weapon);
        SetField(context, "FirePoints", points);
        SetField(context, "Origin", Vector3.zero);
        SetField(context, "BaseAngle", 0f);
        SetField(context, "Damage", 100);
        SetField(context, "DamageMultiplier", 1f);

        return context;
    }

    private static void Fire(string patternTypeName, object context)
    {
        object pattern = Activator.CreateInstance(T(patternTypeName));
        MethodInfo fire = pattern.GetType().GetMethod("Fire", ANY_INSTANCE);
        Assert.IsNotNull(fire, $"{patternTypeName}.Fire를 찾을 수 없다");
        fire.Invoke(pattern, new[] { context });
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        m_stage = new GameObject("AttackTestStage");

        m_bulletPrefab = new GameObject("BulletPrefab");
        m_bulletPrefab.SetActive(false);
        var box = m_bulletPrefab.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        m_bulletPrefab.AddComponent(T("BulletProjectile"));

        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < m_created.Count; i++)
        {
            UnityEngine.Object.DestroyImmediate(m_created[i]);
        }
        m_created.Clear();

        if (m_bulletPrefab != null)
        {
            UnityEngine.Object.DestroyImmediate(m_bulletPrefab);
        }

        if (m_stage != null)
        {
            UnityEngine.Object.DestroyImmediate(m_stage);
        }

        Type bullet = TestReflectionHelper.GetGameType("BulletProjectile");
        UnityEngine.Object[] leftovers =
            UnityEngine.Object.FindObjectsByType(bullet, FindObjectsSortMode.None);
        for (int i = 0; i < leftovers.Length; i++)
        {
            UnityEngine.Object.DestroyImmediate(((Component)leftovers[i]).gameObject);
        }
    }

    [UnityTest]
    public IEnumerator 단발은_1발을_쏜다()
    {
        object weapon = MakeWeapon("Single", 1, 0f);
        Fire("SingleAttackPattern", MakeContext(weapon));
        yield return null;

        Assert.AreEqual(1, LiveBulletCount());
    }

    [UnityTest]
    public IEnumerator 산탄은_탄환수만큼_쏜다()
    {
        object weapon = MakeWeapon("Spread", 5, 60f);
        Fire("SpreadAttackPattern", MakeContext(weapon));
        yield return null;

        Assert.AreEqual(5, LiveBulletCount());
    }

    [UnityTest]
    public IEnumerator 산탄은_부채꼴로_퍼진다()
    {
        object weapon = MakeWeapon("Spread", 3, 60f);
        Fire("SpreadAttackPattern", MakeContext(weapon));
        yield return null;

        var angles = new List<float>();
        UnityEngine.Object[] bullets =
            UnityEngine.Object.FindObjectsByType(T("BulletProjectile"), FindObjectsSortMode.None);

        for (int i = 0; i < bullets.Length; i++)
        {
            angles.Add(((Component)bullets[i]).transform.eulerAngles.z);
        }

        angles.Sort();
        Assert.AreEqual(3, angles.Count);
        Assert.Greater(Mathf.DeltaAngle(angles[0], angles[2]), 1f, "세 발의 각도가 모두 같다");
    }

    [UnityTest]
    public IEnumerator 발사체에_피해와_소유자가_실린다()
    {
        object weapon = MakeWeapon("Single", 1, 0f);
        object context = MakeContext(weapon);
        SetField(context, "DamageMultiplier", 0.5f);

        Fire("SingleAttackPattern", context);
        yield return null;

        UnityEngine.Object[] bullets =
            UnityEngine.Object.FindObjectsByType(T("BulletProjectile"), FindObjectsSortMode.None);
        Assert.AreEqual(1, bullets.Length);

        PropertyInfo damage = bullets[0].GetType().GetProperty("Damage", ANY_INSTANCE);
        Assert.AreEqual(50, damage.GetValue(bullets[0]), "배율 0.5가 적용되지 않았다");
    }
}
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "AttackPatternTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "error CS|Exiting with code"
```

기대: `AttackContextDTO 타입을 찾을 수 없다`로 4개 모두 FAIL.

- [ ] **Step 3: 컨텍스트와 인터페이스 작성**

`Assets/_Game/Scripts/Player/Attack/AttackContextDTO.cs`:

```csharp
using UnityEngine;

public class AttackContextDTO
{
    public PlayerCharacterController Owner;
    public WeaponGroupSO Weapon;
    public Transform[] FirePoints;
    public Vector3 Origin;
    public float BaseAngle;
    public int Damage;
    public float DamageMultiplier = 1f;
    public ObjectPoolManager Pool;

    public bool IsValid => Weapon != null;

    public int ScaledDamage => Mathf.RoundToInt(Damage * DamageMultiplier);
}
```

`Assets/_Game/Scripts/Player/Attack/IAttackPattern.cs`:

```csharp
public interface IAttackPattern
{
    void Fire(AttackContextDTO context);
}
```

- [ ] **Step 4: ProjectileLauncher 작성**

`Assets/_Game/Scripts/Player/Attack/ProjectileLauncher.cs`:

```csharp
using UnityEngine;

/// <summary>
/// 발사체 1발을 만들어 무기군 값을 실어 보낸다.
/// 여러 패턴이 공유하므로 생성 규칙을 한 곳에 둔다.
/// </summary>
public static class ProjectileLauncher
{
    public static GameObject Launch(AttackContextDTO context, Vector3 position, float angle)
    {
        if (context == null || context.Weapon == null || context.Weapon.ProjectilePrefab == null)
        {
            return null;
        }

        var rotation = Quaternion.Euler(0f, 0f, angle);
        GameObject projectile;

        if (context.Pool != null)
        {
            projectile = context.Pool.GetFromPool(context.Weapon.ProjectilePrefab, position, rotation);
        }
        else
        {
            projectile = Object.Instantiate(context.Weapon.ProjectilePrefab, position, rotation);
            projectile.SetActive(true);
        }

        if (projectile == null)
        {
            return null;
        }

        projectile.transform.localScale = Vector3.one * context.Weapon.ProjectileScale;

        if (projectile.TryGetComponent<BulletProjectile>(out var bullet))
        {
            bullet.SetSpeed(context.Weapon.ProjectileSpeed);
            bullet.Damage = context.ScaledDamage;
            bullet.OwnerID = context.Owner != null ? context.Owner.CharacterID : string.Empty;
            bullet.PierceCount = context.Weapon.PierceCount;
            bullet.PierceDamageFalloff = context.Weapon.PierceDamageFalloff;
        }

        return projectile;
    }
}
```

- [ ] **Step 5: 단발과 산탄 작성**

`Assets/_Game/Scripts/Player/Attack/SingleAttackPattern.cs`:

```csharp
using UnityEngine;

public class SingleAttackPattern : IAttackPattern
{
    public void Fire(AttackContextDTO context)
    {
        if (context == null || !context.IsValid)
        {
            return;
        }

        Vector3 position = FirePointOrOrigin(context, 0);
        ProjectileLauncher.Launch(context, position, context.BaseAngle);
    }

    internal static Vector3 FirePointOrOrigin(AttackContextDTO context, int index)
    {
        if (context.FirePoints == null || context.FirePoints.Length == 0)
        {
            return context.Origin;
        }

        Transform point = context.FirePoints[index % context.FirePoints.Length];
        return point != null ? point.position : context.Origin;
    }
}
```

`Assets/_Game/Scripts/Player/Attack/SpreadAttackPattern.cs`:

```csharp
using UnityEngine;

public class SpreadAttackPattern : IAttackPattern
{
    public void Fire(AttackContextDTO context)
    {
        if (context == null || !context.IsValid)
        {
            return;
        }

        int count = Mathf.Max(1, context.Weapon.BulletCount);
        float spread = context.Weapon.SpreadAngle;

        for (int i = 0; i < count; i++)
        {
            float offset = count > 1
                ? -spread / 2f + (spread / (count - 1)) * i
                : 0f;

            Vector3 position = SingleAttackPattern.FirePointOrOrigin(context, i);
            ProjectileLauncher.Launch(context, position, context.BaseAngle + offset);
        }
    }
}
```

- [ ] **Step 6: 테스트 통과 확인**

Step 2와 같은 명령. 기대: 4개 모두 PASS.

- [ ] **Step 7: 커밋**

```bash
git add Assets/_Game/Scripts/Player/Attack \
        Assets/_Game/Tests/PlayMode/AttackPatternTests.cs \
        Assets/_Game/Tests/PlayMode/AttackPatternTests.cs.meta
git commit -m "공격 패턴 전략 구조와 단발/산탄 구현"
```

---

### Task 4: 관통 패턴

저격총과 검이 쓴다. 발사 자체는 단발과 같고 관통 값이 발사체에 실린다.

**Files:**
- Create: `Assets/_Game/Scripts/Player/Attack/PiercingAttackPattern.cs`
- Modify: `Assets/_Game/Tests/PlayMode/AttackPatternTests.cs`

**Interfaces:**
- Consumes: Task 3의 `IAttackPattern`, `AttackContextDTO`, `ProjectileLauncher.Launch`
- Produces: `PiercingAttackPattern` — 기본 생성자

- [ ] **Step 1: 실패하는 테스트 추가**

`AttackPatternTests.cs`의 마지막 `}` 바로 앞에 붙인다:

```csharp
    private object MakePiercingWeapon(int pierceCount, float falloff, float scale)
    {
        var weapon = ScriptableObject.CreateInstance(T("WeaponGroupSO"));
        m_created.Add(weapon);

        SetField(weapon, "m_pattern", Enum.Parse(T("WeaponAttackPattern"), "Piercing"));
        SetField(weapon, "m_projectilePrefab", m_bulletPrefab);
        SetField(weapon, "m_bulletCount", 1);
        SetField(weapon, "m_projectileSpeed", 10f);
        SetField(weapon, "m_projectileScale", scale);
        SetField(weapon, "m_pierceCount", pierceCount);
        SetField(weapon, "m_pierceDamageFalloff", falloff);

        return weapon;
    }

    [UnityTest]
    public IEnumerator 관통은_무기군_값을_발사체에_싣는다()
    {
        object weapon = MakePiercingWeapon(3, 0.2f, 1f);
        Fire("PiercingAttackPattern", MakeContext(weapon));
        yield return null;

        UnityEngine.Object[] bullets =
            UnityEngine.Object.FindObjectsByType(T("BulletProjectile"), FindObjectsSortMode.None);
        Assert.AreEqual(1, bullets.Length);

        PropertyInfo pierce = bullets[0].GetType().GetProperty("PierceCount", ANY_INSTANCE);
        PropertyInfo falloff = bullets[0].GetType().GetProperty("PierceDamageFalloff", ANY_INSTANCE);

        Assert.AreEqual(3, pierce.GetValue(bullets[0]));
        Assert.AreEqual(0.2f, (float)falloff.GetValue(bullets[0]), 0.001f);
    }

    [UnityTest]
    public IEnumerator 검기는_발사체_크기가_커진다()
    {
        object weapon = MakePiercingWeapon(-1, 0f, 3f);
        Fire("PiercingAttackPattern", MakeContext(weapon));
        yield return null;

        UnityEngine.Object[] bullets =
            UnityEngine.Object.FindObjectsByType(T("BulletProjectile"), FindObjectsSortMode.None);
        Assert.AreEqual(1, bullets.Length);
        Assert.AreEqual(3f, ((Component)bullets[0]).transform.localScale.x, 0.001f);
    }
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "AttackPatternTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "error CS|Exiting with code"
```

기대: `PiercingAttackPattern 타입을 찾을 수 없다`로 새 2개가 FAIL.

- [ ] **Step 3: 구현**

`Assets/_Game/Scripts/Player/Attack/PiercingAttackPattern.cs`:

```csharp
using UnityEngine;

/// <summary>
/// 저격총과 검이 쓴다. 발사 형태는 단발과 같고, 관통 횟수와 감쇠는
/// ProjectileLauncher가 무기군에서 읽어 발사체에 싣는다.
/// 검기는 ProjectileScale과 ProjectileSpeed로 표현한다.
/// </summary>
public class PiercingAttackPattern : IAttackPattern
{
    public void Fire(AttackContextDTO context)
    {
        if (context == null || !context.IsValid)
        {
            return;
        }

        Vector3 position = SingleAttackPattern.FirePointOrOrigin(context, 0);
        ProjectileLauncher.Launch(context, position, context.BaseAngle);
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Step 2와 같은 명령. 기대: 6개 모두 PASS.

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Game/Scripts/Player/Attack/PiercingAttackPattern.cs \
        Assets/_Game/Scripts/Player/Attack/PiercingAttackPattern.cs.meta \
        Assets/_Game/Tests/PlayMode/AttackPatternTests.cs
git commit -m "관통 공격 패턴 구현"
```

---

### Task 5: 범위 피해 도우미와 폭발·연쇄

폭발과 연쇄는 둘 다 "주변 적을 찾아 피해를 준다"라 도우미를 공유한다.

**Files:**
- Create: `Assets/_Game/Scripts/Player/Attack/AreaDamage.cs`
- Create: `Assets/_Game/Scripts/Player/Attack/ExplosiveAttackPattern.cs`
- Create: `Assets/_Game/Scripts/Player/Attack/ChainAttackPattern.cs`
- Modify: `Assets/_Game/Scripts/Player/BulletProjectile.cs`
- Create: `Assets/_Game/Tests/PlayMode/AreaDamageTests.cs`

**Interfaces:**
- Consumes: Task 1의 `BulletProjectile`, Task 3의 `AttackContextDTO`
- Produces:
  - `AreaDamage.ApplyInRadius(Vector3 center, float radius, int damage, string ownerID)` → `int` (피격 수)
  - `AreaDamage.ApplyChain(Vector3 origin, float radius, int hops, int damage, float falloff, string ownerID)` → `int` (피격 수)
  - `AreaDamage.ApplyInBeam(Vector3 origin, float angle, float width, float range, int damage, string ownerID)` → `int` (피격 수). Task 6이 쓴다.
  - `BulletProjectile.ExplosionRadius` (`float`, get/set), `BulletProjectile.ChainCount` (`int`, get/set), `BulletProjectile.ChainRadius` (`float`, get/set), `BulletProjectile.ChainDamageFalloff` (`float`, get/set)

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/_Game/Tests/PlayMode/AreaDamageTests.cs`:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AreaDamageTests
{
    private GameObject m_stage;

    private static Type T(string name)
    {
        Type type = TestReflectionHelper.GetGameType(name);
        Assert.IsNotNull(type, $"{name} 타입을 찾을 수 없다");
        return type;
    }

    private static object Call(string method, params object[] args)
    {
        MethodInfo info = T("AreaDamage").GetMethod(method,
            BindingFlags.Static | BindingFlags.Public);
        Assert.IsNotNull(info, $"AreaDamage.{method}를 찾을 수 없다");
        return info.Invoke(null, args);
    }

    private GameObject MakeTarget(Vector3 position)
    {
        var go = new GameObject("Target");
        go.tag = "Enemy";
        go.transform.SetParent(m_stage.transform);
        go.transform.position = position;

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.4f, 0.4f);

        return go;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        m_stage = new GameObject("AreaTestStage");
        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        if (m_stage != null)
        {
            UnityEngine.Object.DestroyImmediate(m_stage);
        }
    }

    [UnityTest]
    public IEnumerator 폭발은_반경_안의_적만_맞춘다()
    {
        MakeTarget(new Vector3(0.5f, 0f, 0f));
        MakeTarget(new Vector3(1.0f, 0f, 0f));
        MakeTarget(new Vector3(9.0f, 0f, 0f));
        yield return new WaitForFixedUpdate();

        int hits = (int)Call("ApplyInRadius", Vector3.zero, 3f, 100, "tester");

        Assert.AreEqual(2, hits, "반경 3 안의 2기만 맞아야 한다");
    }

    [UnityTest]
    public IEnumerator 연쇄는_홉_수만큼_전파된다()
    {
        MakeTarget(new Vector3(0f, 0f, 0f));
        MakeTarget(new Vector3(1f, 0f, 0f));
        MakeTarget(new Vector3(2f, 0f, 0f));
        MakeTarget(new Vector3(3f, 0f, 0f));
        MakeTarget(new Vector3(30f, 0f, 0f));
        yield return new WaitForFixedUpdate();

        int hits = (int)Call("ApplyChain", Vector3.zero, 1.5f, 3, 100, 0.3f, "tester");

        Assert.AreEqual(4, hits, "최초 1기 + 3홉 = 4기여야 한다");
    }

    [UnityTest]
    public IEnumerator 연쇄는_같은_적을_다시_때리지_않는다()
    {
        MakeTarget(new Vector3(0f, 0f, 0f));
        MakeTarget(new Vector3(1f, 0f, 0f));
        yield return new WaitForFixedUpdate();

        int hits = (int)Call("ApplyChain", Vector3.zero, 1.5f, 5, 100, 0f, "tester");

        Assert.AreEqual(2, hits, "적이 2기뿐이면 2회를 넘을 수 없다");
    }

    [UnityTest]
    public IEnumerator 빔은_직선상의_적만_맞춘다()
    {
        MakeTarget(new Vector3(0f, 2f, 0f));
        MakeTarget(new Vector3(0f, 5f, 0f));
        MakeTarget(new Vector3(8f, 2f, 0f));
        yield return new WaitForFixedUpdate();

        int hits = (int)Call("ApplyInBeam", Vector3.zero, 0f, 1.5f, 10f, 100, "tester");

        Assert.AreEqual(2, hits, "폭 1.5 안의 2기만 맞아야 한다");
    }
}
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "AreaDamageTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "error CS|Exiting with code"
```

기대: `AreaDamage 타입을 찾을 수 없다`로 4개 모두 FAIL.

- [ ] **Step 3: AreaDamage 작성**

`Assets/_Game/Scripts/Player/Attack/AreaDamage.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 범위 피해를 한 곳에 모은다. 폭발/연쇄/빔이 공유한다.
/// SkillLaser가 쓰던 "범위 안 적을 찾아 TakeDamage" 방식과 같다.
/// </summary>
public static class AreaDamage
{
    private static readonly List<Collider2D> s_buffer = new List<Collider2D>();

    public static int ApplyInRadius(Vector3 center, float radius, int damage, string ownerID)
    {
        if (radius <= 0f)
        {
            return 0;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        return ApplyToAll(hits, damage, ownerID, null);
    }

    public static int ApplyInBeam(Vector3 origin, float angle, float width, float range, int damage, string ownerID)
    {
        if (width <= 0f || range <= 0f)
        {
            return 0;
        }

        // 발사 방향은 로컬 up이므로 각도에 90도를 더해 중심을 잡는다
        float radians = (angle + 90f) * Mathf.Deg2Rad;
        var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        Vector2 center = (Vector2)origin + direction * (range * 0.5f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(width, range), angle);
        return ApplyToAll(hits, damage, ownerID, null);
    }

    public static int ApplyChain(Vector3 origin, float radius, int hops, int damage, float falloff, string ownerID)
    {
        if (radius <= 0f)
        {
            return 0;
        }

        var visited = new HashSet<Component>();
        Vector3 current = origin;
        int applied = 0;
        int budget = Mathf.Max(0, hops) + 1;

        for (int i = 0; i < budget; i++)
        {
            Component target = FindNearestUnvisited(current, radius, visited);

            if (target == null)
            {
                break;
            }

            visited.Add(target);

            float scale = falloff > 0f ? Mathf.Pow(1f - falloff, i) : 1f;
            ApplyToOne(target, Mathf.RoundToInt(damage * scale), ownerID);

            current = target.transform.position;
            applied++;
        }

        return applied;
    }

    private static Component FindNearestUnvisited(Vector3 center, float radius, HashSet<Component> visited)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        Component nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Component target = ResolveTarget(hits[i]);

            if (target == null || visited.Contains(target))
            {
                continue;
            }

            float distance = Vector3.Distance(center, target.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = target;
            }
        }

        return nearest;
    }

    private static int ApplyToAll(Collider2D[] hits, int damage, string ownerID, HashSet<Component> visited)
    {
        var seen = visited ?? new HashSet<Component>();
        int applied = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            Component target = ResolveTarget(hits[i]);

            if (target == null || !seen.Add(target))
            {
                continue;
            }

            ApplyToOne(target, damage, ownerID);
            applied++;
        }

        return applied;
    }

    private static Component ResolveTarget(Collider2D collider)
    {
        if (collider == null)
        {
            return null;
        }

        if (!collider.CompareTag("Enemy") && !collider.CompareTag("Boss"))
        {
            return null;
        }

        if (collider.TryGetComponent<EnemyController>(out var enemy))
        {
            return enemy;
        }

        if (collider.TryGetComponent<BossController>(out var boss))
        {
            return boss;
        }

        // 컨트롤러가 없는 표적(테스트용 더미)도 피격 수에는 센다
        return collider;
    }

    private static void ApplyToOne(Component target, int damage, string ownerID)
    {
        if (target is EnemyController enemy)
        {
            enemy.TakeDamage(damage, ownerID);
            return;
        }

        if (target is BossController boss)
        {
            boss.TakeDamage(damage, ownerID);
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Step 2와 같은 명령. 기대: 4개 모두 PASS.

- [ ] **Step 5: BulletProjectile에 폭발·연쇄 값 추가**

`BulletProjectile.cs`의 `PierceDamageFalloff` 프로퍼티 다음에 추가:

```csharp
    /// <summary>0보다 크면 명중 지점에 범위 피해를 준다.</summary>
    public float ExplosionRadius { get; set; }

    /// <summary>0보다 크면 명중 후 인접 적으로 전파한다.</summary>
    public int ChainCount { get; set; }
    public float ChainRadius { get; set; }
    public float ChainDamageFalloff { get; set; }
```

`OnTriggerEnter2D`의 `OnHitTarget?.Invoke(OwnerID, applied);` 바로 다음에 추가:

```csharp
        if (ExplosionRadius > 0f)
        {
            AreaDamage.ApplyInRadius(transform.position, ExplosionRadius, applied, OwnerID);
        }

        if (ChainCount > 0 && ChainRadius > 0f)
        {
            AreaDamage.ApplyChain(transform.position, ChainRadius, ChainCount, applied,
                ChainDamageFalloff, OwnerID);
        }
```

`ProjectileLauncher.Launch`의 `bullet.PierceDamageFalloff = ...` 다음에 추가:

```csharp
            bullet.ExplosionRadius = context.Weapon.ExplosionRadius;
            bullet.ChainCount = context.Weapon.ChainCount;
            bullet.ChainRadius = context.Weapon.ChainRadius;
            bullet.ChainDamageFalloff = context.Weapon.ChainDamageFalloff;
```

- [ ] **Step 6: 폭발·연쇄 패턴 작성**

`Assets/_Game/Scripts/Player/Attack/ExplosiveAttackPattern.cs`:

```csharp
using UnityEngine;

/// <summary>
/// 유탄 발사기. 발사 형태는 단발이고, 폭발 반경은 발사체가 명중 시 처리한다.
/// </summary>
public class ExplosiveAttackPattern : IAttackPattern
{
    public void Fire(AttackContextDTO context)
    {
        if (context == null || !context.IsValid)
        {
            return;
        }

        Vector3 position = SingleAttackPattern.FirePointOrOrigin(context, 0);
        ProjectileLauncher.Launch(context, position, context.BaseAngle);
    }
}
```

`Assets/_Game/Scripts/Player/Attack/ChainAttackPattern.cs`:

```csharp
using UnityEngine;

/// <summary>
/// 지팡이. 마법탄이 명중하면 발사체가 인접 적으로 연쇄를 전파한다.
/// </summary>
public class ChainAttackPattern : IAttackPattern
{
    public void Fire(AttackContextDTO context)
    {
        if (context == null || !context.IsValid)
        {
            return;
        }

        Vector3 position = SingleAttackPattern.FirePointOrOrigin(context, 0);
        ProjectileLauncher.Launch(context, position, context.BaseAngle);
    }
}
```

- [ ] **Step 7: 컴파일 확인**

```bash
"$UNITY" -batchmode -quit -projectPath "$PROJ" -logFile - 2>&1 | grep -E "error CS"
```

기대: 출력 없음.

- [ ] **Step 8: 커밋**

```bash
git add Assets/_Game/Scripts/Player/Attack \
        Assets/_Game/Scripts/Player/BulletProjectile.cs \
        Assets/_Game/Tests/PlayMode/AreaDamageTests.cs \
        Assets/_Game/Tests/PlayMode/AreaDamageTests.cs.meta
git commit -m "범위 피해 도우미와 폭발/연쇄 공격 패턴 구현"
```

---

### Task 6: 빔 패턴

레이저. 발사체가 아니라 히트스캔이다.

**Files:**
- Create: `Assets/_Game/Scripts/Player/Attack/BeamAttackPattern.cs`
- Modify: `Assets/_Game/Tests/PlayMode/AttackPatternTests.cs`

**Interfaces:**
- Consumes: Task 3의 `IAttackPattern`, `AttackContextDTO`, Task 5의 `AreaDamage.ApplyInBeam`
- Produces: `BeamAttackPattern` — 기본 생성자

- [ ] **Step 1: 실패하는 테스트 추가**

`AttackPatternTests.cs`의 마지막 `}` 바로 앞에 붙인다:

```csharp
    private GameObject MakeBeamTarget(Vector3 position)
    {
        var go = new GameObject("BeamTarget");
        go.tag = "Enemy";
        go.transform.SetParent(m_stage.transform);
        go.transform.position = position;

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.4f, 0.4f);

        return go;
    }

    [UnityTest]
    public IEnumerator 빔은_발사체를_만들지_않는다()
    {
        var weapon = ScriptableObject.CreateInstance(T("WeaponGroupSO"));
        m_created.Add(weapon);
        SetField(weapon, "m_pattern", Enum.Parse(T("WeaponAttackPattern"), "Beam"));
        SetField(weapon, "m_projectilePrefab", m_bulletPrefab);
        SetField(weapon, "m_beamWidth", 1.5f);
        SetField(weapon, "m_beamRange", 10f);

        MakeBeamTarget(new Vector3(0f, 3f, 0f));
        yield return new WaitForFixedUpdate();

        Fire("BeamAttackPattern", MakeContext(weapon));
        yield return null;

        Assert.AreEqual(0, LiveBulletCount(), "빔은 히트스캔이라 발사체가 없어야 한다");
    }

    [UnityTest]
    public IEnumerator 빔_폭이_0이면_아무것도_하지_않는다()
    {
        var weapon = ScriptableObject.CreateInstance(T("WeaponGroupSO"));
        m_created.Add(weapon);
        SetField(weapon, "m_pattern", Enum.Parse(T("WeaponAttackPattern"), "Beam"));
        SetField(weapon, "m_beamWidth", 0f);
        SetField(weapon, "m_beamRange", 0f);

        MakeBeamTarget(new Vector3(0f, 3f, 0f));
        yield return new WaitForFixedUpdate();

        Assert.DoesNotThrow(() => Fire("BeamAttackPattern", MakeContext(weapon)));
        yield return null;
    }
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "AttackPatternTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "error CS|Exiting with code"
```

기대: `BeamAttackPattern 타입을 찾을 수 없다`로 새 2개가 FAIL.

- [ ] **Step 3: 구현**

`Assets/_Game/Scripts/Player/Attack/BeamAttackPattern.cs`:

```csharp
using UnityEngine;

/// <summary>
/// 레이저. 발사체를 만들지 않고 직선 범위에 즉시 피해를 준다.
/// SkillLaser가 쓰던 방식과 같되 무기군 값을 따른다.
/// </summary>
public class BeamAttackPattern : IAttackPattern
{
    public void Fire(AttackContextDTO context)
    {
        if (context == null || !context.IsValid)
        {
            return;
        }

        Vector3 origin = SingleAttackPattern.FirePointOrOrigin(context, 0);
        string ownerID = context.Owner != null ? context.Owner.CharacterID : string.Empty;

        AreaDamage.ApplyInBeam(
            origin,
            context.BaseAngle,
            context.Weapon.BeamWidth,
            context.Weapon.BeamRange,
            context.ScaledDamage,
            ownerID);
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Step 2와 같은 명령. 기대: 8개 모두 PASS.

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Game/Scripts/Player/Attack/BeamAttackPattern.cs \
        Assets/_Game/Scripts/Player/Attack/BeamAttackPattern.cs.meta \
        Assets/_Game/Tests/PlayMode/AttackPatternTests.cs
git commit -m "빔 공격 패턴 구현"
```

---

### Task 7: PlayerAttackComponent를 무기군에 연결

조준·발사 주기·컨텍스트 구성만 남기고 발사를 패턴에 위임한다. 기관총 예열도 여기에 넣는다.

**Files:**
- Modify: `Assets/_Game/Scripts/Player/PlayerAttackComponent.cs`
- Modify: `Assets/_Game/Scripts/UI/BattleSceneInitializer.cs`
- Create: `Assets/_Game/Tests/PlayMode/AttackComponentWeaponTests.cs`

**Interfaces:**
- Consumes: Task 2의 `WeaponGroupSO`, `CharacterDataSO.WeaponGroup`, Task 3~6의 패턴 6종
- Produces:
  - `PlayerAttackComponent.SetWeapon(WeaponGroupSO weapon)`
  - `PlayerAttackComponent.CurrentWeapon` (`WeaponGroupSO`, 읽기 전용)
  - `PlayerAttackComponent.CurrentFireRate` (`float`, 읽기 전용) — 예열이 반영된 값

- [ ] **Step 1: 실패하는 테스트 작성**

`Assets/_Game/Tests/PlayMode/AttackComponentWeaponTests.cs`:

```csharp
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AttackComponentWeaponTests
{
    private const BindingFlags ANY_INSTANCE =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private GameObject m_stage;

    private static Type T(string name)
    {
        Type type = TestReflectionHelper.GetGameType(name);
        Assert.IsNotNull(type, $"{name} 타입을 찾을 수 없다");
        return type;
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, ANY_INSTANCE);
        Assert.IsNotNull(field, $"필드 {name}을 찾을 수 없다");
        field.SetValue(target, value);
    }

    private static object GetProp(object target, string name)
    {
        PropertyInfo prop = target.GetType().GetProperty(name, ANY_INSTANCE);
        Assert.IsNotNull(prop, $"프로퍼티 {name}을 찾을 수 없다");
        return prop.GetValue(target);
    }

    private static object Invoke(object target, string name, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(name, ANY_INSTANCE);
        Assert.IsNotNull(method, $"메서드 {name}을 찾을 수 없다");
        return method.Invoke(target, args);
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        m_stage = new GameObject("WeaponTestStage");
        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        if (m_stage != null)
        {
            UnityEngine.Object.DestroyImmediate(m_stage);
        }
    }

    private object MakeAttackComponent()
    {
        var go = new GameObject("Attacker");
        go.transform.SetParent(m_stage.transform);
        return go.AddComponent(T("PlayerAttackComponent"));
    }

    [Test]
    public void 무기군_미주입시_CurrentWeapon은_null이다()
    {
        object attack = MakeAttackComponent();
        Assert.IsNull(GetProp(attack, "CurrentWeapon"));
    }

    [Test]
    public void SetWeapon하면_발사주기가_무기군을_따른다()
    {
        object attack = MakeAttackComponent();

        var weapon = ScriptableObject.CreateInstance(T("WeaponGroupSO"));
        SetField(weapon, "m_fireRate", 0.15f);
        SetField(weapon, "m_pattern", Enum.Parse(T("WeaponAttackPattern"), "Single"));

        try
        {
            Invoke(attack, "SetWeapon", weapon);

            Assert.IsNotNull(GetProp(attack, "CurrentWeapon"));
            Assert.AreEqual(0.15f, (float)GetProp(attack, "CurrentFireRate"), 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(weapon);
        }
    }

    [Test]
    public void 예열이_없으면_발사주기가_그대로다()
    {
        object attack = MakeAttackComponent();

        var weapon = ScriptableObject.CreateInstance(T("WeaponGroupSO"));
        SetField(weapon, "m_fireRate", 0.5f);
        SetField(weapon, "m_windupTime", 0f);
        SetField(weapon, "m_minFireRate", 0f);

        try
        {
            Invoke(attack, "SetWeapon", weapon);
            SetField(attack, "m_windupProgress", 1f);

            Assert.AreEqual(0.5f, (float)GetProp(attack, "CurrentFireRate"), 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(weapon);
        }
    }

    [Test]
    public void 예열이_최대면_최소_발사주기가_된다()
    {
        object attack = MakeAttackComponent();

        var weapon = ScriptableObject.CreateInstance(T("WeaponGroupSO"));
        SetField(weapon, "m_fireRate", 0.2f);
        SetField(weapon, "m_windupTime", 3f);
        SetField(weapon, "m_minFireRate", 0.05f);

        try
        {
            Invoke(attack, "SetWeapon", weapon);

            SetField(attack, "m_windupProgress", 0f);
            Assert.AreEqual(0.2f, (float)GetProp(attack, "CurrentFireRate"), 0.001f);

            SetField(attack, "m_windupProgress", 3f);
            Assert.AreEqual(0.05f, (float)GetProp(attack, "CurrentFireRate"), 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(weapon);
        }
    }
}
```

- [ ] **Step 2: 테스트가 실패하는지 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "AttackComponentWeaponTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "error CS|Exiting with code"
```

기대: `프로퍼티 CurrentWeapon을 찾을 수 없다` 등으로 4개 모두 FAIL.

- [ ] **Step 3: PlayerAttackComponent에 무기군 지원 추가**

`PlayerAttackComponent.cs`의 필드 선언부(`m_targetingRange` 다음)에 추가:

```csharp
    private WeaponGroupSO m_weapon;
    private IAttackPattern m_pattern;
    private float m_windupProgress;

    public WeaponGroupSO CurrentWeapon => m_weapon;

    /// <summary>예열이 반영된 발사 주기. 무기군이 없으면 직렬화된 값을 쓴다.</summary>
    public float CurrentFireRate
    {
        get
        {
            if (m_weapon == null)
            {
                return m_fireRate;
            }

            if (m_weapon.WindupTime <= 0f || m_weapon.MinFireRate <= 0f)
            {
                return m_weapon.FireRate;
            }

            float t = Mathf.Clamp01(m_windupProgress / m_weapon.WindupTime);
            return Mathf.Lerp(m_weapon.FireRate, m_weapon.MinFireRate, t);
        }
    }

    public void SetWeapon(WeaponGroupSO weapon)
    {
        m_weapon = weapon;
        m_windupProgress = 0f;
        m_pattern = CreatePattern(weapon);
    }

    private static IAttackPattern CreatePattern(WeaponGroupSO weapon)
    {
        if (weapon == null)
        {
            return null;
        }

        switch (weapon.Pattern)
        {
            case WeaponAttackPattern.Spread:
                return new SpreadAttackPattern();
            case WeaponAttackPattern.Piercing:
                return new PiercingAttackPattern();
            case WeaponAttackPattern.Beam:
                return new BeamAttackPattern();
            case WeaponAttackPattern.Explosive:
                return new ExplosiveAttackPattern();
            case WeaponAttackPattern.Chain:
                return new ChainAttackPattern();
            default:
                return new SingleAttackPattern();
        }
    }
```

`Update()`에서 `m_fireTimer >= m_fireRate` 비교를 `m_fireTimer >= CurrentFireRate`로 바꾸고, 예열 누적을 넣는다. `Update()` 본문의 발사 판정 부분을 아래로 교체:

```csharp
        if (m_fireTimer >= CurrentFireRate && canFire)
        {
            m_fireTimer = 0f;
            Fire();

            if (m_weapon != null && m_weapon.WindupTime > 0f)
            {
                m_windupProgress = Mathf.Min(m_windupProgress + CurrentFireRate, m_weapon.WindupTime);
            }
        }
        else if (!canFire)
        {
            m_windupProgress = 0f;
        }
```

`Fire()` 메서드 맨 앞(`if (m_bulletPrefab == null)` 검사보다 위)에 무기군 경로를 추가:

```csharp
        if (m_pattern != null && m_weapon != null)
        {
            FireWithWeapon();
            return;
        }
```

`Fire()` 메서드 다음에 새 메서드를 추가:

```csharp
    private void FireWithWeapon()
    {
        float baseAngle;

        if ((!m_owner.IsActive || !m_owner.IsDragging) && CurrentTarget != null)
        {
            Vector3 direction = (CurrentTarget.TargetTransform.position - transform.position).normalized;
            baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        }
        else
        {
            baseAngle = transform.rotation.eulerAngles.z;
        }

        var context = new AttackContextDTO
        {
            Owner = m_owner,
            Weapon = m_weapon,
            FirePoints = m_firePoints,
            Origin = transform.position,
            BaseAngle = baseAngle,
            Damage = m_owner != null && m_owner.Stats != null ? m_owner.Stats.AttackDamage : 0,
            DamageMultiplier = (m_owner != null && m_owner.IsActive) ? 1f : 0.5f,
            Pool = FindAnyObjectByType<ObjectPoolManager>(),
        };

        m_pattern.Fire(context);
    }
```

- [ ] **Step 4: BattleSceneInitializer에서 주입**

`BattleSceneInitializer.cs`에서 `controller.SetIdentity(...)` 호출 다음 줄에 추가:

```csharp
                            if (controller.TryGetComponent<PlayerAttackComponent>(out var attackComponent))
                            {
                                attackComponent.SetWeapon(charData.WeaponGroup);
                            }
```

- [ ] **Step 5: 테스트 통과 확인**

Step 2와 같은 명령. 기대: 4개 모두 PASS.

- [ ] **Step 6: 전체 회귀 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 | grep -E "error CS|Exiting with code"
```

기대: 전부 PASS. 5~8분 걸린다.

- [ ] **Step 7: 커밋**

```bash
git add Assets/_Game/Scripts/Player/PlayerAttackComponent.cs \
        Assets/_Game/Scripts/UI/BattleSceneInitializer.cs \
        Assets/_Game/Tests/PlayMode/AttackComponentWeaponTests.cs \
        Assets/_Game/Tests/PlayMode/AttackComponentWeaponTests.cs.meta
git commit -m "PlayerAttackComponent를 무기군에 연결, 기관총 예열 추가"
```

---

### Task 8: 무기군 에셋 9종 생성과 캐릭터 연결

에디터 스크립트로 자동화한다. `CharacterRosterBuilder`와 같은 방식이다.

**Files:**
- Create: `Assets/Editor/WeaponGroupBuilder.cs`
- Create: `Assets/_Game/Resources/Weapons/*.asset` (스크립트가 생성)
- Modify: `Assets/_Game/Resources/{a..i}_CharacterData.asset` (스크립트가 연결)

**Interfaces:**
- Consumes: Task 2의 `WeaponGroupSO`, `CharacterDataSO.WeaponGroup`
- Produces: 무기군 에셋 9종과 캐릭터 9종의 연결. `WeaponGroupBuilder.Build()` (정적, `-executeMethod`로 호출 가능)

- [ ] **Step 1: 빌더 작성**

`Assets/Editor/WeaponGroupBuilder.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 무기군 카드 9종을 WeaponGroupSO 에셋으로 만들고 같은 이름의 캐릭터에 연결한다.
/// 이미 있는 에셋은 덮어쓰지 않는다. 멱등이다.
/// </summary>
public static class WeaponGroupBuilder
{
    private const string WEAPON_DIR = "Assets/_Game/Resources/Weapons";
    private const string RESOURCE_DIR = "Assets/_Game/Resources";

    private class Spec
    {
        public string ID;
        public string Name;
        public WeaponAttackPattern Pattern;
        public float FireRate;
        public float Speed = 15f;
        public float Scale = 1f;
        public int BulletCount = 1;
        public float SpreadAngle;
        public int PierceCount;
        public float PierceFalloff;
        public float ExplosionRadius;
        public int ChainCount;
        public float ChainRadius;
        public float ChainFalloff;
        public float WindupTime;
        public float MinFireRate;
        public float BeamWidth;
        public float BeamRange;
    }

    private static readonly Spec[] SPECS =
    {
        new Spec { ID = "pistol",  Name = "권총",       Pattern = WeaponAttackPattern.Single,    FireRate = 0.5f },
        new Spec { ID = "rifle",   Name = "소총",       Pattern = WeaponAttackPattern.Single,    FireRate = 0.15f },
        new Spec { ID = "mg",      Name = "기관총",     Pattern = WeaponAttackPattern.Single,    FireRate = 0.2f, WindupTime = 3f, MinFireRate = 0.05f },
        new Spec { ID = "shotgun", Name = "샷건",       Pattern = WeaponAttackPattern.Spread,    FireRate = 0.8f, BulletCount = 5, SpreadAngle = 60f },
        new Spec { ID = "sniper",  Name = "저격총",     Pattern = WeaponAttackPattern.Piercing,  FireRate = 1.2f, PierceCount = 3, PierceFalloff = 0.2f, Speed = 25f },
        new Spec { ID = "sword",   Name = "검",         Pattern = WeaponAttackPattern.Piercing,  FireRate = 1.5f, PierceCount = -1, Scale = 3f, Speed = 6f },
        new Spec { ID = "laser",   Name = "레이저",     Pattern = WeaponAttackPattern.Beam,      FireRate = 0.8f, BeamWidth = 1.5f, BeamRange = 20f },
        new Spec { ID = "grenade", Name = "유탄 발사기", Pattern = WeaponAttackPattern.Explosive, FireRate = 1.0f, ExplosionRadius = 3f, Speed = 8f },
        new Spec { ID = "staff",   Name = "지팡이",     Pattern = WeaponAttackPattern.Chain,     FireRate = 0.7f, ChainCount = 3, ChainRadius = 4f, ChainFalloff = 0.3f },
    };

    [MenuItem("SpaceCaptain/무기군 에셋 생성 및 연결")]
    public static void Build()
    {
        var log = new StringBuilder();
        Directory.CreateDirectory(WEAPON_DIR);

        GameObject bulletPrefab = FindDefaultBulletPrefab();

        if (bulletPrefab == null)
        {
            Debug.LogError("[WeaponGroupBuilder] 기본 탄환 프리팹을 찾을 수 없다: Assets/_Game/Prefabs/Projectiles/Bullet.prefab");
            return;
        }

        var byName = new Dictionary<string, WeaponGroupSO>();

        for (int i = 0; i < SPECS.Length; i++)
        {
            Spec spec = SPECS[i];
            string path = $"{WEAPON_DIR}/{spec.ID}_WeaponGroup.asset";

            var weapon = AssetDatabase.LoadAssetAtPath<WeaponGroupSO>(path);

            if (weapon == null)
            {
                weapon = ScriptableObject.CreateInstance<WeaponGroupSO>();
                Apply(weapon, spec, bulletPrefab);
                AssetDatabase.CreateAsset(weapon, path);
                log.AppendLine($"  생성: {spec.Name} ({spec.Pattern})");
            }
            else
            {
                log.AppendLine($"  유지: {spec.Name} (이미 있음)");
            }

            byName[spec.Name] = weapon;
        }

        int linked = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:CharacterDataSO", new[] { RESOURCE_DIR }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var character = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(path);

            if (character == null || !byName.TryGetValue(character.CharacterName, out var weapon))
            {
                continue;
            }

            var so = new SerializedObject(character);
            SerializedProperty prop = so.FindProperty("m_weaponGroup");

            if (prop.objectReferenceValue != weapon)
            {
                prop.objectReferenceValue = weapon;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(character);
                log.AppendLine($"  연결: {character.CharacterID} ({character.CharacterName}) -> {weapon.name}");
            }

            linked++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.AppendLine($"무기군 {SPECS.Length}종, 연결된 캐릭터 {linked}종");
        Debug.Log("[WeaponGroupBuilder] 완료\n" + log);
    }

    private static GameObject FindDefaultBulletPrefab()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Projectiles/Bullet.prefab");
    }

    private static void Apply(WeaponGroupSO weapon, Spec spec, GameObject bulletPrefab)
    {
        var so = new SerializedObject(weapon);

        so.FindProperty("m_weaponGroupID").stringValue = spec.ID;
        so.FindProperty("m_displayName").stringValue = spec.Name;
        so.FindProperty("m_pattern").enumValueIndex = (int)spec.Pattern;
        so.FindProperty("m_projectilePrefab").objectReferenceValue = bulletPrefab;
        so.FindProperty("m_fireRate").floatValue = spec.FireRate;
        so.FindProperty("m_projectileSpeed").floatValue = spec.Speed;
        so.FindProperty("m_projectileScale").floatValue = spec.Scale;
        so.FindProperty("m_bulletCount").intValue = spec.BulletCount;
        so.FindProperty("m_spreadAngle").floatValue = spec.SpreadAngle;
        so.FindProperty("m_pierceCount").intValue = spec.PierceCount;
        so.FindProperty("m_pierceDamageFalloff").floatValue = spec.PierceFalloff;
        so.FindProperty("m_explosionRadius").floatValue = spec.ExplosionRadius;
        so.FindProperty("m_chainCount").intValue = spec.ChainCount;
        so.FindProperty("m_chainRadius").floatValue = spec.ChainRadius;
        so.FindProperty("m_chainDamageFalloff").floatValue = spec.ChainFalloff;
        so.FindProperty("m_windupTime").floatValue = spec.WindupTime;
        so.FindProperty("m_minFireRate").floatValue = spec.MinFireRate;
        so.FindProperty("m_beamWidth").floatValue = spec.BeamWidth;
        so.FindProperty("m_beamRange").floatValue = spec.BeamRange;

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
```

- [ ] **Step 2: 빌더 실행**

```bash
"$UNITY" -batchmode -quit -projectPath "$PROJ" \
  -executeMethod WeaponGroupBuilder.Build -logFile - 2>&1 \
  | grep -E "WeaponGroupBuilder|생성:|연결:|error CS"
```

기대: 무기군 9종 생성, 캐릭터 9종 연결.

- [ ] **Step 3: 연결 확인**

```bash
ls "$PROJ/Assets/_Game/Resources/Weapons/" | grep -c "_WeaponGroup.asset"
grep -L "m_weaponGroup: {fileID: 11400000" "$PROJ"/Assets/_Game/Resources/[a-i]_CharacterData.asset
```

기대: 첫 명령이 `9`. 두 번째 명령은 출력 없음(전부 연결됨).

- [ ] **Step 4: 전체 회귀 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 | grep -E "error CS|Exiting with code"
```

기대: 전부 PASS.

- [ ] **Step 5: 커밋**

```bash
git add Assets/Editor/WeaponGroupBuilder.cs \
        Assets/Editor/WeaponGroupBuilder.cs.meta \
        Assets/_Game/Resources/Weapons \
        Assets/_Game/Resources
git commit -m "무기군 에셋 9종 생성 및 캐릭터 연결"
```

---

### Task 9: 인게임 통합 검증

편성한 캐릭터가 자기 무기군 패턴대로 쏘는지 실제 전투 씬에서 확인한다.

**Files:**
- Create: `Assets/_Game/Tests/PlayMode/WeaponInBattleTests.cs`

**Interfaces:**
- Consumes: Task 7의 `PlayerAttackComponent.SetWeapon` / `CurrentWeapon`, Task 8의 무기군 에셋
- Produces: 없음 (최종 검증)

- [ ] **Step 1: 테스트 작성**

`Assets/_Game/Tests/PlayMode/WeaponInBattleTests.cs`:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// 편성한 캐릭터가 인게임에서 자기 무기군 패턴을 들고 스폰되는지 확인한다.
/// </summary>
public class WeaponInBattleTests
{
    private const string SAVE_KEY = "SpaceCaptain.Deck";
    private const int MAX_WAIT_FRAMES = 900;

    private const BindingFlags ANY_INSTANCE =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private object m_userData;
    private List<string> m_originalDeck;
    private float m_originalTimeScale;

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

    private static object FindInScene(string typeName)
    {
        Type type = TestReflectionHelper.GetGameType(typeName);
        Assert.IsNotNull(type, $"{typeName} 타입을 찾을 수 없다");
        return UnityEngine.Object.FindAnyObjectByType(type, FindObjectsInactive.Include);
    }

    private static List<string> DeckOf(object userData)
    {
        return (List<string>)GetField(GetProp(userData, "LobbyData"), "DeckCharacters");
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        m_originalTimeScale = Time.timeScale;
        m_userData = Resources.Load("UserData");
        Assert.IsNotNull(m_userData, "Resources/UserData를 찾을 수 없다");
        m_originalDeck = new List<string>(DeckOf(m_userData));
        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = m_originalTimeScale;
        PlayerPrefs.DeleteKey(SAVE_KEY);

        if (m_userData != null && m_originalDeck != null)
        {
            List<string> deck = DeckOf(m_userData);
            deck.Clear();
            deck.AddRange(m_originalDeck);
        }
    }

    private IEnumerator LoadBattle(List<string> deck, Action<IList> onReady)
    {
        PlayerPrefs.SetString(SAVE_KEY, string.Join("\n", deck));
        PlayerPrefs.Save();

        SceneManager.LoadScene("InGame");
        yield return null;

        IList characters = null;

        for (int i = 0; i < MAX_WAIT_FRAMES; i++)
        {
            object swapManager = FindInScene("PlayerSwapManager");

            if (swapManager != null)
            {
                characters = (IList)GetProp(swapManager, "Characters");

                if (characters != null && characters.Count > 0)
                {
                    break;
                }
            }

            yield return null;
        }

        Assert.IsNotNull(characters, "스폰된 캐릭터가 없다");
        Assert.Greater(characters.Count, 0, "스폰된 캐릭터가 없다");
        onReady(characters);
    }

    [UnityTest]
    public IEnumerator 편성한_캐릭터가_자기_무기군을_들고_스폰된다()
    {
        var deck = new List<string> { "a", "e", "g" };
        IList characters = null;

        yield return LoadBattle(deck, c => characters = c);

        object database = Resources.Load("CharacterDatabase");
        MethodInfo getCharacter = database.GetType().GetMethod("GetCharacter", ANY_INSTANCE);

        for (int i = 0; i < characters.Count; i++)
        {
            var component = (Component)characters[i];
            string id = (string)GetProp(characters[i], "CharacterID");

            object attack = component.GetComponent(TestReflectionHelper.GetGameType("PlayerAttackComponent"));
            Assert.IsNotNull(attack, $"{id}에 PlayerAttackComponent가 없다");

            object weapon = GetProp(attack, "CurrentWeapon");
            Assert.IsNotNull(weapon, $"{id}에 무기군이 주입되지 않았다");

            object data = getCharacter.Invoke(database, new object[] { id });
            object expected = GetProp(data, "WeaponGroup");

            Assert.AreEqual(
                GetProp(expected, "DisplayName"),
                GetProp(weapon, "DisplayName"),
                $"{id}의 무기군이 편성 데이터와 다르다");
        }
    }

    [UnityTest]
    public IEnumerator 무기군마다_패턴이_다르다()
    {
        // e=샷건(Spread), g=검(Piercing), i=유탄 발사기(Explosive)
        var deck = new List<string> { "e", "g", "i" };
        IList characters = null;

        yield return LoadBattle(deck, c => characters = c);

        var patterns = new List<string>();

        for (int i = 0; i < characters.Count; i++)
        {
            var component = (Component)characters[i];
            object attack = component.GetComponent(TestReflectionHelper.GetGameType("PlayerAttackComponent"));
            object weapon = GetProp(attack, "CurrentWeapon");
            patterns.Add(GetProp(weapon, "Pattern").ToString());
        }

        CollectionAssert.AreEqual(
            new List<string> { "Spread", "Piercing", "Explosive" },
            patterns,
            "무기군별 패턴이 편성 순서대로 실리지 않았다");
    }
}
```

- [ ] **Step 2: 테스트 실행**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testFilter "WeaponInBattleTests" -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 \
  | grep -E "error CS|Exiting with code"
```

기대: 2개 모두 PASS.

- [ ] **Step 3: 전체 회귀 확인**

```bash
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform PlayMode \
  -testResults "$PROJ/Logs/test-results.xml" -logFile - 2>&1 | grep -E "error CS|Exiting with code"

"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform EditMode \
  -testResults "$PROJ/Logs/edit-results.xml" -logFile - 2>&1 | grep -E "error CS|Exiting with code"
```

기대: PlayMode·EditMode 전부 PASS.

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Game/Tests/PlayMode/WeaponInBattleTests.cs \
        Assets/_Game/Tests/PlayMode/WeaponInBattleTests.cs.meta
git commit -m "무기군이 인게임까지 도달하는지 검증하는 통합 테스트 추가"
```

---

## Self-Review 결과

**스펙 커버리지**

| 스펙 항목 | 담당 태스크 |
|---|---|
| §3 피해 적용을 탄환으로 이전, 풀링 버그 수정 | Task 1 |
| §4 `WeaponGroupSO`, `CharacterDataSO` 참조 | Task 2 |
| §5 `Single` / `Spread` | Task 3 |
| §5 `Piercing` (저격총, 검) | Task 1(관통 동작) + Task 4(발사) |
| §5 `Explosive` (유탄) | Task 5 |
| §5 `Chain` (지팡이) | Task 5 |
| §5 `Beam` (레이저) | Task 6 |
| §5 기관총 예열 | Task 7 |
| §6 `IAttackPattern` 전략 구조 | Task 3 |
| §6 하위 호환 (무기군 미주입 시 기존 동작) | Task 7 Step 3, Task 7 테스트 1번 |
| §7 `BattleSceneInitializer` 주입 | Task 7 Step 4 |
| §8 무기군 9종 에셋 생성 | Task 8 |
| §9 검증 10항목 | Task 1·3·4·5·6·7·9에 분산 |

**§9 검증 항목 대응**

| 스펙의 검증 항목 | 어디서 |
|---|---|
| 관통 통과 / 한계 / 감쇠 / 비관통 소멸 | Task 1 |
| 폭발 범위 | Task 5 |
| 연쇄 전파 | Task 5 |
| 빔 직선 | Task 5(`ApplyInBeam`) + Task 6(발사체 미생성) |
| 산탄 수 | Task 3 |
| 풀 반환 | Task 1 Step 3에서 `Release()`가 풀로 반환. 적/보스의 `Destroy` 제거로 보장 |
| 하위 호환 | Task 7 |

**의도적으로 다르게 한 것**

스펙 §6의 `AttackContextDTO`는 `Target`(IAttackTarget) 필드를 포함했지만, 패턴 6종 중 어느 것도 표적 객체를 직접 쓰지 않는다 — 조준은 `PlayerAttackComponent`가 각도로 변환해 `BaseAngle`로 넘긴다. 쓰지 않는 필드라 뺐다. 대신 발사 위치 기준점으로 `Origin`을 넣었다.

**알려진 한계**

`AreaDamage.ResolveTarget`은 `EnemyController`/`BossController`가 없는 콜라이더도 피격 수에 센다. 테스트 더미를 세기 위한 것이며, 실제 피해는 `ApplyToOne`에서 두 컨트롤러에만 적용된다. 실전에서 태그가 `Enemy`인데 컨트롤러가 없는 오브젝트는 없다.
