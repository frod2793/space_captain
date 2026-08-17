using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// 로비에서 저장한 편성이 InGame 씬의 스폰까지 그대로 도달하는지 확인한다.
/// 씬에 미리 배치된 캐릭터가 아니라 PlayerSwapManager.Characters 목록을 본다.
/// </summary>
public class BattleDeckApplyTests
{
    private const string SAVE_KEY = "SpaceCaptain.Deck";
    private const int MAX_WAIT_FRAMES = 900;

    private const BindingFlags ANY_INSTANCE =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private object m_userData;
    private List<string> m_originalDeck;

    private static object GetProp(object target, string name)
    {
        PropertyInfo prop = target.GetType().GetProperty(name, ANY_INSTANCE);
        Assert.IsNotNull(prop, $"프로퍼티 {name}을 찾을 수 없다");
        return prop.GetValue(target);
    }

    private static object GetField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, ANY_INSTANCE);
        Assert.IsNotNull(field, $"필드 {name}을 찾을 수 없다");
        return field.GetValue(target);
    }

    private static object FindInScene(string typeName)
    {
        Type type = TestReflectionHelper.GetGameType(typeName);
        Assert.IsNotNull(type, $"{typeName} 타입을 찾을 수 없다");
        return UnityEngine.Object.FindAnyObjectByType(type, FindObjectsInactive.Include);
    }

    private static List<string> DeckOf(object userData)
    {
        return (List<string>)GetField(GetProp(userData, "LobbyData"), "DeckCharacters");
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {

        m_userData = Resources.Load("UserData");
        Assert.IsNotNull(m_userData, "Resources/UserData를 찾을 수 없다");
        m_originalDeck = new List<string>(DeckOf(m_userData));

        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        // 캡처한 값을 되돌리면 그 값이 0일 때 0을 그대로 복원해 다음 클래스가 멈춘다.
        // 전투 씬은 Awake에서 timeScale = 0으로 시작하므로 무조건 1로 되돌린다.
        Time.timeScale = 1f;
        PlayerPrefs.DeleteKey(SAVE_KEY);

        if (m_userData != null && m_originalDeck != null)
        {
            List<string> deck = DeckOf(m_userData);
            deck.Clear();
            deck.AddRange(m_originalDeck);
        }
    }

    /// <summary>편성을 PlayerPrefs에 심고 InGame을 띄운 뒤 스폰된 캐릭터 목록을 돌려준다.</summary>
    private IEnumerator LoadBattleWithDeck(List<string> deck, Action<IList> onReady)
    {
        PlayerPrefs.SetString(SAVE_KEY, string.Join("\n", deck));
        PlayerPrefs.Save();

        SceneManager.LoadScene("InGame");
        yield return null;

        object swapManager = null;
        IList characters = null;

        for (int i = 0; i < MAX_WAIT_FRAMES; i++)
        {
            swapManager = FindInScene("PlayerSwapManager");

            if (swapManager != null)
            {
                characters = (IList)GetProp(swapManager, "Characters");

                if (characters != null && characters.Count > 0)
                {
                    break;
                }
            }

            yield return null;
        }

        Assert.IsNotNull(swapManager, "PlayerSwapManager가 InGame 씬에 없다");
        Assert.IsNotNull(characters, "Characters 목록이 null이다");
        Assert.Greater(characters.Count, 0, "편성이 전투에 반영되지 않았다 - 스폰된 캐릭터가 없다");

        onReady(characters);
    }

    private static List<string> IdsOf(IList characters)
    {
        var ids = new List<string>();

        for (int i = 0; i < characters.Count; i++)
        {
            ids.Add((string)GetProp(characters[i], "CharacterID"));
        }

        return ids;
    }

    [UnityTest]
    public IEnumerator 저장된_편성_순서대로_인게임에_스폰된다()
    {
        // 뒤집힘/기본값 대체를 구분할 수 있는 순서를 쓴다.
        // 기본값은 a,b,c,d,e 이고 이 덱을 뒤집으면 b,d,a,e,c 다.
        var deck = new List<string> { "c", "e", "a", "d", "b" };
        IList characters = null;

        yield return LoadBattleWithDeck(deck, c => characters = c);

        List<string> actual = IdsOf(characters);
        CollectionAssert.AreEqual(deck, actual,
            $"스폰 순서가 편성과 다르다. 저장={string.Join(",", deck)} 실제={string.Join(",", actual)}");
    }

    [UnityTest]
    public IEnumerator 편성_선두가_Active로_투입된다()
    {
        var deck = new List<string> { "c", "a", "b", "d", "e" };
        IList characters = null;

        yield return LoadBattleWithDeck(deck, c => characters = c);

        Assert.IsTrue((bool)GetProp(characters[0], "IsActive"), "선두 캐릭터가 Active가 아니다");

        for (int i = 1; i < characters.Count; i++)
        {
            string id = (string)GetProp(characters[i], "CharacterID");
            Assert.IsFalse((bool)GetProp(characters[i], "IsActive"), $"{id}(슬롯 {i})가 Active로 잘못 들어갔다");
        }
    }

    [UnityTest]
    public IEnumerator 필드3_예비2로_역할이_배정된다()
    {
        var deck = new List<string> { "a", "b", "c", "d", "e" };
        IList characters = null;

        yield return LoadBattleWithDeck(deck, c => characters = c);

        Assert.AreEqual(5, characters.Count, "5명 편성이 5명으로 스폰되지 않았다");

        var states = new List<string>();
        for (int i = 0; i < characters.Count; i++)
        {
            states.Add(GetProp(characters[i], "SwapState").ToString());
        }

        Assert.AreEqual("Active", states[0], "슬롯 0은 Active여야 한다");
        Assert.AreEqual("Standby", states[1], "슬롯 1은 Standby여야 한다");
        Assert.AreEqual("Standby", states[2], "슬롯 2는 Standby여야 한다");
        Assert.AreEqual("Reserve", states[3], "슬롯 3은 Reserve여야 한다");
        Assert.AreEqual("Reserve", states[4], "슬롯 4는 Reserve여야 한다");
    }

    [UnityTest]
    public IEnumerator 캐릭터_이름은_프리팹이_아니라_편성한_데이터를_따른다()
    {
        // f~i는 a와 같은 프리팹을 공유한다. 주입이 없으면 전부 a의 이름으로 보인다.
        var deck = new List<string> { "g", "h", "i" };
        IList characters = null;

        yield return LoadBattleWithDeck(deck, c => characters = c);

        object database = Resources.Load("CharacterDatabase");
        Assert.IsNotNull(database, "Resources/CharacterDatabase를 찾을 수 없다");

        MethodInfo getCharacter = database.GetType().GetMethod("GetCharacter", ANY_INSTANCE);

        for (int i = 0; i < characters.Count; i++)
        {
            string id = (string)GetProp(characters[i], "CharacterID");
            object data = getCharacter.Invoke(database, new object[] { id });
            Assert.IsNotNull(data, $"DB에서 {id}를 찾을 수 없다");

            string expected = (string)GetProp(data, "CharacterName");
            string actual = (string)GetProp(characters[i], "CharacterName");

            Assert.AreEqual(expected, actual, $"{id}의 이름이 편성 데이터와 다르다");
        }
    }

    [UnityTest]
    public IEnumerator 세명만_편성하면_세명만_스폰된다()
    {
        var deck = new List<string> { "b", "c", "d" };
        IList characters = null;

        yield return LoadBattleWithDeck(deck, c => characters = c);

        CollectionAssert.AreEqual(deck, IdsOf(characters), "3명 편성이 그대로 반영되지 않았다");
    }
}
