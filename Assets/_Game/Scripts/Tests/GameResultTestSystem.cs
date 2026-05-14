using UnityEngine;
using System.Collections.Generic;

public class GameResultTestSystem : MonoBehaviour
{
    [SerializeField] private GameResultPanelView m_targetView;
    [SerializeField] private List<Sprite> m_testMvpSprites;
    [SerializeField] private ItemDatabaseSO m_itemDatabase;
    [SerializeField] private EasyTransition.TransitionSettings m_transitionSettings;

    [ContextMenu("Test Victory")]
    public void func_TestVictory()
    {
        RunTest(true);
    }

    [ContextMenu("Test Defeat")]
    public void func_TestDefeat()
    {
        RunTest(false);
    }

    [ContextMenu("Test Random")]
    public void func_TestRandom()
    {
        RunTest(Random.value > 0.5f);
    }

    public void func_CloseTestUI()
    {
        if (m_targetView != null)
        {
            m_targetView.gameObject.SetActive(false);
            Time.timeScale = 1f;
        }
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
            GameResultViewModel viewModel = new GameResultViewModel(mockData);
            
            viewModel.OnBackToMain += () => 
            {
                Time.timeScale = 1f;
                if (m_transitionSettings != null)
                {
                    ISceneLoader loader = new EasyTransitionLoader(m_transitionSettings);
                    loader.LoadScene("Main");
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
                }
            };

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

        if (m_itemDatabase != null)
        {
            var allItems = m_itemDatabase.GetAllItems();

            if (allItems != null && allItems.Count > 0)
            {
                int rewardCount = Random.Range(3, 7);
                for (int i = 0; i < rewardCount; i++)
                {
                    var itemData = allItems[Random.Range(0, allItems.Count)];
                    RewardItemDTO reward = new RewardItemDTO
                    {
                        ItemId = itemData.ItemId,
                        Amount = Random.Range(1, 100),
                        ItemIcon = itemData.ItemIcon
                    };
                    dto.StageRewards.Add(reward);
                }
            }
        }

        return dto;
    }
}
