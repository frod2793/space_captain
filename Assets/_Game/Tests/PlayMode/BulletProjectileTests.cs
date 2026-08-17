using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

public class BulletProjectileTests
{
    private readonly List<GameObject> m_createdObjects = new List<GameObject>();
    private readonly List<DamageRecord> m_damageRecords = new List<DamageRecord>();
    private Type m_bulletType;
    private Type m_enemyType;
    private EventInfo m_damageEvent;
    private Action<string, int> m_damageHandler;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        m_damageRecords.Clear();
        var poolObject = Track(new GameObject("BulletProjectileTests_Pool"));
        poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        m_bulletType = TestReflectionHelper.GetGameType("BulletProjectile");
        m_enemyType = TestReflectionHelper.GetGameType("EnemyController");
        m_damageEvent = m_enemyType.GetEvent("OnDamageDealt", BindingFlags.Public | BindingFlags.Static);
        m_damageHandler = (ownerID, damage) => m_damageRecords.Add(new DamageRecord(ownerID, damage));
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

        yield return null;
        m_createdObjects.Clear();
    }

    [UnityTest]
    public IEnumerator BulletAppliesDamageThroughIAttackTarget()
    {
        var target = CreateTarget("BulletProjectileTests_Target", Vector3.zero);
        var bullet = CreateBullet(new Vector3(-3f, 0f, 0f));
        SetBulletProperty(bullet, "Damage", 25);
        SetBulletProperty(bullet, "OwnerID", "player-1");

        yield return MoveBulletIntoTarget(bullet, target);

        Assert.AreEqual(1, m_damageRecords.Count);
        Assert.AreEqual(25, m_damageRecords[0].Damage);
        Assert.AreEqual("player-1", m_damageRecords[0].OwnerID);
    }

    [UnityTest]
    public IEnumerator BulletDoesNotDamageTheSameTargetTwice()
    {
        var target = CreateTarget("BulletProjectileTests_Target", Vector3.zero);
        var bullet = CreateBullet(new Vector3(-3f, 0f, 0f));
        SetBulletProperty(bullet, "Damage", 25);
        SetBulletProperty(bullet, "MaxTargets", 2);

        yield return MoveBulletIntoTarget(bullet, target);
        yield return MoveBulletOutOfTarget(bullet);
        yield return MoveBulletIntoTarget(bullet, target);

        Assert.AreEqual(1, m_damageRecords.Count);
    }

    [UnityTest]
    public IEnumerator BulletWithNoPierceReleasesOnFirstHit()
    {
        var target = CreateTarget("BulletProjectileTests_Target", Vector3.zero);
        var bullet = CreateBullet(new Vector3(-3f, 0f, 0f));
        SetBulletProperty(bullet, "Damage", 25);
        SetBulletProperty(bullet, "MaxTargets", 1);

        yield return MoveBulletIntoTarget(bullet, target);

        Assert.IsFalse(bullet.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator PiercingBulletDamagesDistinctTargetsWithRateAfterEachHit()
    {
        var firstTarget = CreateTarget("BulletProjectileTests_Target_1", Vector3.zero);
        var secondTarget = CreateTarget("BulletProjectileTests_Target_2", new Vector3(0f, 3f, 0f));
        var thirdTarget = CreateTarget("BulletProjectileTests_Target_3", new Vector3(0f, 6f, 0f));
        var bullet = CreateBullet(new Vector3(-3f, 0f, 0f));
        SetBulletProperty(bullet, "Damage", 100);
        SetBulletProperty(bullet, "MaxTargets", 3);
        SetBulletProperty(bullet, "PierceDamageRate", 0.5f);

        yield return MoveBulletIntoTarget(bullet, firstTarget);
        yield return MoveBulletIntoTarget(bullet, secondTarget);
        yield return MoveBulletIntoTarget(bullet, thirdTarget);

        Assert.AreEqual(3, m_damageRecords.Count);
        Assert.AreEqual(100, m_damageRecords[0].Damage);
        Assert.AreEqual(50, m_damageRecords[1].Damage);
        Assert.AreEqual(25, m_damageRecords[2].Damage);
        Assert.IsFalse(bullet.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator PoolActivatesNewObjectCreatedFromInactivePrefab()
    {
        var prefab = CreateBulletPrefab();
        prefab.SetActive(false);
        var pool = m_createdObjects[0].GetComponent(m_bulletType.Assembly.GetType("ObjectPoolManager"));
        var getFromPool = pool.GetType().GetMethod("GetFromPool", BindingFlags.Public | BindingFlags.Instance);

        var instance = (GameObject)getFromPool.Invoke(pool, new object[]
        {
            prefab,
            Vector3.zero,
            Quaternion.identity
        });

        Assert.IsTrue(instance.activeSelf);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReusedPooledBulletStartsNextShotWithConfiguredDamage()
    {
        var prefab = CreateBulletPrefab();
        var firstTarget = CreateTarget("BulletProjectileTests_Target_1", Vector3.zero);
        var pool = m_createdObjects[0].GetComponent(m_bulletType.Assembly.GetType("ObjectPoolManager"));
        var getFromPool = pool.GetType().GetMethod("GetFromPool", BindingFlags.Public | BindingFlags.Instance);
        var returnToPool = pool.GetType().GetMethod("ReturnToPool", BindingFlags.Public | BindingFlags.Instance);
        var bulletObject = (GameObject)getFromPool.Invoke(pool, new object[]
        {
            prefab,
            new Vector3(-3f, 0f, 0f),
            Quaternion.identity
        });
        var bullet = bulletObject.GetComponent(m_bulletType);
        m_bulletType.GetMethod("SetSpeed", BindingFlags.Public | BindingFlags.Instance).Invoke(bullet, new object[] { 0f });
        SetBulletProperty(bullet, "Damage", 100);
        SetBulletProperty(bullet, "MaxTargets", 2);
        SetBulletProperty(bullet, "PierceDamageRate", 0.5f);

        yield return MoveBulletIntoTarget(bullet, firstTarget);
        Assert.AreEqual(50, GetBulletProperty(bullet, "Damage"));

        returnToPool.Invoke(pool, new object[] { bulletObject });
        var secondTarget = CreateTarget("BulletProjectileTests_Target_2", new Vector3(0f, 3f, 0f));
        var reusedObject = (GameObject)getFromPool.Invoke(pool, new object[]
        {
            prefab,
            new Vector3(-3f, 0f, 0f),
            Quaternion.identity
        });

        Assert.AreSame(bulletObject, reusedObject);
        Assert.AreEqual(100, GetBulletProperty(reusedObject.GetComponent(m_bulletType), "Damage"));

        yield return MoveBulletIntoTarget(reusedObject.GetComponent(m_bulletType), secondTarget);
        Assert.AreEqual(2, m_damageRecords.Count);
        Assert.AreEqual(100, m_damageRecords[1].Damage);
    }

    [Test]
    public void DespawnResetsWeaponOnlyProjectileState()
    {
        var bullet = CreateBullet(Vector3.zero);
        SetBulletProperty(bullet, "MaxTargets", 3);
        SetBulletProperty(bullet, "PierceDamageRate", 0.5f);
        SetBulletProperty(bullet, "DamageFalloffRate", 0.4f);
        var onHitType = m_bulletType.GetProperty("OnHit", BindingFlags.Public | BindingFlags.Instance).PropertyType;
        SetBulletProperty(bullet, "OnHit", Delegate.CreateDelegate(onHitType,
            GetType().GetMethod(nameof(IgnoreHit), BindingFlags.NonPublic | BindingFlags.Static)));

        m_bulletType.GetMethod("OnDespawn", BindingFlags.Public | BindingFlags.Instance).Invoke(bullet, null);

        Assert.AreEqual(1, GetBulletProperty(bullet, "MaxTargets"));
        Assert.AreEqual(1f, GetBulletProperty(bullet, "PierceDamageRate"));
        Assert.AreEqual(0f, GetBulletProperty(bullet, "DamageFalloffRate"));
        Assert.IsNull(GetBulletProperty(bullet, "OnHit"));
    }

    [Test]
    public void ResetForLegacyFire_RestoresSerializedSpeedAndRange()
    {
        var bullet = CreateBullet(Vector3.zero);
        SetPrivateField(bullet, "m_speed", 15f);
        SetPrivateField(bullet, "m_maxRange", 10f);
        SetPrivateField(bullet, "m_defaultSpeed", 15f);
        SetPrivateField(bullet, "m_defaultRange", 10f);
        m_bulletType.GetMethod("SetSpeed", BindingFlags.Public | BindingFlags.Instance).Invoke(bullet, new object[] { 99f });
        m_bulletType.GetMethod("SetRange", BindingFlags.Public | BindingFlags.Instance).Invoke(bullet, new object[] { 3f });

        m_bulletType.GetMethod("ResetForLegacyFire", BindingFlags.Public | BindingFlags.Instance).Invoke(bullet, null);

        Assert.AreEqual(15f, GetPrivateField(bullet, "m_speed"));
        Assert.AreEqual(10f, GetPrivateField(bullet, "m_maxRange"));
    }

    [Test]
    public void ShotgunFalloffDealsLessDamageAtLongRange()
    {
        var nearTarget = CreateTarget("BulletProjectileTests_FalloffNear", new Vector3(0f, 1f, 0f));
        var farTarget = CreateTarget("BulletProjectileTests_FalloffFar", new Vector3(0f, 9f, 0f));
        var falloffProperty = m_bulletType.GetProperty("DamageFalloffRate", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(falloffProperty, "BulletProjectile must expose weapon damage falloff.");

        var nearBullet = CreateBullet(Vector3.zero);
        SetBulletProperty(nearBullet, "Damage", 100);
        SetBulletProperty(nearBullet, "MaxTargets", 1);
        SetBulletProperty(nearBullet, "DamageFalloffRate", 0.6f);
        nearBullet.transform.position = nearTarget.transform.position;
        InvokeTrigger(nearBullet, nearTarget);

        var farBullet = CreateBullet(Vector3.zero);
        SetBulletProperty(farBullet, "Damage", 100);
        SetBulletProperty(farBullet, "MaxTargets", 1);
        SetBulletProperty(farBullet, "DamageFalloffRate", 0.6f);
        farBullet.transform.position = farTarget.transform.position;
        InvokeTrigger(farBullet, farTarget);

        Assert.AreEqual(2, m_damageRecords.Count);
        Assert.Greater(m_damageRecords[0].Damage, m_damageRecords[1].Damage);
    }

    [Test]
    public void ZeroFalloffWeaponDealsSameDamageAtEveryRange()
    {
        var nearTarget = CreateTarget("BulletProjectileTests_NoFalloffNear", new Vector3(0f, 1f, 0f));
        var farTarget = CreateTarget("BulletProjectileTests_NoFalloffFar", new Vector3(0f, 9f, 0f));
        var falloffProperty = m_bulletType.GetProperty("DamageFalloffRate", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(falloffProperty, "BulletProjectile must expose weapon damage falloff.");

        var nearBullet = CreateBullet(Vector3.zero);
        SetBulletProperty(nearBullet, "Damage", 100);
        SetBulletProperty(nearBullet, "DamageFalloffRate", 0f);
        nearBullet.transform.position = nearTarget.transform.position;
        InvokeTrigger(nearBullet, nearTarget);

        var farBullet = CreateBullet(Vector3.zero);
        SetBulletProperty(farBullet, "Damage", 100);
        SetBulletProperty(farBullet, "DamageFalloffRate", 0f);
        farBullet.transform.position = farTarget.transform.position;
        InvokeTrigger(farBullet, farTarget);

        Assert.AreEqual(2, m_damageRecords.Count);
        Assert.AreEqual(m_damageRecords[0].Damage, m_damageRecords[1].Damage);
    }

    private GameObject CreateBulletPrefab()
    {
        var prefab = Track(new GameObject("BulletProjectileTests_Prefab"));
        prefab.transform.position = new Vector3(100f, 100f, 0f);
        prefab.AddComponent<BoxCollider2D>().isTrigger = true;
        var body = prefab.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        prefab.AddComponent(m_bulletType);
        return prefab;
    }

    private Component CreateBullet(Vector3 position)
    {
        var bulletObject = Track(new GameObject("BulletProjectileTests_Bullet"));
        bulletObject.transform.position = position;
        bulletObject.AddComponent<BoxCollider2D>().isTrigger = true;
        var body = bulletObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var bullet = bulletObject.AddComponent(m_bulletType);
        m_bulletType.GetMethod("SetSpeed", BindingFlags.Public | BindingFlags.Instance).Invoke(bullet, new object[] { 0f });
        return bullet;
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

    private void InvokeTrigger(Component bullet, Component target)
    {
        m_bulletType.GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(bullet, new object[] { target.GetComponent<Collider2D>() });
    }

    private GameObject Track(GameObject gameObject)
    {
        m_createdObjects.Add(gameObject);
        return gameObject;
    }

    private void SetBulletProperty(Component bullet, string propertyName, object value)
    {
        m_bulletType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).SetValue(bullet, value);
    }

    private object GetBulletProperty(Component bullet, string propertyName)
    {
        return m_bulletType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).GetValue(bullet);
    }

    private void SetPrivateField(Component bullet, string fieldName, object value)
    {
        m_bulletType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(bullet, value);
    }

    private object GetPrivateField(Component bullet, string fieldName)
    {
        return m_bulletType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bullet);
    }

    private static void IgnoreHit(object target)
    {
    }

    private readonly struct DamageRecord
    {
        public readonly string OwnerID;
        public readonly int Damage;

        public DamageRecord(string ownerID, int damage)
        {
            OwnerID = ownerID;
            Damage = damage;
        }
    }
}
