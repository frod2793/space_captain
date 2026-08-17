using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PartyViewModelTests
{
    private const string SAVE_KEY = "SpaceCaptain.Deck";

    private const BindingFlags ANY_INSTANCE =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private Type m_characterDataType;
    private Type m_statsType;
    private Type m_partyViewModelType;

    private object m_userData;
    private object m_database;

    private readonly List<UnityEngine.Object> m_created = new List<UnityEngine.Object>();

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

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, ANY_INSTANCE);
        Assert.IsNotNull(field, $"필드 {name}을 찾을 수 없다");
        field.SetValue(target, value);
    }

    private static object Invoke(object target, string name, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(name, ANY_INSTANCE);
        Assert.IsNotNull(method, $"메서드 {name}을 찾을 수 없다");
        return method.Invoke(target, args);
    }

    private static void AddHandler(object target, string eventName, Action handler)
    {
        EventInfo evt = target.GetType().GetEvent(eventName, ANY_INSTANCE);
        Assert.IsNotNull(evt, $"이벤트 {eventName}을 찾을 수 없다");
        evt.AddEventHandler(target, handler);
    }

    private static int CountOf(object enumerable)
    {
        int count = 0;
        foreach (object unused in (IEnumerable)enumerable)
        {
            count++;
        }
        return count;
    }

    private static List<string> DeckIds(object viewModel)
    {
        var ids = new List<string>();

        foreach (object item in (IEnumerable)GetProp(viewModel, "Deck"))
        {
            ids.Add(item == null ? null : (string)GetProp(item, "CharacterID"));
        }

        return ids;
    }

    private List<string> SavedDeckIds()
    {
        return (List<string>)GetField(GetProp(m_userData, "LobbyData"), "DeckCharacters");
    }

    private object MakeCharacter(string id, int attack, int hp)
    {
        object data = ScriptableObject.CreateInstance(m_characterDataType);
        object stats = Activator.CreateInstance(m_statsType);

        SetField(stats, "ID", id);
        SetField(stats, "AttackDamage", attack);
        SetField(stats, "MaxHp", hp);
        SetField(stats, "CurrentHp", hp);

        SetField(data, "m_characterID", id);
        SetField(data, "m_characterName", id.ToUpper());
        SetField(data, "m_baseStats", stats);

        m_created.Add((UnityEngine.Object)data);
        return data;
    }

    private void SetupDatabase(params object[] characters)
    {
        Type listType = typeof(List<>).MakeGenericType(m_characterDataType);
        object list = Activator.CreateInstance(listType);
        MethodInfo add = listType.GetMethod("Add");

        for (int i = 0; i < characters.Length; i++)
        {
            add.Invoke(list, new[] { characters[i] });
        }

        SetField(m_database, "m_characters", list);
    }

    private object MakeViewModel(List<string> savedDeck)
    {
        List<string> deck = SavedDeckIds();
        deck.Clear();

        if (savedDeck != null)
        {
            deck.AddRange(savedDeck);
        }

        object viewModel = Activator.CreateInstance(m_partyViewModelType);
        Invoke(viewModel, "SetData", m_userData, m_database);
        return viewModel;
    }

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);

        m_characterDataType = TestReflectionHelper.GetGameType("CharacterDataSO");
        m_statsType = TestReflectionHelper.GetGameType("PlayerStatsDTO");
        m_partyViewModelType = TestReflectionHelper.GetGameType("PartyViewModel");

        Assert.IsNotNull(m_characterDataType, "CharacterDataSO 타입을 찾을 수 없다");
        Assert.IsNotNull(m_statsType, "PlayerStatsDTO 타입을 찾을 수 없다");
        Assert.IsNotNull(m_partyViewModelType, "PartyViewModel 타입을 찾을 수 없다");

        m_userData = ScriptableObject.CreateInstance(TestReflectionHelper.GetGameType("UserDataSO"));
        m_database = ScriptableObject.CreateInstance(TestReflectionHelper.GetGameType("CharacterDatabaseSO"));

        m_created.Add((UnityEngine.Object)m_userData);
        m_created.Add((UnityEngine.Object)m_database);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);

        for (int i = 0; i < m_created.Count; i++)
        {
            UnityEngine.Object.DestroyImmediate(m_created[i]);
        }

        m_created.Clear();
    }

    [Test]
    public void SetData하면_덱은_항상_5칸이다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100), MakeCharacter("b", 20, 200));
        object viewModel = MakeViewModel(new List<string> { "a", "b" });

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual(5, deck.Count);
        Assert.AreEqual("a", deck[0]);
        Assert.AreEqual("b", deck[1]);
        Assert.IsNull(deck[2]);
        Assert.IsNull(deck[3]);
        Assert.IsNull(deck[4]);
    }

    [Test]
    public void SetData하면_DB_전체가_AllCharacters로_노출된다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100), MakeCharacter("b", 20, 200), MakeCharacter("c", 30, 300));
        object viewModel = MakeViewModel(null);

        Assert.AreEqual(3, CountOf(GetProp(viewModel, "AllCharacters")));
    }

    [Test]
    public void DB에_없는_ID는_덱에서_무시된다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(new List<string> { "없는놈", "a" });

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual("a", deck[0], "빈칸 없이 앞으로 당겨져야 한다");
        Assert.IsNull(deck[1]);
    }

    [Test]
    public void 저장된_덱이_5개를_넘으면_앞에서_5개만_읽는다()
    {
        SetupDatabase(
            MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1),
            MakeCharacter("d", 1, 1), MakeCharacter("e", 1, 1), MakeCharacter("f", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c", "d", "e", "f" });

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual(5, deck.Count);
        Assert.AreEqual("e", deck[4]);
    }

    [Test]
    public void PendingSlot_초기값은_음수1이다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(null);

        Assert.AreEqual(-1, (int)GetProp(viewModel, "PendingSlot"));
    }

    [Test]
    public void BeginSelect하면_PendingSlot이_바뀌고_이벤트가_발생한다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(null);

        int requested = 0;
        AddHandler(viewModel, "OnSelectRequested", () => requested++);

        Invoke(viewModel, "BeginSelect", 3);

        Assert.AreEqual(3, (int)GetProp(viewModel, "PendingSlot"));
        Assert.AreEqual(1, requested);
    }

    [Test]
    public void 범위를_벗어난_BeginSelect는_무시된다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(null);

        Invoke(viewModel, "BeginSelect", 5);
        Assert.AreEqual(-1, (int)GetProp(viewModel, "PendingSlot"));

        Invoke(viewModel, "BeginSelect", -2);
        Assert.AreEqual(-1, (int)GetProp(viewModel, "PendingSlot"));
    }

    [Test]
    public void CancelSelect하면_PendingSlot이_풀리고_닫힘이벤트가_발생한다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(null);

        int closed = 0;
        AddHandler(viewModel, "OnSelectClosed", () => closed++);

        Invoke(viewModel, "BeginSelect", 1);
        Invoke(viewModel, "CancelSelect");

        Assert.AreEqual(-1, (int)GetProp(viewModel, "PendingSlot"));
        Assert.AreEqual(1, closed);
    }

    [Test]
    public void 빈슬롯에_미편성_캐릭터를_고르면_그_슬롯에_들어간다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100), MakeCharacter("b", 20, 200));
        object viewModel = MakeViewModel(new List<string> { "a" });

        Invoke(viewModel, "BeginSelect", 1);
        Invoke(viewModel, "PickCharacter", "b");

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual("a", deck[0]);
        Assert.AreEqual("b", deck[1]);
        Assert.AreEqual(-1, (int)GetProp(viewModel, "PendingSlot"));
    }

    [Test]
    public void 이미_다른_슬롯에_있는_캐릭터를_고르면_두_슬롯이_교환된다()
    {
        SetupDatabase(
            MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1),
            MakeCharacter("d", 1, 1), MakeCharacter("e", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c", "d", "e" });

        Invoke(viewModel, "BeginSelect", 0);
        Invoke(viewModel, "PickCharacter", "d");

        CollectionAssert.AreEqual(
            new List<string> { "d", "b", "c", "a", "e" },
            DeckIds(viewModel));
    }

    [Test]
    public void 교환후에도_덱에_중복이_없다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c" });

        Invoke(viewModel, "BeginSelect", 2);
        Invoke(viewModel, "PickCharacter", "a");

        List<string> deck = DeckIds(viewModel);
        var seen = new HashSet<string>();

        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i] != null)
            {
                Assert.IsTrue(seen.Add(deck[i]), "중복이 생겼다");
            }
        }
    }

    [Test]
    public void 같은_슬롯의_캐릭터를_다시_고르면_해제된다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b" });

        Invoke(viewModel, "BeginSelect", 0);
        Invoke(viewModel, "PickCharacter", "a");

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual("b", deck[0], "a가 빠지고 b가 당겨와야 한다");
        Assert.IsNull(deck[1]);
    }

    [Test]
    public void 가운데_슬롯을_비우면_뒤가_앞으로_당겨온다()
    {
        SetupDatabase(
            MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1),
            MakeCharacter("d", 1, 1), MakeCharacter("e", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c", "d", "e" });

        Invoke(viewModel, "ClearSlot", 1);

        CollectionAssert.AreEqual(
            new List<string> { "a", "c", "d", "e", null },
            DeckIds(viewModel));
    }

    [Test]
    public void 빈칸은_항상_뒤쪽에만_연속으로_존재한다()
    {
        SetupDatabase(
            MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c" });

        Invoke(viewModel, "ClearSlot", 0);

        List<string> deck = DeckIds(viewModel);
        bool sawNull = false;

        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i] == null)
            {
                sawNull = true;
            }
            else
            {
                Assert.IsFalse(sawNull, $"인덱스 {i}에 빈칸 뒤의 캐릭터가 있다");
            }
        }
    }

    [Test]
    public void 뒤쪽_빈슬롯을_골라도_앞의_빈칸부터_채워진다()
    {
        SetupDatabase(
            MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b" });

        Invoke(viewModel, "BeginSelect", 4);
        Invoke(viewModel, "PickCharacter", "c");

        List<string> deck = DeckIds(viewModel);

        Assert.AreEqual("c", deck[2], "빈칸이 앞으로 당겨져 슬롯2에 놓인다");
        Assert.IsNull(deck[3]);
        Assert.IsNull(deck[4]);
    }

    [Test]
    public void 선택중이_아니면_PickCharacter는_무시된다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a" });

        Invoke(viewModel, "PickCharacter", "b");

        Assert.IsNull(DeckIds(viewModel)[1]);
    }

    [Test]
    public void DB에_없는_ID를_고르면_무시된다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a" });

        Invoke(viewModel, "BeginSelect", 1);
        Invoke(viewModel, "PickCharacter", "없는놈");

        Assert.IsNull(DeckIds(viewModel)[1]);
        Assert.AreEqual(1, (int)GetProp(viewModel, "PendingSlot"), "실패했으므로 선택 상태가 유지된다");
    }

    [Test]
    public void PickCharacter_성공시_덱변경과_선택닫힘_이벤트가_모두_발생한다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a" });

        int changed = 0;
        int closed = 0;
        AddHandler(viewModel, "OnDeckChanged", () => changed++);
        AddHandler(viewModel, "OnSelectClosed", () => closed++);

        Invoke(viewModel, "BeginSelect", 1);
        Invoke(viewModel, "PickCharacter", "b");

        Assert.AreEqual(1, changed);
        Assert.AreEqual(1, closed);
    }

    [Test]
    public void 전투력은_공격력곱10더하기체력의_합이다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100), MakeCharacter("b", 20, 200));
        object viewModel = MakeViewModel(new List<string> { "a", "b" });

        // a: 10*10 + 100 = 200,  b: 20*10 + 200 = 400
        Assert.AreEqual(600, (int)GetProp(viewModel, "CombatPower"));
    }

    [Test]
    public void 빈칸은_전투력에_0으로_계산된다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(new List<string> { "a" });

        Assert.AreEqual(200, (int)GetProp(viewModel, "CombatPower"));
    }

    [Test]
    public void 자동편성은_전투력_상위5명을_내림차순으로_채운다()
    {
        SetupDatabase(
            MakeCharacter("low", 1, 10),
            MakeCharacter("top", 100, 1000),
            MakeCharacter("mid", 50, 500),
            MakeCharacter("a", 10, 100),
            MakeCharacter("b", 20, 200),
            MakeCharacter("c", 30, 300));
        object viewModel = MakeViewModel(null);

        Invoke(viewModel, "AutoArrange");

        CollectionAssert.AreEqual(
            new List<string> { "top", "mid", "c", "b", "a" },
            DeckIds(viewModel));
    }

    [Test]
    public void 보유가_5명_미만이면_자동편성후_나머지는_빈칸이다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100), MakeCharacter("b", 20, 200));
        object viewModel = MakeViewModel(null);

        Invoke(viewModel, "AutoArrange");

        CollectionAssert.AreEqual(
            new List<string> { "b", "a", null, null, null },
            DeckIds(viewModel));
    }

    [Test]
    public void 자동편성은_덱변경_이벤트를_발생시킨다()
    {
        SetupDatabase(MakeCharacter("a", 10, 100));
        object viewModel = MakeViewModel(null);

        int changed = 0;
        AddHandler(viewModel, "OnDeckChanged", () => changed++);

        Invoke(viewModel, "AutoArrange");

        Assert.AreEqual(1, changed);
    }

    [Test]
    public void Commit은_빈칸을_뺀_ID리스트를_DeckCharacters에_넣는다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1), MakeCharacter("c", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b", "c" });

        Invoke(viewModel, "ClearSlot", 1);
        Invoke(viewModel, "Commit");

        CollectionAssert.AreEqual(new List<string> { "a", "c" }, SavedDeckIds());
    }

    [Test]
    public void Commit은_PlayerPrefs에_저장한다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "b" });

        Invoke(viewModel, "BeginSelect", 0);
        Invoke(viewModel, "PickCharacter", "b");
        Invoke(viewModel, "Commit");

        SavedDeckIds().Clear();
        Invoke(m_userData, "LoadData");

        CollectionAssert.AreEqual(new List<string> { "b", "a" }, SavedDeckIds());
    }

    [Test]
    public void 저장된_덱에_중복이_있으면_하나만_남긴다()
    {
        SetupDatabase(MakeCharacter("a", 1, 1), MakeCharacter("b", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a", "a", "b" });

        CollectionAssert.AreEqual(
            new List<string> { "a", "b", null, null, null },
            DeckIds(viewModel));
    }

    [Test]
    public void 덱_길이는_DECK_SIZE_상수를_따른다()
    {
        int deckSize = (int)m_partyViewModelType
            .GetField("DECK_SIZE", BindingFlags.Public | BindingFlags.Static)
            .GetValue(null);

        SetupDatabase(MakeCharacter("a", 1, 1));
        object viewModel = MakeViewModel(new List<string> { "a" });

        Assert.AreEqual(deckSize, DeckIds(viewModel).Count);
    }
}
