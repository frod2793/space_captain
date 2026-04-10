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
        for(int i=0; i<2; i++)
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

        Type charType = TestReflectionHelper.GetGameType("PlayerCharacterController");
        Type statsType = TestReflectionHelper.GetGameType("PlayerStatsDTO");

        for (int i = 0; i < 3; i++)
        {
            var charGo = new GameObject($"Char_{i}");
            charGo.transform.SetParent(m_testStage.transform);
            
            var spriteRenderer = charGo.AddComponent<SpriteRenderer>();
            charGo.AddComponent<BoxCollider2D>();
            
            var character = charGo.AddComponent(charType);
            
            SetPrivateField(character, "m_spriteRenderer", spriteRenderer);
            
            // 초기 스탯 설정
            var stats = Activator.CreateInstance(statsType);
            SetPrivateField(stats, "MaxHp", 100);
            SetPrivateField(stats, "CurrentHp", 100);
            SetPrivateField(stats, "BaseRange", 10f);
            SetPrivateField(stats, "BaseBulletScale", 1f);
            
            charType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)?.Invoke(character, new[] { stats });
            m_characters.Add(character);
        }

        // SwapManager 리스트에 캐릭터 등록
        var charactersField = managerType.GetField("m_characters", BindingFlags.NonPublic | BindingFlags.Instance);
        var listType = typeof(List<>).MakeGenericType(charType);
        var charList = Activator.CreateInstance(listType);
        var addMethod = listType.GetMethod("Add");
        foreach (var c in m_characters) addMethod.Invoke(charList, new[] { c });
        charactersField.SetValue(m_swapManager, charList);

        // 초기화 실행
        managerType.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(m_swapManager, null);

        yield return new WaitForSeconds(0.1f); 
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UnityEngine.Object.Destroy(m_testStage);
        yield return null;
    }

    #region 기능 테스트 케이스
    [UnityTest]
    public IEnumerator TC01_Swap_Logic_Verification()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        
        var swapMethod = managerType.GetMethod("SwitchToCharacter", BindingFlags.Public | BindingFlags.Instance);
        swapMethod?.Invoke(m_swapManager, new[] { m_characters[1] });

        yield return new WaitForSeconds(0.8f); 

        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        Assert.AreEqual(m_characters[1], activeChar, "스왑 후 활성 캐릭터가 일치하지 않습니다.");
    }

    [UnityTest]
    public IEnumerator TC02_Standby_Stat_Correction_Check()
    {
        // 0번(Active)과 1번(Standby) 캐릭터의 IsActive 상태 검증
        Type charType = TestReflectionHelper.GetGameType("PlayerCharacterController");
        
        bool activeCharState = (bool)charType.GetProperty("IsActive")?.GetValue(m_characters[0]);
        bool standbyCharState = (bool)charType.GetProperty("IsActive")?.GetValue(m_characters[1]);

        // 스탯 보정(사거리 0.5배, 데미지 등)은 PlayerAttackComponent의 내부 지역 변수를 통해 동적으로 계산됩니다.
        // 이 보정의 핵심 트리거가 되는 IsActive 플래그가 Standby 캐릭터에서 명확히 분리되는지 검증합니다.
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

        charType.GetMethod("TakeDamage", BindingFlags.Public | BindingFlags.Instance)?.Invoke(activeChar, new object[] { 100 });

        yield return new WaitForSeconds(0.5f); 

        var newActive = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        Assert.AreNotEqual(activeChar, newActive, "사망 후 활성 캐릭터가 교체되어야 합니다.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC04_Swap_Cooldown_Timestamp_Check()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        
        var swapMethod = managerType.GetMethod("SwitchToCharacter", BindingFlags.Public | BindingFlags.Instance);
        
        // 첫 번째 스왑 시도
        swapMethod?.Invoke(m_swapManager, new[] { m_characters[1] });
        yield return null; 

        var property = managerType.GetProperty("CurrentSwapCooldown", BindingFlags.Public | BindingFlags.Instance);
        float currentCooldown = (float)property.GetValue(m_swapManager);
        
        Assert.IsTrue(currentCooldown > 0, "스왑 애니메이션 직후 CurrentSwapCooldown은 0보다 커야 합니다.");

        var activeCharAfter1stSwap = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        
        // 쿨다운 중 바로 두 번째 스왑 시도 (무시되어야 함)
        swapMethod?.Invoke(m_swapManager, new[] { m_characters[2] });
        yield return null;

        var activeCharAfter2ndSwap = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);
        
        Assert.AreEqual(activeCharAfter1stSwap, activeCharAfter2ndSwap, "쿨다운 중에는 스왑이 실행되지 않아야 합니다.");
    }

    [UnityTest]
    public IEnumerator TC05_Attack_Component_Caching_Check()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        Type attackType = TestReflectionHelper.GetGameType("PlayerAttackComponent");

        // PlayerSwapManager의 Start에서 각 캐릭터별 어택 컴포넌트 추가 및 캐싱이 진행됨
        var cacheField = managerType.GetField("m_attackComponentCache", BindingFlags.NonPublic | BindingFlags.Instance);
        var cacheDict = cacheField?.GetValue(m_swapManager) as IDictionary;

        Assert.IsNotNull(cacheDict, "Attack 컴포넌트 딕셔너리가 초기화되어야 합니다.");
        Assert.AreEqual(3, cacheDict.Count, "모든 캐릭터(3명)의 Attack 컴포넌트가 딕셔너리에 올바르게 캐싱되어야 합니다.");

        yield return null;
    }

    [UnityTest]
    public IEnumerator TC06_Swap_Animation_State_Check()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);

        var swapMethod = managerType.GetMethod("SwitchToCharacter", BindingFlags.Public | BindingFlags.Instance);
        
        // 스왑 시작
        swapMethod?.Invoke(m_swapManager, new[] { m_characters[1] });
        
        // 한 프레임 대기 후 상태 확인 (Tremor fix: oldActive는 비활성화되어야 함)
        yield return null; 

        Type charType = TestReflectionHelper.GetGameType("PlayerCharacterController");
        GameObject oldGo = ((MonoBehaviour)activeChar).gameObject;
        
        Assert.IsFalse(oldGo.activeSelf, "스왑 애니메이션 도중 구 활성 캐릭터는 비활성화(떨림 방지) 상태여야 합니다.");

        // 스왑 끝날때까지 대기 (m_swapDuration은 0.5초 기준이므로 0.6초 대기)
        yield return new WaitForSeconds(0.6f);

        GameObject newActiveGo = ((MonoBehaviour)managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager)).gameObject;
        
        Assert.IsTrue(newActiveGo.activeSelf, "스왑 애니메이션 완료 후 새로운 캐릭터는 활성화되어야 합니다.");
    }
    #endregion

    #region 리플렉션 유틸리티
    private void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        field?.SetValue(target, value);
    }
    #endregion
}
