using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Main 씬을 실제로 띄워 파티 편성 UI 배선이 동작하는지 확인한다.
/// 프리팹/씬 연결이 빠지면 여기서 잡힌다.
/// </summary>
public class PartyUIWiringTests
{
    private const string SAVE_KEY = "SpaceCaptain.Deck";
    private const int MAX_WAIT_FRAMES = 600;

    private const BindingFlags ANY_INSTANCE =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private List<string> m_originalDeck;
    private object m_userData;

    private static object GetField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, ANY_INSTANCE);
        Assert.IsNotNull(field, $"필드 {name}을 찾을 수 없다");
        return field.GetValue(target);
    }

    private static object GetProp(object target, string name)
    {
        PropertyInfo prop = target.GetType().GetProperty(name, ANY_INSTANCE);
        Assert.IsNotNull(prop, $"프로퍼티 {name}을 찾을 수 없다");
        return prop.GetValue(target);
    }

    private static object FindInScene(string typeName)
    {
        Type type = TestReflectionHelper.GetGameType(typeName);
        Assert.IsNotNull(type, $"{typeName} 타입을 찾을 수 없다");
        return UnityEngine.Object.FindAnyObjectByType(type, FindObjectsInactive.Include);
    }

    private static List<string> DeckOf(object userData)
    {
        object lobbyData = GetProp(userData, "LobbyData");
        return (List<string>)GetField(lobbyData, "DeckCharacters");
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);

        SceneManager.LoadScene("Main");
        yield return null;

        // LobbyInitializer가 로컬라이징을 비동기로 읽으므로 뷰모델 주입까지 기다린다
        object popup = null;
        for (int i = 0; i < MAX_WAIT_FRAMES; i++)
        {
            popup = FindInScene("PartyPopupView");

            if (popup != null && GetField(popup, "m_viewModel") != null)
            {
                break;
            }

            yield return null;
        }

        Assert.IsNotNull(popup, "PartyPopupView가 Main 씬에 없다 - 씬 배선이 빠졌다");
        Assert.IsNotNull(GetField(popup, "m_viewModel"), "LobbyInitializer가 팝업에 ViewModel을 주입하지 않았다");

        m_userData = Resources.Load("UserData");
        Assert.IsNotNull(m_userData, "Resources/UserData를 찾을 수 없다");
        m_originalDeck = new List<string>(DeckOf(m_userData));
    }

    [TearDown]
    public void TearDown()
    {
        // 테스트가 Resources의 UserData 에셋을 건드리므로 원래 편성으로 되돌린다
        if (m_userData != null && m_originalDeck != null)
        {
            List<string> deck = DeckOf(m_userData);
            deck.Clear();
            deck.AddRange(m_originalDeck);
        }

        PlayerPrefs.DeleteKey(SAVE_KEY);
    }

    [UnityTest]
    public IEnumerator 편성버튼을_누르면_팝업이_열린다()
    {
        object lobbyView = FindInScene("LobbyView");
        Assert.IsNotNull(lobbyView, "LobbyView가 씬에 없다");

        var partyButton = (Button)GetField(lobbyView, "m_partyButton");
        Assert.IsNotNull(partyButton, "LobbyView.m_partyButton이 연결되지 않았다");

        object popup = FindInScene("PartyPopupView");
        var popupBehaviour = (MonoBehaviour)popup;

        Assert.IsFalse(popupBehaviour.gameObject.activeSelf, "팝업은 처음에 닫혀 있어야 한다");

        partyButton.onClick.Invoke();
        yield return null;

        Assert.IsTrue(popupBehaviour.gameObject.activeSelf, "편성 버튼을 눌러도 팝업이 열리지 않았다");
    }

    [UnityTest]
    public IEnumerator 슬롯을_누르면_선택패널이_열리고_그리드가_채워진다()
    {
        object popup = FindInScene("PartyPopupView");
        var popupBehaviour = (MonoBehaviour)popup;
        popupBehaviour.gameObject.SetActive(true);
        yield return null;

        var slots = (Array)GetField(popup, "m_slotViews");
        Assert.AreEqual(5, slots.Length, "슬롯 배열은 5칸이어야 한다");

        var selectPanel = (GameObject)GetField(popup, "m_selectPanel");
        Assert.IsNotNull(selectPanel, "m_selectPanel이 연결되지 않았다");
        Assert.IsFalse(selectPanel.activeSelf, "선택 패널은 처음에 닫혀 있어야 한다");

        object firstSlot = slots.GetValue(0);
        Assert.IsNotNull(firstSlot, "m_slotViews[0]이 비어 있다");

        var slotButton = (Button)GetField(firstSlot, "m_button");
        Assert.IsNotNull(slotButton, "슬롯 프리팹의 m_button이 연결되지 않았다");

        slotButton.onClick.Invoke();
        yield return null;

        Assert.IsTrue(selectPanel.activeSelf, "슬롯을 눌러도 선택 패널이 열리지 않았다");

        var grid = (Transform)GetField(popup, "m_gridContainer");
        Assert.IsNotNull(grid, "m_gridContainer가 연결되지 않았다");

        object viewModel = GetField(popup, "m_viewModel");
        var all = (IEnumerable)GetProp(viewModel, "AllCharacters");

        int expected = 0;
        foreach (object unused in all)
        {
            expected++;
        }

        Assert.Greater(expected, 0, "보유 캐릭터가 0명이라 그리드를 검증할 수 없다");
        Assert.AreEqual(expected, grid.childCount, "그리드 셀 수가 보유 캐릭터 수와 다르다");
    }

    [UnityTest]
    public IEnumerator 그리드에서_캐릭터를_고르면_덱이_바뀌고_저장된다()
    {
        object popup = FindInScene("PartyPopupView");
        var popupBehaviour = (MonoBehaviour)popup;
        popupBehaviour.gameObject.SetActive(true);
        yield return null;

        object viewModel = GetField(popup, "m_viewModel");

        // 슬롯 0을 고른 뒤 그리드의 마지막 캐릭터를 선택한다
        var slots = (Array)GetField(popup, "m_slotViews");
        ((Button)GetField(slots.GetValue(0), "m_button")).onClick.Invoke();
        yield return null;

        var grid = (Transform)GetField(popup, "m_gridContainer");
        Transform lastCell = grid.GetChild(grid.childCount - 1);
        var cellView = lastCell.GetComponent(TestReflectionHelper.GetGameType("CharacterSlotView"));
        ((Button)GetField(cellView, "m_button")).onClick.Invoke();
        yield return null;

        object firstInDeck = ((IList)GetProp(viewModel, "Deck"))[0];
        Assert.IsNotNull(firstInDeck, "선택 후에도 슬롯 0이 비어 있다");

        var closeButton = (Button)GetField(popup, "m_closeButton");
        closeButton.onClick.Invoke();
        yield return new WaitForSeconds(0.5f);

        Assert.IsFalse(popupBehaviour.gameObject.activeSelf, "닫기 후에도 팝업이 열려 있다");

        string saved = PlayerPrefs.GetString(SAVE_KEY, string.Empty);
        Assert.IsNotEmpty(saved, "닫을 때 편성이 저장되지 않았다");

        string expectedFirst = (string)GetProp(firstInDeck, "CharacterID");
        Assert.AreEqual(expectedFirst, saved.Split('\n')[0], "저장된 선두 캐릭터가 편성과 다르다");
    }
}
