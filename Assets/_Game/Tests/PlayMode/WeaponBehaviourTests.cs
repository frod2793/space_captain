using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WeaponBehaviourTests
{
    private readonly List<GameObject> m_createdObjects = new List<GameObject>();
    private readonly List<int> m_damageRecords = new List<int>();
    private Type m_enemyType;
    private EventInfo m_damageEvent;
    private Action<string, int> m_damageHandler;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // 전투 씬을 띄운 테스트가 timeScale = 0을 남기면 스케일 시간 대기가 영원히 만료되지 않는다
        Time.timeScale = 1f;

        m_damageRecords.Clear();
        m_enemyType = TestReflectionHelper.GetGameType("EnemyController");
        m_damageEvent = m_enemyType.GetEvent("OnDamageDealt", BindingFlags.Public | BindingFlags.Static);
        m_damageHandler = (_, damage) => m_damageRecords.Add(damage);
        m_damageEvent.AddEventHandler(null, m_damageHandler);
        yield return new WaitForFixedUpdate();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        m_damageEvent?.RemoveEventHandler(null, m_damageHandler);
        for (int i = 0; i < m_createdObjects.Count; i++)
        {
            if (m_createdObjects[i] != null)
            {
                UnityEngine.Object.Destroy(m_createdObjects[i]);
            }
        }

        yield return new WaitForFixedUpdate();
        Physics2D.SyncTransforms();
        m_createdObjects.Clear();
    }

    [UnityTest]
    public IEnumerator StraightWeaponFiresAlongSuppliedTargetDirection()
    {
        var origin = Track(new GameObject("WeaponBehaviourTests_StraightOrigin")).transform;
        var target = CreateTarget("WeaponBehaviourTests_StraightTarget", new Vector3(0f, 5f, 0f));
        var prefab = CreateBulletPrefab();
        var data = CreateData(prefab);
        SetField(data, "ProjectileSpeed", 0f);

        object context = CreateContext(data, origin.position, 0f, 5, "player", target, origin);
        InvokeFire("StraightWeapon", context);

        var bullet = FindBulletAt(origin.position, prefab.name);
        Assert.IsNotNull(bullet);
        Track(bullet.gameObject);
        Assert.That(Mathf.Abs(bullet.transform.eulerAngles.z), Is.LessThan(0.01f));
        yield return null;
    }

    [UnityTest]
    public IEnumerator StraightWeaponReturnsProjectileAtConfiguredRange()
    {
        var origin = Track(new GameObject("WeaponBehaviourTests_StraightRangeOrigin")).transform;
        var poolObject = Track(new GameObject("WeaponBehaviourTests_StraightRangePool"));
        var pool = poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var prefab = CreateActiveBulletPrefab();
        var data = CreateData(prefab);
        SetField(data, "Range", 1f);
        SetField(data, "ProjectileSpeed", 10f);

        object context = CreateContext(data, origin.position, 0f, 1, "player", null, origin);
        SetField(context, "Pool", pool);
        InvokeFire("StraightWeapon", context);

        var bullet = FindBulletAt(origin.position, prefab.name);
        Assert.IsNotNull(bullet);
        yield return new WaitForSecondsRealtime(0.2f);

        Assert.IsFalse(bullet.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator StraightWeaponPassesDamagePierceAndReleaseToProjectile()
    {
        var origin = Track(new GameObject("WeaponBehaviourTests_StraightDamageOrigin")).transform;
        var first = CreateTarget("WeaponBehaviourTests_StraightDamageTarget_1", new Vector3(0f, 2f, 0f));
        var second = CreateTarget("WeaponBehaviourTests_StraightDamageTarget_2", new Vector3(0f, 4f, 0f));
        var poolObject = Track(new GameObject("WeaponBehaviourTests_StraightPool"));
        var pool = poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var prefab = CreateActiveBulletPrefab();
        var data = CreateData(prefab);
        SetField(data, "ProjectileSpeed", 0f);
        SetField(data, "MaxTargets", 2);
        SetField(data, "PierceDamageRate", 0.5f);

        object context = CreateContext(data, origin.position, 0f, 4, "player", first, origin);
        SetField(context, "Pool", pool);
        InvokeFire("StraightWeapon", context);

        var bullet = FindBulletAt(origin.position, prefab.name);
        Assert.IsNotNull(bullet);
        yield return MoveBulletIntoTarget(bullet, first);
        yield return MoveBulletOutOfTarget(bullet);
        yield return MoveBulletIntoTarget(bullet, second);

        Assert.AreEqual(2, m_damageRecords.Count);
        Assert.AreEqual(4, m_damageRecords[0]);
        Assert.AreEqual(2, m_damageRecords[1]);
        Assert.IsFalse(bullet.gameObject.activeSelf);
        m_createdObjects.Add(bullet.gameObject);
    }

    [UnityTest]
    public IEnumerator WeaponDataUsesMaxTargetsAndDamageMultiplierDefaults()
    {
        var dataType = TestReflectionHelper.GetGameType("WeaponDataSO");
        Assert.IsNotNull(dataType.GetField("MaxTargets", BindingFlags.Public | BindingFlags.Instance));
        Assert.IsNull(dataType.GetField("PierceCount", BindingFlags.Public | BindingFlags.Instance));
        Assert.IsNull(dataType.GetField("ChargeTime", BindingFlags.Public | BindingFlags.Instance));
        Assert.IsNotNull(dataType.GetField("DamageMultiplier", BindingFlags.Public | BindingFlags.Instance));

        var expectedMultipliers = new Dictionary<string, float>
        {
            ["pistol"] = 1f,
            ["rifle"] = 0.4f,
            ["machine-gun"] = 0.15f,
            ["shotgun"] = 0.32f,
            ["sniper-rifle"] = 1.5f,
            ["sword"] = 2f,
            ["laser"] = 0.2f,
            ["grenade-launcher"] = 0.6f,
            ["staff"] = 0.6f
        };
        var expectedMaxTargets = new Dictionary<string, int>
        {
            ["pistol"] = 1,
            ["rifle"] = 1,
            ["machine-gun"] = 1,
            ["shotgun"] = 1,
            ["sniper-rifle"] = 3,
            ["sword"] = 5,
            ["laser"] = 1,
            ["grenade-launcher"] = 1,
            ["staff"] = 1
        };
        var weapons = Resources.LoadAll("Weapons", dataType);
        Assert.AreEqual(9, weapons.Length);
        for (int i = 0; i < weapons.Length; i++)
        {
            string weaponId = (string)dataType.GetField("WeaponID").GetValue(weapons[i]);
            Assert.AreEqual(expectedMultipliers[weaponId], (float)dataType.GetField("DamageMultiplier").GetValue(weapons[i]));
            Assert.AreEqual(expectedMaxTargets[weaponId], (int)dataType.GetField("MaxTargets").GetValue(weapons[i]));
        }

        yield return null;
    }

    [Test]
    public void SwordSingleShotDamageIsAtLeastSniperForTheSameCharacterAttackDamage()
    {
        var dataType = TestReflectionHelper.GetGameType("WeaponDataSO");
        var weapons = Resources.LoadAll("Weapons", dataType);
        UnityEngine.Object sword = null;
        UnityEngine.Object sniper = null;
        for (int i = 0; i < weapons.Length; i++)
        {
            string weaponId = (string)dataType.GetField("WeaponID").GetValue(weapons[i]);
            if (weaponId == "sword") sword = weapons[i];
            if (weaponId == "sniper-rifle") sniper = weapons[i];
        }

        Assert.IsNotNull(sword);
        Assert.IsNotNull(sniper);
        const int attackDamage = 10;
        int swordDamage = Mathf.CeilToInt(attackDamage * (float)dataType.GetField("DamageMultiplier").GetValue(sword));
        int sniperDamage = Mathf.CeilToInt(attackDamage * (float)dataType.GetField("DamageMultiplier").GetValue(sniper));
        Assert.GreaterOrEqual(swordDamage, sniperDamage);
    }

    [UnityTest]
    public IEnumerator BeamWeaponDamagesEveryTargetInTheBeam()
    {
        var origin = Track(new GameObject("WeaponBehaviourTests_BeamOrigin")).transform;
        var first = CreateTarget("WeaponBehaviourTests_BeamTarget_1", new Vector3(0f, 2f, 0f));
        var second = CreateTarget("WeaponBehaviourTests_BeamTarget_2", new Vector3(0f, 5f, 0f));
        CreateTarget("WeaponBehaviourTests_BeamTarget_Outside", new Vector3(2f, 2f, 0f));
        Physics2D.SyncTransforms();

        var data = CreateData(null);
        SetField(data, "BeamWidth", 1f);
        SetField(data, "BeamRange", 10f);
        object context = CreateContext(data, origin.position, 0f, 2, "player", first, origin);
        InvokeFire("BeamWeapon", context);

        Assert.AreEqual(2, m_damageRecords.Count);
        yield return null;
    }

    [UnityTest]
    public IEnumerator BeamWeaponReusesPooledSkillLaserVisual()
    {
        var origin = Track(new GameObject("WeaponBehaviourTests_BeamVisualOrigin")).transform;
        var poolObject = Track(new GameObject("WeaponBehaviourTests_BeamVisualPool"));
        var pool = poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var visualPrefab = CreateVisualPrefab();
        var data = CreateData(null);
        SetField(data, "BeamVisualPrefab", visualPrefab);
        SetField(data, "BeamWidth", 1f);
        SetField(data, "BeamRange", 5f);
        object context = CreateContext(data, origin.position, 0f, 1, "player", null, origin);
        SetField(context, "Pool", pool);

        InvokeFire("BeamWeapon", context);

        var visual = FindActiveVisual(visualPrefab);
        Assert.IsNotNull(visual);
        visual.SetActive(false);
        yield return new WaitForSecondsRealtime(0.2f);
        InvokeFire("BeamWeapon", context);

        Assert.AreSame(visual, FindActiveVisual(visualPrefab));
        yield return null;
    }

    [UnityTest]
    public IEnumerator ExplosiveWeaponOnlyDamagesTargetsInsideRadius()
    {
        var center = CreateTarget("WeaponBehaviourTests_ExplosionCenter", new Vector3(0f, 3f, 0f));
        CreateTarget("WeaponBehaviourTests_ExplosionInside", new Vector3(1f, 3f, 0f));
        CreateTarget("WeaponBehaviourTests_ExplosionOutside", new Vector3(3f, 3f, 0f));
        Physics2D.SyncTransforms();

        var poolObject = Track(new GameObject("WeaponBehaviourTests_ExplosionPool"));
        var pool = poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var prefab = CreateExplosivePrefab();
        var data = CreateData(prefab);
        SetField(data, "ExplosionRadius", 1.5f);
        SetField(data, "ProjectileSpeed", 10f);
        object context = CreateContext(data, Vector3.zero, 0f, 3, "player", center, null);
        SetField(context, "Pool", pool);
        InvokeFire("ExplosiveWeapon", context);

        var projectile = FindExplosiveProjectileAt(Vector3.zero);
        Assert.IsNotNull(projectile);
        Assert.IsTrue(projectile.gameObject.activeSelf);
        m_createdObjects.Add(projectile.gameObject);
        yield return new WaitForSecondsRealtime(0.5f);

        Assert.AreEqual(2, m_damageRecords.Count);
        Assert.IsFalse(projectile.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator ChainWeaponWaitsForPrimaryProjectileHitAndDoesNotHitSameTargetTwice()
    {
        var origin = Track(new GameObject("WeaponBehaviourTests_ChainOrigin")).transform;
        var first = CreateTarget("WeaponBehaviourTests_ChainTarget_1", Vector3.zero);
        CreateTarget("WeaponBehaviourTests_ChainTarget_2", new Vector3(1f, 0f, 0f));
        CreateTarget("WeaponBehaviourTests_ChainTarget_3", new Vector3(2f, 0f, 0f));
        var poolObject = Track(new GameObject("WeaponBehaviourTests_ChainPool"));
        var pool = poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var prefab = CreateActiveBulletPrefab();
        Physics2D.SyncTransforms();

        var data = CreateData(prefab);
        SetField(data, "ChainCount", 4);
        SetField(data, "ChainRange", 1.5f);
        SetField(data, "ChainDamageRate", 0.5f);
        object context = CreateContext(data, origin.position, 0f, 4, "player", first, origin);
        SetField(context, "Pool", pool);
        InvokeFire("ChainWeapon", context);

        Assert.AreEqual(0, m_damageRecords.Count);
        var bullet = FindBulletAt(origin.position, prefab.name);
        Assert.IsNotNull(bullet);
        var trigger = bullet.GetType().GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        trigger.Invoke(bullet, new object[] { first.GetComponent<Collider2D>() });

        Assert.AreEqual(3, m_damageRecords.Count);
        Assert.AreEqual(4, m_damageRecords[0]);
        Assert.AreEqual(2, m_damageRecords[1]);
        Assert.AreEqual(1, m_damageRecords[2]);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ChainWeaponStartsAfterLethalPrimaryProjectileHit()
    {
        var origin = Track(new GameObject("WeaponBehaviourTests_LethalChainOrigin")).transform;
        var first = CreateTarget("WeaponBehaviourTests_LethalChainTarget_1", Vector3.zero);
        var second = CreateTarget("WeaponBehaviourTests_LethalChainTarget_2", new Vector3(1f, 0f, 0f));
        var poolObject = Track(new GameObject("WeaponBehaviourTests_LethalChainPool"));
        var pool = poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var prefab = CreateActiveBulletPrefab();
        Physics2D.SyncTransforms();

        var data = CreateData(prefab);
        SetField(data, "ChainCount", 1);
        SetField(data, "ChainRange", 1.5f);
        SetField(data, "ChainDamageRate", 0.5f);
        object context = CreateContext(data, origin.position, 0f, 10, "player", first, origin);
        SetField(context, "Pool", pool);
        InvokeFire("ChainWeapon", context);

        var bullet = FindBulletAt(origin.position, prefab.name);
        Assert.IsNotNull(bullet);
        var trigger = bullet.GetType().GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        trigger.Invoke(bullet, new object[] { first.GetComponent<Collider2D>() });

        Assert.AreEqual(2, m_damageRecords.Count);
        CollectionAssert.AreEquivalent(new[] { 10, 5 }, m_damageRecords);
        Assert.IsFalse(first.gameObject.activeSelf);
        Assert.IsTrue(second.gameObject.activeSelf);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ChainWeaponReusesPooledSkillLaserVisualAfterPrimaryHit()
    {
        var origin = Track(new GameObject("WeaponBehaviourTests_ChainVisualOrigin")).transform;
        var target = CreateTarget("WeaponBehaviourTests_ChainVisualTarget", Vector3.zero);
        CreateTarget("WeaponBehaviourTests_ChainVisualNextTarget", new Vector3(1f, 0f, 0f));
        var poolObject = Track(new GameObject("WeaponBehaviourTests_ChainVisualPool"));
        var pool = poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var visualPrefab = CreateVisualPrefab();
        var projectilePrefab = CreateActiveBulletPrefab();
        var data = CreateData(projectilePrefab);
        SetField(data, "BeamVisualPrefab", visualPrefab);
        SetField(data, "ChainCount", 1);
        SetField(data, "ChainRange", 2f);
        object context = CreateContext(data, origin.position, 0f, 1, "player", target, origin);
        SetField(context, "Pool", pool);
        Physics2D.SyncTransforms();

        InvokeFire("ChainWeapon", context);

        Assert.AreEqual(0, m_damageRecords.Count);
        var bullet = FindBulletAt(origin.position, projectilePrefab.name);
        var trigger = bullet.GetType().GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        trigger.Invoke(bullet, new object[] { target.GetComponent<Collider2D>() });
        var visual = FindActiveVisual(visualPrefab);
        Assert.IsNotNull(visual);
        visual.SetActive(false);
        yield return new WaitForSecondsRealtime(0.2f);
        InvokeFire("ChainWeapon", context);
        bullet = FindBulletAt(origin.position, projectilePrefab.name);
        trigger = bullet.GetType().GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);
        trigger.Invoke(bullet, new object[] { target.GetComponent<Collider2D>() });
        Assert.AreSame(visual, FindActiveVisual(visualPrefab));
        yield return null;
    }

    [Test]
    public void WeaponTargetQueryUsesReusableNonAllocBuffersAndSharedTargetFilter()
    {
        var queryType = TestReflectionHelper.GetGameType("WeaponTargetQuery");
        Assert.IsNotNull(queryType);
        Assert.IsNotNull(queryType.GetMethod("BoxCast", BindingFlags.Public | BindingFlags.Static));
        Assert.IsNotNull(queryType.GetMethod("OverlapCircle", BindingFlags.Public | BindingFlags.Static));
        Assert.IsNotNull(queryType.GetMethod("TryGetEnemyTarget", BindingFlags.Public | BindingFlags.Static));

        object[] firstBoxCastArgs = { Vector2.zero, Vector2.one, 0f, Vector2.up, 1f, null };
        object[] secondBoxCastArgs = { Vector2.zero, Vector2.one, 0f, Vector2.up, 1f, null };
        object[] firstOverlapArgs = { Vector2.zero, 1f, null };
        object[] secondOverlapArgs = { Vector2.zero, 1f, null };
        object firstRaycastResult = queryType.GetMethod("BoxCast").Invoke(null, firstBoxCastArgs);
        object secondRaycastResult = queryType.GetMethod("BoxCast").Invoke(null, secondBoxCastArgs);
        object firstOverlapResult = queryType.GetMethod("OverlapCircle").Invoke(null, firstOverlapArgs);
        object secondOverlapResult = queryType.GetMethod("OverlapCircle").Invoke(null, secondOverlapArgs);
        RaycastHit2D[] firstRaycastBuffer = (RaycastHit2D[])firstBoxCastArgs[5];
        RaycastHit2D[] secondRaycastBuffer = (RaycastHit2D[])secondBoxCastArgs[5];
        Collider2D[] firstOverlapBuffer = (Collider2D[])firstOverlapArgs[2];
        Collider2D[] secondOverlapBuffer = (Collider2D[])secondOverlapArgs[2];
        int firstRaycastCount = (int)firstRaycastResult;
        int secondRaycastCount = (int)secondRaycastResult;
        int firstOverlapCount = (int)firstOverlapResult;
        int secondOverlapCount = (int)secondOverlapResult;
        Assert.AreEqual(firstRaycastCount, secondRaycastCount);
        Assert.AreEqual(firstOverlapCount, secondOverlapCount);
        Assert.AreSame(firstRaycastBuffer, secondRaycastBuffer);
        Assert.AreSame(firstOverlapBuffer, secondOverlapBuffer);
    }

    [Test]
    public void WeaponTargetQueryRequiresEnemyOrBossTagAndFindsBossOnColliderParent()
    {
        var queryType = TestReflectionHelper.GetGameType("WeaponTargetQuery");
        var tryGetEnemyTarget = queryType.GetMethod("TryGetEnemyTarget");
        var bossType = TestReflectionHelper.GetGameType("BossController");

        var enemyObject = Track(new GameObject("WeaponBehaviourTests_UntaggedEnemy"));
        var enemyCollider = enemyObject.AddComponent<BoxCollider2D>();
        enemyObject.AddComponent(m_enemyType);
        object[] enemyArgs = { enemyCollider, null };
        Assert.IsFalse((bool)tryGetEnemyTarget.Invoke(null, enemyArgs));

        var bossObject = Track(new GameObject("WeaponBehaviourTests_BossParent"));
        bossObject.tag = "Boss";
        var boss = bossObject.AddComponent(bossType);
        var colliderObject = Track(new GameObject("WeaponBehaviourTests_BossCollider"));
        colliderObject.transform.SetParent(bossObject.transform);
        var bossCollider = colliderObject.AddComponent<BoxCollider2D>();
        object[] bossArgs = { bossCollider, null };

        Assert.IsTrue((bool)tryGetEnemyTarget.Invoke(null, bossArgs));
        Assert.AreSame(boss, bossArgs[1]);

        bossObject.tag = "Untagged";
        bossArgs[1] = null;
        Assert.IsFalse((bool)tryGetEnemyTarget.Invoke(null, bossArgs));
    }

    [Test]
    public void WeaponTargetQueryExpandsBuffersWhenResultsAreSaturated()
    {
        var queryType = TestReflectionHelper.GetGameType("WeaponTargetQuery");
        var boxCast = queryType.GetMethod("BoxCast");
        var overlapCircle = queryType.GetMethod("OverlapCircle");
        for (int i = 0; i < 40; i++)
        {
            var colliderObject = Track(new GameObject($"WeaponBehaviourTests_BufferCollider_{i}"));
            colliderObject.AddComponent<BoxCollider2D>();
        }

        Physics2D.SyncTransforms();
        object[] boxArgs = { Vector2.zero, new Vector2(2f, 2f), 0f, Vector2.up, 0f, null };
        object[] overlapArgs = { Vector2.zero, 2f, null };
        int boxCount = (int)boxCast.Invoke(null, boxArgs);
        int overlapCount = (int)overlapCircle.Invoke(null, overlapArgs);

        Assert.GreaterOrEqual(boxCount, 40);
        Assert.GreaterOrEqual(overlapCount, 40);
        Assert.Greater(((RaycastHit2D[])boxArgs[5]).Length, 32);
        Assert.Greater(((Collider2D[])overlapArgs[2]).Length, 32);
    }

    [Test]
    public void ExplosiveProjectileReusesAndClearsHitBuffer()
    {
        var projectileType = TestReflectionHelper.GetGameType("ExplosiveProjectile");
        var projectile = Track(new GameObject("WeaponBehaviourTests_ExplosiveBuffer"))
            .AddComponent(projectileType);
        var bufferField = projectileType.GetField("m_hitIds", BindingFlags.NonPublic | BindingFlags.Instance);
        var applyDamage = projectileType.GetMethod("ApplyExplosionDamage", BindingFlags.NonPublic | BindingFlags.Instance);
        var firstBuffer = (HashSet<int>)bufferField.GetValue(projectile);
        firstBuffer.Add(123);
        applyDamage.Invoke(projectile, null);
        var secondBuffer = (HashSet<int>)bufferField.GetValue(projectile);

        Assert.AreSame(firstBuffer, secondBuffer);
        Assert.IsFalse(secondBuffer.Contains(123));
    }

    private GameObject CreateBulletPrefab()
    {
        var prefab = Track(new GameObject($"WeaponBehaviourTests_BulletPrefab_{Guid.NewGuid():N}"));
        prefab.transform.position = new Vector3(100f, 100f, 0f);
        prefab.SetActive(false);
        prefab.AddComponent<BoxCollider2D>().isTrigger = true;
        var body = prefab.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        prefab.AddComponent(TestReflectionHelper.GetGameType("BulletProjectile"));
        return prefab;
    }

    private GameObject CreateActiveBulletPrefab()
    {
        var prefab = Track(new GameObject($"WeaponBehaviourTests_ActiveBulletPrefab_{Guid.NewGuid():N}"));
        prefab.transform.position = new Vector3(100f, 100f, 0f);
        prefab.AddComponent<BoxCollider2D>().isTrigger = true;
        var body = prefab.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        prefab.AddComponent(TestReflectionHelper.GetGameType("BulletProjectile"));
        return prefab;
    }

    private GameObject CreateExplosivePrefab()
    {
        var prefab = Track(new GameObject($"WeaponBehaviourTests_ExplosivePrefab_{Guid.NewGuid():N}"));
        prefab.transform.position = new Vector3(100f, 100f, 0f);
        prefab.AddComponent(TestReflectionHelper.GetGameType("ExplosiveProjectile"));
        return prefab;
    }

    private GameObject CreateVisualPrefab()
    {
        var prefab = Track(new GameObject($"WeaponBehaviourTests_VisualPrefab_{Guid.NewGuid():N}"));
        prefab.AddComponent(TestReflectionHelper.GetGameType("SkillLaser"));
        return prefab;
    }

    private GameObject FindActiveVisual(GameObject prefab)
    {
        var skillLaserType = TestReflectionHelper.GetGameType("SkillLaser");
        var visuals = UnityEngine.Object.FindObjectsByType(skillLaserType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < visuals.Length; i++)
        {
            var visual = (Component)visuals[i];
            if (visual.gameObject != prefab && visual.gameObject.activeSelf && visual.gameObject.name == prefab.name)
            {
                return visual.gameObject;
            }
        }

        return null;
    }

    private Component CreateTarget(string name, Vector3 position)
    {
        var targetObject = Track(new GameObject(name));
        targetObject.tag = "Enemy";
        targetObject.transform.position = position;
        targetObject.AddComponent<BoxCollider2D>();
        return targetObject.AddComponent(m_enemyType);
    }

    private IEnumerator MoveBulletIntoTarget(Component bullet, Component target)
    {
        bullet.transform.position = target.transform.position;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
    }

    private IEnumerator MoveBulletOutOfTarget(Component bullet)
    {
        bullet.transform.position += Vector3.right * 3f;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
    }

    private object CreateData(GameObject projectilePrefab)
    {
        var dataType = TestReflectionHelper.GetGameType("WeaponDataSO");
        var data = ScriptableObject.CreateInstance(dataType);
        SetField(data, "ProjectilePrefab", projectilePrefab);
        SetField(data, "Behaviour", Enum.Parse(TestReflectionHelper.GetGameType("WeaponBehaviourType"), "Straight"));
        return data;
    }

    private object CreateContext(object data, Vector3 origin, float baseAngle, int damage, string ownerID, Component target, Transform firePoint)
    {
        var contextType = TestReflectionHelper.GetGameType("WeaponFireContext");
        object context = Activator.CreateInstance(contextType);
        SetField(context, "Origin", origin);
        SetField(context, "BaseAngle", baseAngle);
        SetField(context, "Damage", damage);
        SetField(context, "OwnerID", ownerID);
        SetField(context, "Target", target);
        SetField(context, "Data", data);
        SetField(context, "BulletCount", 1);
        SetField(context, "SpreadAngle", 0f);
        SetField(context, "ScaleMultiplier", 1f);

        var firePoints = Array.CreateInstance(typeof(Transform), firePoint == null ? 0 : 1);
        if (firePoint != null)
        {
            firePoints.SetValue(firePoint, 0);
        }
        SetField(context, "FirePoints", firePoints);
        return context;
    }

    private Component FindBulletAt(Vector3 position, string prefabName)
    {
        var bulletType = TestReflectionHelper.GetGameType("BulletProjectile");
        var bullets = UnityEngine.Object.FindObjectsByType(bulletType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < bullets.Length; i++)
        {
            var bullet = (Component)bullets[i];
            if ((bullet.gameObject.name == prefabName || bullet.gameObject.name == prefabName + "(Clone)") &&
                Vector3.Distance(bullet.transform.position, position) <= 0.01f)
            {
                bullet.gameObject.SetActive(true);
                return bullet;
            }
        }

        return null;
    }

    private Component FindExplosiveProjectileAt(Vector3 position)
    {
        var projectileType = TestReflectionHelper.GetGameType("ExplosiveProjectile");
        var projectiles = UnityEngine.Object.FindObjectsByType(projectileType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < projectiles.Length; i++)
        {
            var projectile = (Component)projectiles[i];
            if (Vector3.Distance(projectile.transform.position, position) <= 0.01f)
            {
                return projectile;
            }
        }

        return null;
    }

    private void InvokeFire(string behaviourTypeName, object context)
    {
        var behaviourType = TestReflectionHelper.GetGameType(behaviourTypeName);
        var behaviour = Activator.CreateInstance(behaviourType);
        var fire = behaviourType.GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);
        fire.Invoke(behaviour, new[] { context });
    }

    private void SetField(object target, string fieldName, object value)
    {
        target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance).SetValue(target, value);
    }

    private GameObject Track(GameObject gameObject)
    {
        m_createdObjects.Add(gameObject);
        return gameObject;
    }
}
