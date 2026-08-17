using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ButtonEventSystemTests
{
    private class MockLobbyViewModel : ILobbyViewModel
    {
        public bool StartBattleCalled { get; private set; }
        public bool OpenSettingsCalled { get; private set; }
        public bool OpenProfileCalled { get; private set; }
        public StageDifficulty? SelectedDifficultyParam { get; private set; }

        public string Nickname => "TestUser";
        public int Level => 10;
        public int Gold => 5000;
        public int Diamond => 100;
        public int CurrentStamina => 20;
        public int MaxStamina => 50;
        public int RequiredStamina => 1;
        public string CurrentMapName => "TestMap";
        public int MaxWaveReached => 15;
        public StageDifficulty SelectedDifficulty => StageDifficulty.Normal;
        public string DisplayStageName => "TestMap (일반)";

        public event Action OnDataChanged;
        public event Action OnProfileOpenRequested;
        public event Action OnPartyOpenRequested;

        public void StartBattle()
        {
            StartBattleCalled = true;
        }

        public void OpenSettings()
        {
            OpenSettingsCalled = true;
        }

        public void OpenProfile()
        {
            OpenProfileCalled = true;
        }

        public void OpenParty()
        {
            OnPartyOpenRequested?.Invoke();
        }

        public void SelectDifficulty(StageDifficulty difficulty)
        {
            SelectedDifficultyParam = difficulty;
        }
    }

    private class MockUserProfileViewModel : IUserProfileViewModel
    {
        public bool RequestCloseCalled { get; private set; }

        public string UID => "User_12345";
        public string ProfileIconID => "Icon_01";

        public event Action OnCloseRequested;

        public void RequestClose()
        {
            RequestCloseCalled = true;
        }
    }

    private GameObject m_testGo;

    [SetUp]
    public void SetUp()
    {
        m_testGo = new GameObject("ButtonTestStage");
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(m_testGo);
    }

    [Test]
    public void TC14_LobbyView_Button_Events_Verifying()
    {
        Debug.Log("[LobbyView 테스트] 입력: 로비의 각종 버튼(배틀시작, 설정, 프로필, 난이도 선택) 클릭 이벤트 발생");
        Debug.Log("[LobbyView 테스트] 예상 출력: 뷰모델의 대응 메서드(StartBattle, OpenSettings, OpenProfile, SelectDifficulty) 정상 호출");

        var viewGo = new GameObject("LobbyView");
        viewGo.transform.SetParent(m_testGo.transform);
        var lobbyView = viewGo.AddComponent<LobbyView>();

        var nicknameText = new GameObject().AddComponent<TextMeshProUGUI>();
        nicknameText.transform.SetParent(viewGo.transform);
        var levelText = new GameObject().AddComponent<TextMeshProUGUI>();
        levelText.transform.SetParent(viewGo.transform);
        var goldText = new GameObject().AddComponent<TextMeshProUGUI>();
        goldText.transform.SetParent(viewGo.transform);
        var diamondText = new GameObject().AddComponent<TextMeshProUGUI>();
        diamondText.transform.SetParent(viewGo.transform);
        var staminaText = new GameObject().AddComponent<TextMeshProUGUI>();
        staminaText.transform.SetParent(viewGo.transform);
        var mapNameText = new GameObject().AddComponent<TextMeshProUGUI>();
        mapNameText.transform.SetParent(viewGo.transform);
        var maxWaveText = new GameObject().AddComponent<TextMeshProUGUI>();
        maxWaveText.transform.SetParent(viewGo.transform);

        var battleStartBtn = new GameObject().AddComponent<Button>();
        battleStartBtn.transform.SetParent(viewGo.transform);
        var settingsBtn = new GameObject().AddComponent<Button>();
        settingsBtn.transform.SetParent(viewGo.transform);
        var profileBtn = new GameObject().AddComponent<Button>();
        profileBtn.transform.SetParent(viewGo.transform);
        var normalDiffBtn = new GameObject().AddComponent<Button>();
        normalDiffBtn.transform.SetParent(viewGo.transform);
        var eliteDiffBtn = new GameObject().AddComponent<Button>();
        eliteDiffBtn.transform.SetParent(viewGo.transform);

        SetPrivateField(lobbyView, "m_nicknameText", nicknameText);
        SetPrivateField(lobbyView, "m_levelText", levelText);
        SetPrivateField(lobbyView, "m_goldText", goldText);
        SetPrivateField(lobbyView, "m_diamondText", diamondText);
        SetPrivateField(lobbyView, "m_staminaText", staminaText);
        SetPrivateField(lobbyView, "m_mapNameText", mapNameText);
        SetPrivateField(lobbyView, "m_maxWaveText", maxWaveText);
        SetPrivateField(lobbyView, "m_battleStartButton", battleStartBtn);
        SetPrivateField(lobbyView, "m_settingsButton", settingsBtn);
        SetPrivateField(lobbyView, "m_profileButton", profileBtn);
        SetPrivateField(lobbyView, "m_normalDifficultyButton", normalDiffBtn);
        SetPrivateField(lobbyView, "m_eliteDifficultyButton", eliteDiffBtn);

        var mockVM = new MockLobbyViewModel();
        lobbyView.Initialize(mockVM);

        lobbyView.func_OnNormalDifficultyClicked();
        Debug.Log($"[LobbyView 난이도 변경(Normal) 검증] 예상 값: Normal | 현재 값: {mockVM.SelectedDifficultyParam}");
        Assert.AreEqual(StageDifficulty.Normal, mockVM.SelectedDifficultyParam);

        lobbyView.func_OnEliteDifficultyClicked();
        Debug.Log($"[LobbyView 난이도 변경(Elite) 검증] 예상 값: Elite | 현재 값: {mockVM.SelectedDifficultyParam}");
        Assert.AreEqual(StageDifficulty.Elite, mockVM.SelectedDifficultyParam);

        var battleStartMethod = typeof(LobbyView).GetMethod("func_OnBattleStartClicked", BindingFlags.NonPublic | BindingFlags.Instance);
        battleStartMethod?.Invoke(lobbyView, null);
        Debug.Log($"[LobbyView 배틀 시작 검증] 예상 값: True | 현재 값: {mockVM.StartBattleCalled}");
        Assert.IsTrue(mockVM.StartBattleCalled);

        var settingsMethod = typeof(LobbyView).GetMethod("func_OnSettingsClicked", BindingFlags.NonPublic | BindingFlags.Instance);
        settingsMethod?.Invoke(lobbyView, null);
        Debug.Log($"[LobbyView 설정 오픈 검증] 예상 값: True | 현재 값: {mockVM.OpenSettingsCalled}");
        Assert.IsTrue(mockVM.OpenSettingsCalled);

        var profileMethod = typeof(LobbyView).GetMethod("func_OnProfileClicked", BindingFlags.NonPublic | BindingFlags.Instance);
        profileMethod?.Invoke(lobbyView, null);
        Debug.Log($"[LobbyView 프로필 오픈 검증] 예상 값: True | 현재 값: {mockVM.OpenProfileCalled}");
        Assert.IsTrue(mockVM.OpenProfileCalled);
    }

    [Test]
    public void TC15_UserProfilePopupView_Close_Button_Verifying()
    {
        Debug.Log("[UserProfilePopupView 테스트] 입력: 프로필 팝업의 닫기 버튼 클릭 이벤트 발생");
        Debug.Log("[UserProfilePopupView 테스트] 예상 출력: 뷰모델의 RequestClose() 정상 호출");

        var popupGo = new GameObject("UserProfilePopupView");
        popupGo.transform.SetParent(m_testGo.transform);
        var popupView = popupGo.AddComponent<UserProfilePopupView>();

        var uidText = new GameObject().AddComponent<TextMeshProUGUI>();
        uidText.transform.SetParent(popupGo.transform);
        var profileImage = new GameObject().AddComponent<Image>();
        profileImage.transform.SetParent(popupGo.transform);

        SetPrivateField(popupView, "m_uidText", uidText);
        SetPrivateField(popupView, "m_profileImage", profileImage);

        var mockVM = new MockUserProfileViewModel();
        popupView.Initialize(mockVM);

        popupView.func_OnCloseButtonClicked();
        Debug.Log($"[UserProfilePopupView 닫기 검증] 예상 값: True | 현재 값: {mockVM.RequestCloseCalled}");
        Assert.IsTrue(mockVM.RequestCloseCalled);
    }

    private void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(target, value);
    }
}
