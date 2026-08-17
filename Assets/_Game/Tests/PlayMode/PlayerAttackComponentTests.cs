using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerAttackComponentTests
{
    private const string DeckSaveKey = "SpaceCaptain.Deck";
    private readonly List<GameObject> m_createdObjects = new List<GameObject>();
    private readonly List<UnityEngine.Object> m_createdAssets = new List<UnityEngine.Object>();
    private readonly List<int> m_damageRecords = new List<int>();
    private readonly List<string> m_damageOwners = new List<string>();
    private Type m_enemyType;
    private EventInfo m_damageEvent;
    private Action<string, int> m_damageHandler;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        m_damageRecords.Clear();
        m_damageOwners.Clear();
        m_enemyType = TestReflectionHelper.GetGameType("EnemyController");
        m_damageEvent = m_enemyType.GetEvent("OnDamageDealt", BindingFlags.Public | BindingFlags.Static);
        m_damageHandler = (ownerID, damage) =>
        {
            m_damageOwners.Add(ownerID);
            m_damageRecords.Add(damage);
        };
        m_damageEvent.AddEventHandler(null, m_damageHandler);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        m_damageEvent?.RemoveEventHandler(null, m_damageHandler);
        for (int i = 0; i < m_createdObjects.Count; i++)
        {
            if (m_createdObjects[i] != null)
            {
                UnityEngine.Object.Destroy(m_createdObjects[i]);
            }
        }

        for (int i = 0; i < m_createdAssets.Count; i++)
        {
            if (m_createdAssets[i] != null)
            {
                UnityEngine.Object.Destroy(m_createdAssets[i]);
            }
        }

        yield return null;
        m_createdObjects.Clear();
        m_createdAssets.Clear();
    }

    [UnityTest]
    public IEnumerator UpdateDispatchesToConfiguredBeamBehaviour()
    {
        var poolObject = Track(new GameObject("PlayerAttackComponentTests_Pool"));
        poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var target = CreateTarget("PlayerAttackComponentTests_BeamTarget", new Vector3(0f, 2f, 0f));
        Physics2D.SyncTransforms();
        var data = CreateWeaponData("Beam", null);
        SetField(data, "FireRate", 0f);
        SetField(data, "BeamWidth", 1f);
        SetField(data, "BeamRange", 10f);
        var attack = CreatePlayer(data, 7, false, null);
        SetProperty(attack, "CurrentTarget", target);

        bool timedOut = false;
        yield return new WaitUntil(
            () => HasDamageRecord("player-test", 7),
            TimeSpan.FromSeconds(1),
            () => timedOut = true,
            WaitTimeoutMode.Realtime);

        Assert.IsFalse(timedOut, "Beam attack did not dispatch damage within 1 second.");
        Assert.IsTrue(HasDamageRecord("player-test", 7), "Expected owner=player-test damage=7 event.");
    }

    [UnityTest]
    public IEnumerator UpdateAppliesWeaponDamageMultiplier()
    {
        var poolObject = Track(new GameObject("PlayerAttackComponentTests_DamageMultiplierPool"));
        poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var target = CreateTarget("PlayerAttackComponentTests_DamageMultiplierTarget", new Vector3(0f, 2f, 0f));
        Physics2D.SyncTransforms();
        var data = CreateWeaponData("Beam", null);
        SetField(data, "FireRate", 0f);
        SetField(data, "BeamWidth", 1f);
        SetField(data, "BeamRange", 10f);
        SetField(data, "DamageMultiplier", 0.5f);
        var attack = CreatePlayer(data, 7, false, null);
        SetProperty(attack, "CurrentTarget", target);

        bool timedOut = false;
        yield return new WaitUntil(
            () => HasDamageRecord("player-test", 4),
            TimeSpan.FromSeconds(1),
            () => timedOut = true,
            WaitTimeoutMode.Realtime);

        Assert.IsFalse(timedOut, "Weapon damage multiplier was not rounded up.");
    }

    [UnityTest]
    public IEnumerator SharedWeapon_UsesEachCharactersAttackDamage()
    {
        var poolObject = Track(new GameObject("PlayerAttackComponentTests_SharedWeaponPool"));
        poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var firstTarget = CreateTarget("PlayerAttackComponentTests_SharedWeaponTarget_1", new Vector3(-1f, 2f, 0f));
        var secondTarget = CreateTarget("PlayerAttackComponentTests_SharedWeaponTarget_2", new Vector3(1f, 2f, 0f));
        Physics2D.SyncTransforms();

        var sharedWeapon = CreateWeaponData("Beam", null);
        SetField(sharedWeapon, "FireRate", 0f);
        SetField(sharedWeapon, "BeamWidth", 1f);
        SetField(sharedWeapon, "BeamRange", 10f);
        SetField(sharedWeapon, "DamageMultiplier", 1.5f);

        var firstAttack = CreatePlayer(sharedWeapon, 50, false, null);
        var secondAttack = CreatePlayer(sharedWeapon, 40, false, null);
        SetProperty(firstAttack, "CurrentTarget", firstTarget);
        SetProperty(secondAttack, "CurrentTarget", secondTarget);

        bool timedOut = false;
        yield return new WaitUntil(
            () => HasDamageRecord("player-test", 75) && HasDamageRecord("player-test", 60),
            TimeSpan.FromSeconds(1),
            () => timedOut = true,
            WaitTimeoutMode.Realtime);

        Assert.IsFalse(timedOut, "같은 무기가 서로 다른 공격력의 캐릭터에 75와 60 데미지를 적용하지 못했습니다.");
    }

    [UnityTest]
    public IEnumerator SetWeapon_RecreatesBehaviourAndUsesNewWeaponDamage()
    {
        var poolObject = Track(new GameObject("PlayerAttackComponentTests_SetWeaponPool"));
        poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var target = CreateTarget("PlayerAttackComponentTests_SetWeaponTarget", new Vector3(0f, 2f, 0f));
        Physics2D.SyncTransforms();

        var straightWeapon = CreateWeaponData("Straight", null);
        var beamWeapon = CreateWeaponData("Beam", null);
        SetField(beamWeapon, "FireRate", 0f);
        SetField(beamWeapon, "BeamWidth", 1f);
        SetField(beamWeapon, "BeamRange", 10f);
        SetField(beamWeapon, "DamageMultiplier", 2f);
        var attack = CreatePlayer(straightWeapon, 7, false, null);

        MethodInfo setWeapon = attack.GetType().GetMethod("SetWeapon", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(setWeapon, "PlayerAttackComponent.SetWeapon 공개 API가 없습니다.");
        setWeapon.Invoke(attack, new[] { beamWeapon });
        SetProperty(attack, "CurrentTarget", target);

        bool timedOut = false;
        yield return new WaitUntil(
            () => HasDamageRecord("player-test", 14),
            TimeSpan.FromSeconds(1),
            () => timedOut = true,
            WaitTimeoutMode.Realtime);

        Assert.IsFalse(timedOut, "교체한 Beam 무기의 damage/behaviour가 적용되지 않았습니다.");
    }

    [UnityTest]
    public IEnumerator TargetingUsesWeaponRange()
    {
        var poolObject = Track(new GameObject("PlayerAttackComponentTests_RangePool"));
        poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        CreateTarget("PlayerAttackComponentTests_OutOfRangeTarget", new Vector3(0f, 2f, 0f));
        Physics2D.SyncTransforms();
        var data = CreateWeaponData("Beam", null);
        SetField(data, "FireRate", 0f);
        SetField(data, "Range", 1f);
        SetField(data, "BeamWidth", 1f);
        SetField(data, "BeamRange", 10f);
        CreatePlayer(data, 7, false, null);

        yield return null;

        Assert.AreEqual(0, m_damageRecords.Count);
    }

    [UnityTest]
    public IEnumerator ChangingTargetResetsWeaponWarmup()
    {
        var poolObject = Track(new GameObject("PlayerAttackComponentTests_WarmupPool"));
        poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var targetOne = CreateTarget("PlayerAttackComponentTests_WarmupTarget_1", new Vector3(0f, 2f, 0f));
        var targetTwo = CreateTarget("PlayerAttackComponentTests_WarmupTarget_2", new Vector3(1f, 2f, 0f));
        Physics2D.SyncTransforms();
        var data = CreateWeaponData("Beam", null);
        SetField(data, "FireRate", 0.5f);
        SetField(data, "WarmupTime", 1f);
        SetField(data, "MaxFireRate", 0.1f);
        var attack = CreatePlayer(data, 7, false, null);
        SetProperty(attack, "CurrentTarget", targetOne);

        yield return new WaitForSeconds(0.4f);
        SetProperty(attack, "CurrentTarget", targetTwo);
        yield return null;

        float warmupTimer = (float)GetPrivateField(attack, "m_warmupTimer");
        Assert.Less(warmupTimer, 0.1f);
    }

    [UnityTest]
    public IEnumerator UpdatePassesWeaponAndUpgradeBulletSpreadValues()
    {
        var poolObject = Track(new GameObject("PlayerAttackComponentTests_Pool"));
        poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var prefab = CreateBulletPrefab();
        var data = CreateWeaponData("Straight", prefab);
        SetField(data, "FireRate", 0f);
        SetField(data, "ProjectileSpeed", 0f);
        SetField(data, "BulletCount", 2);
        SetField(data, "SpreadAngle", 20f);
        var firePoints = new[]
        {
            Track(new GameObject("PlayerAttackComponentTests_FirePoint_1")).transform,
            Track(new GameObject("PlayerAttackComponentTests_FirePoint_2")).transform
        };
        var attack = CreatePlayer(data, 13, true, firePoints, 1, 5f);

        yield return null;

        var bullets = FindSpawnedBullets(Vector3.zero, prefab.name);
        Assert.AreEqual(3, bullets.Count);
        for (int i = 0; i < bullets.Count; i++)
        {
            Track(bullets[i].gameObject);
        }
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(bullets[0].transform.eulerAngles.z, -12.5f)), Is.LessThan(0.01f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(bullets[1].transform.eulerAngles.z, 0f)), Is.LessThan(0.01f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(bullets[2].transform.eulerAngles.z, 12.5f)), Is.LessThan(0.01f));
        for (int i = 0; i < bullets.Count; i++)
        {
            Assert.AreEqual(13, GetProperty(bullets[i], "Damage"));
        }
    }

    [Test]
    public void CharacterDefaultWeaponsAndReviewedWeaponValuesMatchDesign()
    {
        var rifle = Resources.Load("Weapons/rifle");
        var laser = Resources.Load("Weapons/laser");
        var sword = Resources.Load("Weapons/sword");
        var staff = Resources.Load("Weapons/staff");
        var d = Resources.Load("d_CharacterData");
        var e = Resources.Load("e_CharacterData");

        Assert.AreEqual("rifle", GetField(GetProperty(d, "DefaultWeapon"), "WeaponID"));
        Assert.AreEqual("shotgun", GetField(GetProperty(e, "DefaultWeapon"), "WeaponID"));
        Assert.AreEqual(0.15f, GetField(rifle, "FireRate"));
        Assert.AreEqual(0.8f, GetField(laser, "FireRate"));
        Assert.AreEqual(1.5f, GetField(laser, "BeamWidth"));
        Assert.AreEqual(1.5f, GetField(sword, "FireRate"));
        Assert.AreEqual(-1, GetField(sword, "MaxTargets"));
        Assert.AreEqual(6f, GetField(sword, "ProjectileSpeed"));
        Assert.AreEqual(3f, GetField(sword, "ProjectileScale"));
        Assert.AreEqual(0.7f, GetField(staff, "FireRate"));
        Assert.AreEqual(3, GetField(staff, "ChainCount"));
        Assert.AreEqual(4f, GetField(staff, "ChainRange"));
    }

    [UnityTest]
    public IEnumerator LegacyFireAfterPoolReuseAppliesLegacySpeedRangeAndActiveScale()
    {
        var poolObject = Track(new GameObject("PlayerAttackComponentTests_LegacyPool"));
        poolObject.AddComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var prefab = CreateBulletPrefab();
        var firePoint = Track(new GameObject("PlayerAttackComponentTests_LegacyFirePoint")).transform;
        var attack = CreatePlayer(null, 7, true, new[] { firePoint });
        SetPrivateField(attack, "m_bulletPrefab", prefab);
        SetPrivateField(attack, "m_bulletSpeed", 11f);

        var pool = poolObject.GetComponent(TestReflectionHelper.GetGameType("ObjectPoolManager"));
        var getFromPool = pool.GetType().GetMethod("GetFromPool", BindingFlags.Public | BindingFlags.Instance);
        var returnToPool = pool.GetType().GetMethod("ReturnToPool", BindingFlags.Public | BindingFlags.Instance);
        var pooled = (GameObject)getFromPool.Invoke(pool, new object[] { prefab, Vector3.zero, Quaternion.identity });
        var projectile = pooled.GetComponent(TestReflectionHelper.GetGameType("BulletProjectile"));
        projectile.GetType().GetMethod("SetSpeed", BindingFlags.Public | BindingFlags.Instance).Invoke(projectile, new object[] { 99f });
        projectile.GetType().GetMethod("SetRange", BindingFlags.Public | BindingFlags.Instance).Invoke(projectile, new object[] { 3f });
        returnToPool.Invoke(pool, new object[] { pooled });

        attack.GetType().GetMethod("FireLegacy", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(attack, null);
        yield return null;

        Assert.AreSame(pooled, FindSpawnedBullets(Vector3.zero, prefab.name)[0].gameObject);
        Assert.AreEqual(11f, GetPrivateField(projectile, "m_speed"));
        Assert.AreEqual(10f, GetPrivateField(projectile, "m_maxRange"));
        Assert.AreEqual(Vector3.one, pooled.transform.localScale);
    }

    [UnityTest]
    public IEnumerator MissingBaseStatsStillInjectsCharacterDefaultWeapon()
    {
        object characterData = Resources.Load("d_CharacterData");
        object originalStats = GetProperty(characterData, "BaseStats");
        bool hadDeck = PlayerPrefs.HasKey(DeckSaveKey);
        string originalDeck = PlayerPrefs.GetString(DeckSaveKey);
        SetField(characterData, "m_baseStats", null);
        PlayerPrefs.SetString(DeckSaveKey, "d");
        PlayerPrefs.Save();

        try
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("InGame");
            Component attack = null;
            for (int i = 0; i < 900 && attack == null; i++)
            {
                var controller = UnityEngine.Object.FindAnyObjectByType(TestReflectionHelper.GetGameType("PlayerCharacterController")) as Component;
                if (controller != null && (string)GetProperty(controller, "CharacterID") == "d")
                {
                    attack = controller.GetComponent(TestReflectionHelper.GetGameType("PlayerAttackComponent"));
                }
                yield return null;
            }

            Assert.IsNotNull(attack, "BaseStats가 없는 d 캐릭터가 스폰되지 않았습니다.");
            Assert.AreSame(GetProperty(characterData, "DefaultWeapon"), GetPrivateField(attack, "m_weapon"));
        }
        finally
        {
            SetField(characterData, "m_baseStats", originalStats);
            if (hadDeck)
            {
                PlayerPrefs.SetString(DeckSaveKey, originalDeck);
            }
            else
            {
                PlayerPrefs.DeleteKey(DeckSaveKey);
            }
            PlayerPrefs.Save();
        }
    }

    private Component CreatePlayer(object data, int attackDamage, bool isDragging, Transform[] firePoints, int bulletCountBonus = 0, float spreadAngleBonus = 0f)
    {
        var playerObject = Track(new GameObject("PlayerAttackComponentTests_Player"));
        playerObject.SetActive(false);
        var controllerType = TestReflectionHelper.GetGameType("PlayerCharacterController");
        var attackType = TestReflectionHelper.GetGameType("PlayerAttackComponent");
        var controller = playerObject.AddComponent(controllerType);
        var attack = playerObject.AddComponent(attackType);
        var stats = Activator.CreateInstance(TestReflectionHelper.GetGameType("PlayerStatsDTO"));
        SetField(stats, "ID", "player-test");
        SetField(stats, "AttackDamage", attackDamage);
        SetField(stats, "BulletCountBonus", bulletCountBonus);
        SetField(stats, "SpreadAngleBonus", spreadAngleBonus);
        SetField(stats, "CurrentHp", 100);
        SetField(stats, "IsActive", true);
        controllerType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance).Invoke(controller, new[] { stats });
        SetPrivateField(attack, "m_owner", controller);
        SetPrivateField(attack, "m_weapon", data);
        SetPrivateField(attack, "m_firePoints", firePoints);
        controllerType.GetProperty("IsDragging", BindingFlags.Public | BindingFlags.Instance).SetValue(controller, isDragging);
        if (GetPrivateField(attack, "m_behaviour") == null)
        {
            attackType.GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(attack, null);
        }
        playerObject.SetActive(true);
        return attack;
    }

    private Component CreateTarget(string name, Vector3 position)
    {
        var targetObject = Track(new GameObject(name));
        targetObject.tag = "Enemy";
        targetObject.transform.position = position;
        targetObject.AddComponent<BoxCollider2D>();
        return targetObject.AddComponent(m_enemyType);
    }

    private object CreateWeaponData(string behaviour, GameObject projectilePrefab)
    {
        var data = ScriptableObject.CreateInstance(TestReflectionHelper.GetGameType("WeaponDataSO"));
        m_createdAssets.Add(data);
        SetField(data, "Behaviour", Enum.Parse(TestReflectionHelper.GetGameType("WeaponBehaviourType"), behaviour));
        SetField(data, "ProjectilePrefab", projectilePrefab);
        return data;
    }

    private GameObject CreateBulletPrefab()
    {
        var prefab = Track(new GameObject("PlayerAttackComponentTests_BulletPrefab"));
        prefab.transform.position = new Vector3(100f, 100f, 0f);
        prefab.AddComponent<BoxCollider2D>().isTrigger = true;
        var body = prefab.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        prefab.AddComponent(TestReflectionHelper.GetGameType("BulletProjectile"));
        return prefab;
    }

    private List<Component> FindSpawnedBullets(Vector3 origin, string prefabName)
    {
        var result = new List<Component>();
        var bulletType = TestReflectionHelper.GetGameType("BulletProjectile");
        var bullets = UnityEngine.Object.FindObjectsByType(bulletType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < bullets.Length; i++)
        {
            var bullet = (Component)bullets[i];
            if (bullet.gameObject.activeSelf &&
                (bullet.gameObject.name == prefabName || bullet.gameObject.name == prefabName + "(Clone)") &&
                Vector3.Distance(bullet.transform.position, origin) < 0.1f)
            {
                result.Add(bullet);
            }
        }

        result.Sort((left, right) =>
            Mathf.DeltaAngle(0f, left.transform.eulerAngles.z).CompareTo(
                Mathf.DeltaAngle(0f, right.transform.eulerAngles.z)));
        return result;
    }

    private object GetProperty(object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
    }

    private object GetField(object target, string fieldName)
    {
        return target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    private void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).SetValue(target, value);
    }

    private void SetPrivateField(object target, string fieldName, object value)
    {
        target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    private object GetPrivateField(object target, string fieldName)
    {
        return target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    private void SetField(object target, string fieldName, object value)
    {
        target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    private GameObject Track(GameObject gameObject)
    {
        m_createdObjects.Add(gameObject);
        return gameObject;
    }

    private bool HasDamageRecord(string ownerID, int damage)
    {
        for (int i = 0; i < m_damageRecords.Count; i++)
        {
            if (m_damageOwners[i] == ownerID && m_damageRecords[i] == damage)
            {
                return true;
            }
        }

        return false;
    }
}
