using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class UserDataSaveTests
{
    private const string SAVE_KEY = "SpaceCaptain.Deck";

    private Type m_userDataType;
    private object m_userData;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        m_userDataType = TestReflectionHelper.GetGameType("UserDataSO");
        Assert.IsNotNull(m_userDataType);
        m_userData = ScriptableObject.CreateInstance(m_userDataType);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        UnityEngine.Object.DestroyImmediate((UnityEngine.Object)m_userData);
    }

    [Test]
    public void SaveData_저장후_LoadData하면_덱이_복원된다()
    {
        DeckCharacters.Clear();
        DeckCharacters.Add("a");
        DeckCharacters.Add("b");
        InvokeUserDataMethod("SaveData");

        DeckCharacters.Clear();
        Assert.AreEqual(0, DeckCharacters.Count);

        InvokeUserDataMethod("LoadData");

        Assert.AreEqual(2, DeckCharacters.Count);
        Assert.AreEqual("a", DeckCharacters[0]);
        Assert.AreEqual("b", DeckCharacters[1]);
    }

    [Test]
    public void LoadData는_LobbyData_인스턴스_참조를_유지한다()
    {
        object before = LobbyData;

        DeckCharacters.Clear();
        DeckCharacters.Add("a");
        InvokeUserDataMethod("SaveData");

        DeckCharacters.Clear();
        InvokeUserDataMethod("LoadData");

        Assert.AreSame(before, LobbyData, "새 인스턴스를 대입하면 ViewModel이 든 참조가 끊긴다");
        Assert.AreEqual(1, DeckCharacters.Count);
    }

    [Test]
    public void 저장본이_없으면_LoadData는_기존값을_건드리지_않는다()
    {
        SetLobbyDataField("Nickname", "원본");

        InvokeUserDataMethod("LoadData");

        Assert.AreEqual("원본", GetLobbyDataField("Nickname"));
    }

    [Test]
    public void SaveData는_편성_외의_값을_저장하지_않는다()
    {
        // LobbyData 전체를 저장하면 회복 로직이 없는 스태미나가 0으로 굳어
        // 전투 시작이 영구히 막힌다. 저장 대상은 덱뿐이어야 한다.
        DeckCharacters.Clear();
        DeckCharacters.Add("a");
        SetLobbyDataField("CurrentStamina", 0);
        SetLobbyDataField("Gold", 12345);
        InvokeUserDataMethod("SaveData");

        SetLobbyDataField("CurrentStamina", 20);
        SetLobbyDataField("Gold", 0);
        InvokeUserDataMethod("LoadData");

        Assert.AreEqual(20, GetLobbyDataField("CurrentStamina"), "스태미나가 저장본에 덮여쓰였다");
        Assert.AreEqual(0, GetLobbyDataField("Gold"), "골드가 저장본에 덮여쓰였다");
        Assert.AreEqual(1, DeckCharacters.Count, "덱은 복원되어야 한다");
    }

    [Test]
    public void 빈_덱을_저장해도_LoadData가_깨지지_않는다()
    {
        DeckCharacters.Clear();
        InvokeUserDataMethod("SaveData");

        DeckCharacters.Add("a");
        InvokeUserDataMethod("LoadData");

        Assert.AreEqual(1, DeckCharacters.Count, "빈 저장본은 무시되고 기존 덱이 남는다");
    }

    private object LobbyData => m_userDataType.GetProperty("LobbyData").GetValue(m_userData);

    private List<string> DeckCharacters => (List<string>)GetLobbyDataField("DeckCharacters");

    private object GetLobbyDataField(string fieldName)
    {
        return LobbyData.GetType().GetField(fieldName).GetValue(LobbyData);
    }

    private void SetLobbyDataField(string fieldName, object value)
    {
        LobbyData.GetType().GetField(fieldName).SetValue(LobbyData, value);
    }

    private void InvokeUserDataMethod(string methodName)
    {
        m_userDataType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public).Invoke(m_userData, null);
    }
}
