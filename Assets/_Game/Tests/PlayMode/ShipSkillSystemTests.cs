using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System;

public class ShipSkillSystemTests
{
    private GameObject m_testStage;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        m_testStage = new GameObject("TestStage_ShipSkill");
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UnityEngine.Object.Destroy(m_testStage);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC01_BattleHUDViewModel_ExecuteShipSkill_Fires_Event()
    {
        Type vmType = TestReflectionHelper.GetGameType("BattleHUDViewModel");
        Type dtoType = TestReflectionHelper.GetGameType("BattleProgressDTO");

        var viewModel = Activator.CreateInstance(vmType);
        var dto = Activator.CreateInstance(dtoType);
        vmType.GetProperty("BattleData")?.SetValue(viewModel, dto);

        int receivedIndex = -1;
        var eventInfo = vmType.GetEvent("OnShipSkillExecuted");
        Action<int> handler = (index) => { receivedIndex = index; };
        eventInfo?.AddEventHandler(viewModel, handler);

        vmType.GetMethod("ExecuteShipSkill")?.Invoke(viewModel, new object[] { 0 });

        Assert.AreEqual(0, receivedIndex, "ExecuteShipSkill(0) 호출 시 OnShipSkillExecuted 이벤트가 인덱스 0으로 발행되어야 합니다.");

        receivedIndex = -1;
        vmType.GetMethod("ExecuteShipSkill")?.Invoke(viewModel, new object[] { 3 });
        Assert.AreEqual(3, receivedIndex, "ExecuteShipSkill(3) 호출 시 OnShipSkillExecuted 이벤트가 인덱스 3으로 발행되어야 합니다.");

        eventInfo?.RemoveEventHandler(viewModel, handler);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC02_MasterShip_ExecuteSkill_Index_Routing()
    {
        Type masterType = TestReflectionHelper.GetGameType("MasterShip");
        if (masterType == null)
        {
            Assert.Inconclusive("MasterShip 타입을 찾을 수 없습니다.");
            yield break;
        }

        var shipGo = new GameObject("TestMasterShip");
        shipGo.transform.SetParent(m_testStage.transform);
        var masterShip = shipGo.AddComponent(masterType);

        var executeMethod = masterType.GetMethod("ExecuteSkill", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(executeMethod, "MasterShip에 ExecuteSkill(int) 메서드가 존재해야 합니다.");

        Assert.DoesNotThrow(() =>
        {
            executeMethod.Invoke(masterShip, new object[] { 99 });
        }, "존재하지 않는 스킬 인덱스를 호출해도 예외가 발생하지 않아야 합니다.");

        yield return null;
    }

    [UnityTest]
    public IEnumerator TC03_ShipSkillButton_Component_Index_And_LazyLoading()
    {
        Type buttonType = TestReflectionHelper.GetGameType("ShipSkillButton");
        if (buttonType == null)
        {
            Assert.Inconclusive("ShipSkillButton 타입을 찾을 수 없습니다.");
            yield break;
        }

        var buttonGo = new GameObject("TestSkillButton");
        buttonGo.transform.SetParent(m_testStage.transform);
        buttonGo.AddComponent<Button>();
        var skillButton = buttonGo.AddComponent(buttonType);

        SetPrivateField(skillButton, "m_skillIndex", 2);

        var indexProp = buttonType.GetProperty("SkillIndex");
        int skillIndex = (int)indexProp.GetValue(skillButton);
        Assert.AreEqual(2, skillIndex, "ShipSkillButton의 SkillIndex가 설정한 값(2)과 일치해야 합니다.");

        var buttonProp = buttonType.GetProperty("Button");
        var unityButton = buttonProp.GetValue(skillButton) as Button;
        Assert.IsNotNull(unityButton, "ShipSkillButton의 Button 프로퍼티가 지연 로딩을 통해 UnityEngine.UI.Button을 안전하게 반환해야 합니다.");

        yield return null;
    }

    [UnityTest]
    public IEnumerator TC04_ViewModel_Event_To_MasterShip_Binding_Pipeline()
    {
        Type vmType = TestReflectionHelper.GetGameType("BattleHUDViewModel");
        Type masterType = TestReflectionHelper.GetGameType("MasterShip");
        Type dtoType = TestReflectionHelper.GetGameType("BattleProgressDTO");

        if (vmType == null || masterType == null)
        {
            Assert.Inconclusive("BattleHUDViewModel 또는 MasterShip 타입을 찾을 수 없습니다.");
            yield break;
        }

        var viewModel = Activator.CreateInstance(vmType);
        var dto = Activator.CreateInstance(dtoType);
        vmType.GetProperty("BattleData")?.SetValue(viewModel, dto);

        var shipGo = new GameObject("TestMasterShip_Pipeline");
        shipGo.transform.SetParent(m_testStage.transform);
        var masterShip = shipGo.AddComponent(masterType);

        var executeMethod = masterType.GetMethod("ExecuteSkill", BindingFlags.Public | BindingFlags.Instance);
        var eventInfo = vmType.GetEvent("OnShipSkillExecuted");
        var delegateInstance = Delegate.CreateDelegate(eventInfo.EventHandlerType, masterShip, executeMethod);
        eventInfo.AddEventHandler(viewModel, delegateInstance);

        Assert.DoesNotThrow(() =>
        {
            vmType.GetMethod("ExecuteShipSkill")?.Invoke(viewModel, new object[] { 0 });
        }, "ViewModel -> MasterShip 이벤트 파이프라인이 예외 없이 동작해야 합니다.");

        eventInfo.RemoveEventHandler(viewModel, delegateInstance);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC05_BattleHUDViewModel_AddKill_And_LevelUp()
    {
        Type vmType = TestReflectionHelper.GetGameType("BattleHUDViewModel");
        Type dtoType = TestReflectionHelper.GetGameType("BattleProgressDTO");

        var viewModel = Activator.CreateInstance(vmType);
        var dto = Activator.CreateInstance(dtoType);
        vmType.GetProperty("BattleData")?.SetValue(viewModel, dto);

        int lastLevel = -1;
        var levelEvent = vmType.GetEvent("OnLevelChanged");
        Action<int> levelHandler = (lvl) => { lastLevel = lvl; };
        levelEvent?.AddEventHandler(viewModel, levelHandler);

        int killsForLevel1 = 5;
        for (int i = 0; i < killsForLevel1; i++)
        {
            vmType.GetMethod("AddKill")?.Invoke(viewModel, null);
        }

        Assert.AreEqual(2, lastLevel, "킬 5회 달성 시 레벨이 2로 상승해야 합니다 (표시 레벨 = 내부 레벨 + 1).");

        var currentLevel = dtoType.GetField("CurrentLevel", BindingFlags.Public | BindingFlags.Instance)?.GetValue(dto);
        Assert.AreEqual(1, currentLevel, "내부 레벨 데이터는 1이어야 합니다.");

        levelEvent?.RemoveEventHandler(viewModel, levelHandler);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC06_BattleHUDViewModel_SetWave_And_ToggleSpeed()
    {
        Type vmType = TestReflectionHelper.GetGameType("BattleHUDViewModel");
        Type dtoType = TestReflectionHelper.GetGameType("BattleProgressDTO");

        var viewModel = Activator.CreateInstance(vmType);
        var dto = Activator.CreateInstance(dtoType);
        vmType.GetProperty("BattleData")?.SetValue(viewModel, dto);

        int receivedWave = -1;
        var waveEvent = vmType.GetEvent("OnWaveChanged");
        Action<int> waveHandler = (w) => { receivedWave = w; };
        waveEvent?.AddEventHandler(viewModel, waveHandler);

        vmType.GetMethod("SetWave")?.Invoke(viewModel, new object[] { 5 });
        Assert.AreEqual(5, receivedWave, "SetWave(5) 호출 시 이벤트로 웨이브 5가 전달되어야 합니다.");

        float receivedSpeed = -1f;
        var speedEvent = vmType.GetEvent("OnBattleSpeedChanged");
        Action<float> speedHandler = (s) => { receivedSpeed = s; };
        speedEvent?.AddEventHandler(viewModel, speedHandler);

        vmType.GetMethod("ToggleBattleSpeed")?.Invoke(viewModel, null);
        Assert.AreEqual(1.5f, receivedSpeed, 0.01f, "ToggleBattleSpeed 호출 시 배속이 1.0 -> 1.5로 변경되어야 합니다.");

        waveEvent?.RemoveEventHandler(viewModel, waveHandler);
        speedEvent?.RemoveEventHandler(viewModel, speedHandler);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC07_MasterShipLogic_Damage_And_Destruction()
    {
        Type dtoType = TestReflectionHelper.GetGameType("MasterShipDTO");
        Type logicType = TestReflectionHelper.GetGameType("MasterShipLogic");

        if (dtoType == null || logicType == null)
        {
            Assert.Inconclusive("MasterShipDTO 또는 MasterShipLogic 타입을 찾을 수 없습니다.");
            yield break;
        }

        var shipDto = Activator.CreateInstance(dtoType);
        dtoType.GetField("MaxHp")?.SetValue(shipDto, 100);
        dtoType.GetField("CurrentHp")?.SetValue(shipDto, 100);
        dtoType.GetField("IsDestroyed")?.SetValue(shipDto, false);

        var logic = Activator.CreateInstance(logicType, new[] { shipDto });

        var damageMethod = logicType.GetMethod("OnDamaged");
        float ratio = (float)damageMethod.Invoke(logic, new object[] { 30 });
        Assert.AreEqual(0.7f, ratio, 0.01f, "30 데미지 후 HP 비율은 0.7이어야 합니다.");

        damageMethod.Invoke(logic, new object[] { 70 });
        bool isDestroyed = (bool)logicType.GetMethod("CheckIsDestroyed").Invoke(logic, null);
        Assert.IsTrue(isDestroyed, "HP가 0 이하가 되면 파괴 상태여야 합니다.");

        float afterDestroyRatio = (float)damageMethod.Invoke(logic, new object[] { 10 });
        Assert.AreEqual(0f, afterDestroyRatio, "파괴된 후 추가 데미지는 0f를 반환해야 합니다.");

        yield return null;
    }

    private void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        field?.SetValue(target, value);
    }
}
