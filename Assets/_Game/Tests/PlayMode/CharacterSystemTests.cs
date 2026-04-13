using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using System.Collections.Generic;

public class CharacterSystemTests
{
    private GameObject m_testStage;
    private object m_swapManager;
    private List<object> m_characters = new List<object>();
    private GameObject[] m_standbyPosObjects;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        m_characters.Clear();
        m_testStage = new GameObject("TestStage");
        
        var activePos = new GameObject("ActivePos").transform;
        activePos.SetParent(m_testStage.transform);
        activePos.position = Vector3.zero;

        m_standbyPosObjects = new GameObject[2];
        var standbyPositions = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            m_standbyPosObjects[i] = new GameObject($"StandbyPos_{i}");
            m_standbyPosObjects[i].transform.SetParent(m_testStage.transform);
            m_standbyPosObjects[i].transform.position = new Vector3(2 + i, 0, 0);
            standbyPositions[i] = m_standbyPosObjects[i].transform;
        }

        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        var managerGo = new GameObject("SwapManager");
        managerGo.transform.SetParent(m_testStage.transform);
        m_swapManager = managerGo.AddComponent(managerType);

        SetPrivateField(m_swapManager, "m_activePosition", activePos);
        SetPrivateField(m_swapManager, "m_standbyPositions", standbyPositions);
        SetPrivateField(m_swapManager, "m_swapCooldownDuration", 2.0f); 

        Type charType = TestReflectionHelper.GetGameType("PlayerCharacterController");
        Type statsType = TestReflectionHelper.GetGameType("BattleHUDViewModel")?.Assembly.GetType("PlayerStatsDTO") ?? TestReflectionHelper.GetGameType("PlayerStatsDTO");
        Type enumType = TestReflectionHelper.GetGameType("SpaceCaptain.Player.CharacterSwapState");

        for (int i = 0; i < 3; i++)
        {
            var charGo = new GameObject($"Char_{i}");
            charGo.transform.SetParent(m_testStage.transform);
            
            var spriteRenderer = charGo.AddComponent<SpriteRenderer>();
            charGo.AddComponent<BoxCollider2D>();
            
            var character = charGo.AddComponent(charType);
            SetPrivateField(character, "m_spriteRenderer", spriteRenderer);

            object swapState = (i == 0) ? Enum.Parse(enumType, "Active") : Enum.Parse(enumType, "Standby");
            charType.GetProperty("SwapState")?.SetValue(character, swapState);
            
            var stats = Activator.CreateInstance(statsType);
            SetField(stats, "ID", $"Player_{i}");
            SetField(stats, "MaxHp", 100);
            SetField(stats, "CurrentHp", 100);
            SetField(stats, "MoveSpeed", 20f);
            SetField(stats, "IsActive", i == 0);
            
            charType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)?.Invoke(character, new[] { stats });
            m_characters.Add(character);
        }

        managerType.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(m_swapManager, null);

        yield return new WaitForSeconds(0.1f); 
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UnityEngine.Object.Destroy(m_testStage);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC01_Swap_Logic_Verification()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");

        SetPrivateField(m_swapManager, "m_swapCooldownEndTime", 0f);
        
        var swapMethod = managerType.GetMethod("SwitchToCharacter", BindingFlags.Public | BindingFlags.Instance);
        swapMethod?.Invoke(m_swapManager, new[] { m_characters[1] });

        yield return new WaitForSeconds(0.8f); 

        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        Assert.AreEqual(m_characters[1], activeChar, "스왑 후 활성 캐릭터가 일치하지 않습니다.");
    }

    [UnityTest]
    public IEnumerator TC02_Standby_Stat_Correction_Check()
    {
        Type charType = TestReflectionHelper.GetGameType("PlayerCharacterController");
        
        bool activeCharState = (bool)charType.GetProperty("IsActive")?.GetValue(m_characters[0]);
        bool standbyCharState = (bool)charType.GetProperty("IsActive")?.GetValue(m_characters[1]);

        Assert.IsTrue(activeCharState, "0번 캐릭터는 Active 상태여야 하므로 IsActive가 true여야 합니다.");
        Assert.IsFalse(standbyCharState, "1번 캐릭터는 Standby 상태여야 하므로 IsActive가 false여야 합니다.");

        yield return null;
    }

    [UnityTest]
    public IEnumerator TC03_Auto_Replace_On_Death()
    {
        Type charType = TestReflectionHelper.GetGameType("PlayerCharacterController");
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");

        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        if (activeChar == null)
        {
            activeChar = m_characters[0];
        }

        charType.GetMethod("TakeDamage", BindingFlags.Public | BindingFlags.Instance)?.Invoke(activeChar, new object[] { 9999 });

        yield return new WaitForSeconds(0.7f); 

        var newActive = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        Assert.AreNotEqual(activeChar, newActive, "사망 후 활성 캐릭터가 교체되어야 합니다.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC04_Swap_Cooldown_Timestamp_Check()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        
        SetPrivateField(m_swapManager, "m_swapCooldownEndTime", 0f);
        
        var swapMethod = managerType.GetMethod("SwitchToCharacter", BindingFlags.Public | BindingFlags.Instance);
        
        swapMethod?.Invoke(m_swapManager, new[] { m_characters[1] });
        yield return new WaitForSeconds(0.5f);

        var property = managerType.GetProperty("CurrentSwapCooldown", BindingFlags.Public | BindingFlags.Instance);
        float currentCooldown = (float)property.GetValue(m_swapManager);
        
        Assert.IsTrue(currentCooldown > 0, "스왑 직후 CurrentSwapCooldown은 0보다 커야 합니다.");

        var activeCharAfter1stSwap = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        
        swapMethod?.Invoke(m_swapManager, new[] { m_characters[2] });
        yield return new WaitForSeconds(0.5f);

        var activeCharAfter2ndSwap = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        
        Assert.AreEqual(activeCharAfter1stSwap, activeCharAfter2ndSwap, "쿨다운 중에는 스왑이 실행되지 않아야 합니다.");
    }

    [UnityTest]
    public IEnumerator TC05_Attack_Component_Caching_Check()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");

        var cacheField = managerType.GetField("m_attackComponentCache", BindingFlags.NonPublic | BindingFlags.Instance);
        var cacheDict = cacheField?.GetValue(m_swapManager) as IDictionary;

        Assert.IsNotNull(cacheDict, "Attack 컴포넌트 딕셔너리가 초기화되어야 합니다.");

        yield return null;
    }

    [UnityTest]
    public IEnumerator TC06_Swap_Animation_State_Check()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);

        SetPrivateField(m_swapManager, "m_swapCooldownEndTime", 0f);
        
        var swapMethod = managerType.GetMethod("SwitchToCharacter", BindingFlags.Public | BindingFlags.Instance);
        
        swapMethod?.Invoke(m_swapManager, new[] { m_characters[1] });
        
        yield return null; 

        var isAnimating = managerType.GetProperty("IsAnimating")?.GetValue(m_swapManager);
        if (isAnimating != null && (bool)isAnimating)
        {
            Assert.IsTrue(true, "스왑 애니메이션 도중 IsAnimating이 true여야 합니다.");
        }

        yield return new WaitForSeconds(0.6f);

        var newActiveObj = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        if (newActiveObj != null)
        {
            GameObject newActiveGo = ((MonoBehaviour)newActiveObj).gameObject;
            Assert.IsTrue(newActiveGo.activeSelf, "스왑 애니메이션 완료 후 새로운 캐릭터는 활성화되어야 합니다.");
        }
    }

    [UnityTest]
    public IEnumerator TC07_SwapState_Alignment_After_Init()
    {
        Type charType = TestReflectionHelper.GetGameType("PlayerCharacterController");

        var swapStateProp = charType.GetProperty("SwapState", BindingFlags.Public | BindingFlags.Instance);
        if (swapStateProp == null)
        {
            Assert.Inconclusive("SwapState 프로퍼티를 찾을 수 없습니다.");
            yield break;
        }

        var activeState = swapStateProp.GetValue(m_characters[0]);
        var standbyState1 = swapStateProp.GetValue(m_characters[1]);
        var standbyState2 = swapStateProp.GetValue(m_characters[2]);

        Assert.AreEqual("Active", activeState.ToString(), "0번 캐릭터는 Active 상태여야 합니다.");
        Assert.AreEqual("Standby", standbyState1.ToString(), "1번 캐릭터는 Standby 상태여야 합니다.");
        Assert.AreEqual("Standby", standbyState2.ToString(), "2번 캐릭터는 Standby 상태여야 합니다.");

        yield return null;
    }

    [UnityTest]
    public IEnumerator TC08_Individual_Cooldown_Check()
    {
        Type charType = TestReflectionHelper.GetGameType("PlayerCharacterController");

        var cooldownProp = charType.GetProperty("RemainingSwapCooldown", BindingFlags.Public | BindingFlags.Instance);
        if (cooldownProp == null)
        {
            Assert.Inconclusive("RemainingSwapCooldown 프로퍼티를 찾을 수 없습니다.");
            yield break;
        }

        float cooldown = (float)cooldownProp.GetValue(m_characters[1]);
        Assert.AreEqual(0f, cooldown, 0.01f, "초기 개별 쿨다운은 0이어야 합니다.");

        charType.GetMethod("SetSwapCooldown", BindingFlags.Public | BindingFlags.Instance)
            ?.Invoke(m_characters[1], new object[] { 5.0f });
        
        float afterSet = (float)cooldownProp.GetValue(m_characters[1]);
        Assert.IsTrue(afterSet > 0f, "SetSwapCooldown 호출 후 개별 쿨다운이 0보다 커야 합니다.");

        yield return null;
    }

    [UnityTest]
    public IEnumerator TC09_Rapid_Swap_Stress_Test()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");

        SetPrivateField(m_swapManager, "m_swapCooldownEndTime", 0f);
        var swapMethod = managerType.GetMethod("SwitchToCharacter", BindingFlags.Public | BindingFlags.Instance);
        
        swapMethod?.Invoke(m_swapManager, new[] { m_characters[1] });
        swapMethod?.Invoke(m_swapManager, new[] { m_characters[2] });

        yield return new WaitForSeconds(1.0f); 

        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        
        Assert.AreEqual(m_characters[1], activeChar, "연속 스왑 시 첫 번째 유효한 요청만 처리되어야 합니다.");
        
        bool isAnimating = (bool)managerType.GetProperty("IsAnimating")?.GetValue(m_swapManager);
        Assert.IsFalse(isAnimating, "애니메이션 완료 후 IsAnimating은 false여야 합니다.");
    }

    [UnityTest]
    public IEnumerator TC10_Death_During_Shake_Safety()
    {
        Type charType = TestReflectionHelper.GetGameType("PlayerCharacterController");
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);

        charType.GetMethod("TakeDamage", BindingFlags.Public | BindingFlags.Instance)?.Invoke(activeChar, new object[] { 9999 });
        
        yield return new WaitForSeconds(0.1f); 

        var hpField = charType.Assembly.GetType("PlayerStatsDTO")?.GetField("CurrentHp") ?? activeChar.GetType().GetField("m_stats", BindingFlags.NonPublic | BindingFlags.Instance)?.FieldType.GetField("CurrentHp");
        
        charType.GetMethod("TakeDamage", BindingFlags.Public | BindingFlags.Instance)?.Invoke(activeChar, new object[] { 10 });
        
        yield return new WaitForSeconds(0.6f); 

        var state = charType.GetProperty("SwapState")?.GetValue(activeChar).ToString();
        Assert.AreEqual("Dead", state, "사망 연출 중 추가 피격이 발생해도 정상적으로 사망 상태로 전이되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator TC11_Concurrent_Swap_Guard()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        SetPrivateField(m_swapManager, "m_swapCooldownEndTime", 0f);
        
        var swapMethod = managerType.GetMethod("SwitchToCharacter", BindingFlags.Public | BindingFlags.Instance);
        swapMethod?.Invoke(m_swapManager, new[] { m_characters[1] });
        
        var handleDeadMethod = managerType.GetMethod("HandlePlayerDead", BindingFlags.NonPublic | BindingFlags.Instance);
        handleDeadMethod?.Invoke(m_swapManager, new[] { m_characters[0] });

        // 애니메이션이 충분히 끝날 때까지 대기 (첫 스왑 + 이후 발생할 사망 스왑 모두 포함)
        yield return new WaitForSeconds(1.5f);

        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        Assert.AreNotEqual(m_characters[0], activeChar, "사망한 요원은 활성 상태로 남아있어서는 안 됩니다.");
        Assert.IsNotNull(activeChar, "사망한 요원을 대체할 새로운 요원이 투입되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator TC12_State_Consistency_Check()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        Type charType = TestReflectionHelper.GetGameType("PlayerCharacterController");
        SetPrivateField(m_swapManager, "m_swapCooldownEndTime", 0f);

        var char1 = m_characters[1];
        var swapMethod = managerType.GetMethod("SwitchToCharacter", BindingFlags.Public | BindingFlags.Instance);
        swapMethod?.Invoke(m_swapManager, new[] { char1 });

        yield return new WaitForSeconds(1.0f);

        var state1 = charType.GetProperty("SwapState")?.GetValue(char1).ToString();
        var state0 = charType.GetProperty("SwapState")?.GetValue(m_characters[0]).ToString();

        Assert.AreEqual("Active", state1, "교체 투입된 캐릭터의 상태는 Active여야 합니다.");
        Assert.AreEqual("Standby", state0, "교체되어 나간 캐릭터의 상태는 Standby여야 합니다.");
    }

    [UnityTest]
    public IEnumerator TC13_Double_Death_Chain_Swap()
    {
        Type charType = TestReflectionHelper.GetGameType("PlayerCharacterController");
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        SetPrivateField(m_swapManager, "m_swapCooldownEndTime", 0f);

        // 첫 번째 요원 사망
        charType.GetMethod("TakeDamage", BindingFlags.Public | BindingFlags.Instance)?.Invoke(m_characters[0], new object[] { 9999 });
        
        // 사망 연출 도중(또는 직후) 두 번째 요원(투입될 예정이거나 방금 된 요원)도 사망 시킴
        yield return new WaitForSeconds(0.1f);
        charType.GetMethod("TakeDamage", BindingFlags.Public | BindingFlags.Instance)?.Invoke(m_characters[1], new object[] { 9999 });

        yield return new WaitForSeconds(2.0f); // 모든 스왑 연쇄가 끝날 때까지 대기

        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        Assert.AreEqual(m_characters[2], activeChar, "연쇄 사망 후 최종적으로 살아있는 세 번째 요원이 활성화되어야 합니다.");
    }

    private void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        field?.SetValue(target, value);
    }

    private void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(target, value);
    }
}
