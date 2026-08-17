using System;
using System.Collections.Generic;
using System.Linq;

public class PartyViewModel : IPartyViewModel
{
    public const int DECK_SIZE = 5;
    public const int FIELD_SIZE = 3;

    private const int ATTACK_WEIGHT = 10;

    private readonly List<CharacterDataSO> m_deck = new List<CharacterDataSO>();
    private readonly List<CharacterDataSO> m_allCharacters = new List<CharacterDataSO>();

    private UserDataSO m_userData;
    private CharacterDatabaseSO m_database;
    private int m_pendingSlot = -1;

    public IReadOnlyList<CharacterDataSO> Deck => m_deck;
    public IReadOnlyList<CharacterDataSO> AllCharacters => m_allCharacters;
    public int PendingSlot => m_pendingSlot;

    public int CombatPower
    {
        get
        {
            int total = 0;

            for (int i = 0; i < m_deck.Count; i++)
            {
                total += GetPower(m_deck[i]);
            }

            return total;
        }
    }

    public event Action OnDeckChanged;
    public event Action OnSelectRequested;
    public event Action OnSelectClosed;

    public PartyViewModel()
    {
        for (int i = 0; i < DECK_SIZE; i++)
        {
            m_deck.Add(null);
        }
    }

    public void SetData(UserDataSO userData, CharacterDatabaseSO database)
    {
        m_userData = userData;
        m_database = database;

        m_allCharacters.Clear();

        if (m_database != null)
        {
            List<CharacterDataSO> all = m_database.GetAllCharacters();

            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null)
                {
                    m_allCharacters.Add(all[i]);
                }
            }
        }

        for (int i = 0; i < DECK_SIZE; i++)
        {
            m_deck[i] = null;
        }

        List<string> saved = m_userData != null && m_userData.LobbyData != null
            ? m_userData.LobbyData.DeckCharacters
            : null;

        if (saved != null)
        {
            int slot = 0;

            for (int i = 0; i < saved.Count && slot < DECK_SIZE; i++)
            {
                CharacterDataSO data = m_database != null ? m_database.GetCharacter(saved[i]) : null;

                // 손으로 편집한 에셋이나 옛 저장본에 중복이 있을 수 있다.
                // 여기서 걸러야 PickCharacter의 "중복 없음" 전제가 성립한다.
                if (data != null && !m_deck.Contains(data))
                {
                    m_deck[slot] = data;
                    slot++;
                }
            }
        }

        m_pendingSlot = -1;
        OnDeckChanged?.Invoke();
    }

    public void BeginSelect(int slot)
    {
        if (slot < 0 || slot >= DECK_SIZE)
        {
            return;
        }

        m_pendingSlot = slot;
        OnSelectRequested?.Invoke();
    }

    public void CancelSelect()
    {
        m_pendingSlot = -1;
        OnSelectClosed?.Invoke();
    }

    public void PickCharacter(string characterID)
    {
        if (m_pendingSlot < 0 || m_pendingSlot >= DECK_SIZE)
        {
            return;
        }

        CharacterDataSO data = m_database != null ? m_database.GetCharacter(characterID) : null;

        if (data == null)
        {
            return;
        }

        int slot = m_pendingSlot;
        int existing = m_deck.IndexOf(data);

        m_pendingSlot = -1;
        OnSelectClosed?.Invoke();

        if (existing == slot)
        {
            // 같은 슬롯의 캐릭터를 다시 고르면 해제
            ClearSlot(slot);
            return;
        }

        if (existing >= 0)
        {
            // 이미 다른 슬롯에 있으면 두 슬롯을 교환한다. 중복이 생길 경로가 없다.
            m_deck[existing] = m_deck[slot];
        }

        m_deck[slot] = data;

        Compact();
        OnDeckChanged?.Invoke();
    }

    public void ClearSlot(int slot)
    {
        if (slot < 0 || slot >= DECK_SIZE)
        {
            return;
        }

        m_deck[slot] = null;

        Compact();
        OnDeckChanged?.Invoke();
    }

    public void AutoArrange()
    {
        List<CharacterDataSO> top = m_allCharacters
            .OrderByDescending(GetPower)
            .Take(DECK_SIZE)
            .ToList();

        for (int i = 0; i < DECK_SIZE; i++)
        {
            m_deck[i] = i < top.Count ? top[i] : null;
        }

        OnDeckChanged?.Invoke();
    }

    public void Commit()
    {
        if (m_userData == null || m_userData.LobbyData == null)
        {
            return;
        }

        List<string> ids = m_userData.LobbyData.DeckCharacters;
        ids.Clear();

        for (int i = 0; i < m_deck.Count; i++)
        {
            if (m_deck[i] != null)
            {
                ids.Add(m_deck[i].CharacterID);
            }
        }

        m_userData.SaveData();
    }

    /// <summary>
    /// 덱 중간의 빈칸을 없앤다. 빈칸이 섞이면 BattleSceneInitializer가
    /// 조회 성공분만 담아서 필드/예비 역할이 한 칸씩 밀린다.
    /// </summary>
    private void Compact()
    {
        int write = 0;

        for (int read = 0; read < DECK_SIZE; read++)
        {
            if (m_deck[read] != null)
            {
                m_deck[write] = m_deck[read];
                write++;
            }
        }

        for (; write < DECK_SIZE; write++)
        {
            m_deck[write] = null;
        }
    }

    private static int GetPower(CharacterDataSO data)
    {
        if (data == null || data.BaseStats == null)
        {
            return 0;
        }

        return data.BaseStats.AttackDamage * ATTACK_WEIGHT + data.BaseStats.MaxHp;
    }
}
