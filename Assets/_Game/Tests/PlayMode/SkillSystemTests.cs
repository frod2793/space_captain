using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using System.Collections.Generic;

public class SkillSystemTests
{
    private GameObject m_testStage;
    private object m_swapManager;
    private List<object> m_characters = new List<object>();
    private GameObject[] m_standbyPosObjects;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        m_characters.Clear();
        m_testStage = new GameObject("TestStage_Skill");
        
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
    public IEnumerator TC01_Input_Lock_During_Skill()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);

        Type skillType = TestReflectionHelper.GetGameType("ActiveSkill");
        var skillGo = new GameObject("TestSkill");
        skillGo.transform.SetParent(m_testStage.transform);
        var skillComponent = skillGo.AddComponent(skillType);
        
        SetPrivateField(skillComponent, "m_performanceDuration", 0.1f);  
        SetPrivateField(skillComponent, "m_cooldownTime", 2f);
        
        Type laserType = TestReflectionHelper.GetGameType("SkillLaser");
        if (laserType != null)
        {
            var dummyPrefabGo = new GameObject("DummyPrefab");
            dummyPrefabGo.transform.SetParent(m_testStage.transform);
            var dummyPrefab = dummyPrefabGo.AddComponent(laserType);
            
            var dummyRendererGo = new GameObject("DummyRenderer");
            dummyRendererGo.transform.SetParent(dummyPrefabGo.transform);
            var dummySprite = dummyRendererGo.AddComponent<SpriteRenderer>();
            SetPrivateField(dummyPrefab, "m_laserSprite", dummySprite);

            SetPrivateField(skillComponent, "m_skillEffectPrefab", dummyPrefab);
        }

        skillType.GetMethod("Initialize")?.Invoke(skillComponent, new[] { activeChar });
        SetPrivateField(activeChar, "m_activeSkill", skillComponent);

        SetPrivateField(m_swapManager, "m_swapCooldownEndTime", 0f);

        var lockField = managerType.GetField("m_isInputLocked", BindingFlags.NonPublic | BindingFlags.Instance);
        bool initialLock = (bool)lockField.GetValue(m_swapManager);
        Assert.IsFalse(initialLock, "스킬 실행 전에는 입력이 잠겨있지 않아야 합니다.");

        managerType.GetMethod("ExecuteCharacterActionAsync", BindingFlags.Public | BindingFlags.Instance)?.Invoke(m_swapManager, new[] { activeChar });

        bool lockedDuringSkill = (bool)lockField.GetValue(m_swapManager);
        Assert.IsTrue(lockedDuringSkill, "스킬 컷신 및 로직 수행 중에는 입력이 잠겨야 합니다.");

        yield return new WaitForSeconds(0.3f); 

        bool lockedAfterSkill = (bool)lockField.GetValue(m_swapManager);
        Assert.IsFalse(lockedAfterSkill, "스킬 애니메이션 종료 후 입력 잠금이 정상적으로 해제되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator TC02_CutIn_Skipped_When_No_Skill_Effect()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);

        Type skillType = TestReflectionHelper.GetGameType("ActiveSkill");
        var skillGo = new GameObject("TestSkillNoEffect");
        skillGo.transform.SetParent(m_testStage.transform);
        var skillComponent = skillGo.AddComponent(skillType);
        
        SetPrivateField(skillComponent, "m_performanceDuration", 1.0f);
        SetPrivateField(skillComponent, "m_cooldownTime", 2f);
        
        skillType.GetMethod("Initialize")?.Invoke(skillComponent, new[] { activeChar });
        SetPrivateField(activeChar, "m_activeSkill", skillComponent);

        SetPrivateField(m_swapManager, "m_swapCooldownEndTime", 0f);

        float initialTimeScale = Time.timeScale;
        Assert.AreNotEqual(initialTimeScale, 0f); 

        managerType.GetMethod("ExecuteCharacterActionAsync", BindingFlags.Public | BindingFlags.Instance)?.Invoke(m_swapManager, new[] { activeChar });

        yield return null; 

        Assert.AreEqual(initialTimeScale, Time.timeScale, "이펙트 프리팹이 없을 경우 Time.timeScale이 정지되지 않아야 합니다.");
        
        var lockField = managerType.GetField("m_isInputLocked", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isLocked = (bool)lockField.GetValue(m_swapManager);
        Assert.IsFalse(isLocked, "컷인이 통과(Skip)되었으므로 입력 잠금이 즉시 해제되어야 합니다.");
    }

    [UnityTest]
    public IEnumerator TC03_Skill_Cooldown_Prevents_Re_Execute()
    {
        Type managerType = TestReflectionHelper.GetGameType("PlayerSwapManager");
        var activeChar = managerType.GetProperty("ActiveCharacter")?.GetValue(m_swapManager);

        Type skillType = TestReflectionHelper.GetGameType("ActiveSkill");
        var skillGo = new GameObject("TestSkillCooldown");
        skillGo.transform.SetParent(m_testStage.transform);
        var skillComponent = skillGo.AddComponent(skillType);
        
        SetPrivateField(skillComponent, "m_performanceDuration", 0.05f);
        SetPrivateField(skillComponent, "m_cooldownTime", 999f);
        
        skillType.GetMethod("Initialize")?.Invoke(skillComponent, new[] { activeChar });
        SetPrivateField(activeChar, "m_activeSkill", skillComponent);

        SetPrivateField(m_swapManager, "m_swapCooldownEndTime", 0f);

        managerType.GetMethod("ExecuteCharacterActionAsync", BindingFlags.Public | BindingFlags.Instance)
            ?.Invoke(m_swapManager, new[] { activeChar });

        yield return new WaitForSeconds(0.2f);

        var isReadyProp = skillType.GetProperty("IsReady", BindingFlags.Public | BindingFlags.Instance);
        bool isReady = (bool)isReadyProp.GetValue(skillComponent);
        Assert.IsFalse(isReady, "스킬 실행 후 쿨다운 중에는 IsReady가 false여야 합니다.");
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
