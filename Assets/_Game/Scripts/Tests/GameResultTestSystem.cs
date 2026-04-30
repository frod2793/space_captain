using UnityEngine;
using System.Collections.Generic;

public class GameResultTestSystem : MonoBehaviour
{
    [SerializeField] private GameResultPanelView m_targetView;
    [SerializeField] private List<Sprite> m_testMvpSprites;
    [SerializeField] private List<Sprite> m_testItemIcons;

    [ContextMenu("Test Victory")]
    public void TestVictory()
    {
        RunTest(true);
    }

    [ContextMenu("Test Defeat")]
    public void TestDefeat()
    {
        RunTest(false);
    }

    private void RunTest(bool isClear)
    {
        if (m_targetView == null)
        {
            m_targetView = FindAnyObjectByType<GameResultPanelView>(FindObjectsInactive.Include);
        }

        if (m_targetView != null)
        {
            Time.timeScale = 0f;
            GameResultDTO mockData = GenerateMockData(isClear);
            IGameResultViewModel viewModel = new GameResultViewModel(mockData);
            
            m_targetView.gameObject.SetActive(true);
            m_targetView.Initialize(viewModel);
        }
    }

    private GameResultDTO GenerateMockData(bool isClear)
    {
        GameResultDTO dto = new GameResultDTO();
        dto.IsClear = isClear;

        string[] characterNames = { "ACE", "BLADE", "CRUISER", "DEFENDER", "EAGLE" };
        
        for (int i = 0; i < characterNames.Length; i++)
        {
            string name = characterNames[i];
            // 70% 확률로 데미지 할당, 나머지는 0 (예비 캐릭터 시뮬레이션)
            if (Random.value > 0.3f)
            {
                dto.CharacterDamages[name] = Random.Range(50000, 1000000);
            }
            else
            {
                dto.CharacterDamages[name] = 0;
            }
        }

        string mvpID = string.Empty;
        int maxDamage = -1;
        var keys = new List<string>(dto.CharacterDamages.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            int damage = dto.CharacterDamages[key];
            if (damage > maxDamage)
            {
                maxDamage = damage;
                mvpID = key;
            }
        }

        if (!string.IsNullOrEmpty(mvpID) && m_testMvpSprites != null && m_testMvpSprites.Count > 0)
        {
            int spriteIndex = Random.Range(0, m_testMvpSprites.Count);
            dto.MvpSprite = m_testMvpSprites[spriteIndex];
            dto.MvpCharacterName = mvpID;
        }

        if (m_testItemIcons != null && m_testItemIcons.Count > 0)
        {
            int rewardCount = Random.Range(3, 7);
            for (int i = 0; i < rewardCount; i++)
            {
                int iconIndex = Random.Range(0, m_testItemIcons.Count);
                RewardItemDTO reward = new RewardItemDTO
                {
                    ItemId = $"ITEM_{iconIndex}",
                    Amount = Random.Range(1, 100),
                    ItemIcon = m_testItemIcons[iconIndex]
                };
                dto.StageRewards.Add(reward);
            }
        }

        return dto;
    }
}
