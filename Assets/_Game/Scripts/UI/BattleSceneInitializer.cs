using UnityEngine;
using System;
using System.Collections.Generic;
using SpaceCaptain.Systems.Localization;
using SpaceCaptain.UI.Components;
using Cysharp.Threading.Tasks;
using System.Threading;

public class BattleSceneInitializer : MonoBehaviour
{
    [SerializeField] private BattleHUDView m_hudView;
    [SerializeField] private GameProgressController m_progressController;
    [SerializeField] private GameResultPanelView m_resultPanelView;
    [SerializeField] private ItemDatabaseSO m_itemDatabase;
    [SerializeField] private CharacterDatabaseSO m_characterDatabase;
    [SerializeField] private UserDataSO m_userData;
    [SerializeField] private EasyTransition.TransitionSettings m_transitionSettings;

    private IBattleHUDViewModel m_hudViewModel;
    private IGameProgressViewModel m_progressViewModel;
    private IGameResultViewModel m_resultViewModel;
    private EnemySpawner m_enemySpawner;
    private LocalizationManager m_localizationManager;
    private float m_savedTimeScale = 1f;

    private void Awake()
    {
        Time.timeScale = 0f;
        InitializeSceneAsync().Forget();
    }

    private void OnDestroy()
    {
        EnemyController.OnEnemyDead -= HandleEnemyKill;
        EnemyController.OnDamageDealt -= HandleDamageDealt;
        
        if (m_hudViewModel != null)
        {
            m_hudViewModel.OnBattleSpeedChanged -= UpdateTimeScale;
            m_hudViewModel.OnShowUpgradePanel -= PauseTime;
            m_hudViewModel.OnHideUpgradePanel -= ResumeTime;
            m_hudViewModel.OnShowGameOver -= ShowResultFail;
        }

        if (m_progressViewModel != null)
        {
            m_progressViewModel.OnGameCleared -= ShowResultClear;
        }

        if (m_enemySpawner != null)
        {
            m_enemySpawner.OnWaveChanged -= m_hudViewModel.SetWave;
        }

        var masterShip = FindAnyObjectByType<MasterShip>();
        if (masterShip != null)
        {
            m_hudViewModel.OnShipSkillExecuted -= masterShip.ExecuteSkill;
        }
    }

    private async UniTaskVoid InitializeSceneAsync()
    {
        // 로컬라이징 초기화
        m_localizationManager = new LocalizationManager();
        await m_localizationManager.LoadTranslationsAsync("Localization/TranslationData");

        // 씬 내의 모든 로컬라이징 텍스트 뷰에 매니저 주입
        var localizedTexts = FindObjectsByType<LocalizedTextView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var text in localizedTexts)
        {
            text.Setup(m_localizationManager);
        }

        var battleDTO = new BattleProgressDTO();
        var progressDTO = new ProgressDTO();
        var swapManager = FindAnyObjectByType<PlayerSwapManager>();

        if (swapManager != null)
        {
            var barrier = FindAnyObjectByType<Barrier>();
            var playerHpBar = FindAnyObjectByType<PlayerHpBar>();
            swapManager.Barrier = barrier;
            swapManager.PlayerHUD = playerHpBar;

            // 동적 캐릭터 생성 및 주입
            if (m_characterDatabase != null && m_userData != null)
            {
                var deck = m_userData.LobbyData.DeckCharacters;
                
                if (deck == null || deck.Count == 0)
                {
                    deck = new List<string> { "Player_1", "Player_2", "Player" };
                }

                List<PlayerCharacterController> spawnedCharacters = new List<PlayerCharacterController>();
                for (int i = 0; i < deck.Count; i++)
                {
                    var charData = m_characterDatabase.GetCharacter(deck[i]);
                    if (charData != null && charData.Prefab != null)
                    {
                        GameObject go = Instantiate(charData.Prefab);
                        var controller = go.GetComponent<PlayerCharacterController>();
                        if (controller != null)
                        {
                            // 초기 스탯 주입
                            if (charData.BaseStats != null)
                            {
                                var stats = new PlayerStatsDTO
                                {
                                    ID = charData.CharacterID,
                                    MaxHp = charData.BaseStats.MaxHp,
                                    CurrentHp = charData.BaseStats.MaxHp,
                                    AttackDamage = charData.BaseStats.AttackDamage,
                                    MoveSpeed = charData.BaseStats.MoveSpeed,
                                    Level = charData.BaseStats.Level,
                                    IsActive = (i == 0)
                                };
                                controller.Initialize(stats);
                            }
                            
                            spawnedCharacters.Add(controller);
                        }
                    }
                }

                if (spawnedCharacters.Count > 0)
                {
                    swapManager.SetCharacters(spawnedCharacters);
                }
            }
        }

        m_hudViewModel = new BattleHUDViewModel();
        m_hudViewModel.BattleData = battleDTO;
        m_hudViewModel.SwapManager = swapManager;

        var masterShip = FindAnyObjectByType<MasterShip>();
        var sceneBarrier = FindAnyObjectByType<Barrier>();

        if (masterShip != null)
        {
            masterShip.OnHpChanged += m_hudViewModel.NotifyShipHpChanged;
            m_hudViewModel.OnShipSkillExecuted += masterShip.ExecuteSkill;
            m_hudViewModel.NotifyShipHpChanged(1.0f); 
        }

        m_hudViewModel.OnBattleSpeedChanged += UpdateTimeScale;
        m_hudViewModel.OnShowUpgradePanel += PauseTime;
        m_hudViewModel.OnHideUpgradePanel += ResumeTime;
        m_hudViewModel.OnShowGameOver += ShowResultFail;

        if (sceneBarrier != null)
        {
            sceneBarrier.OnBarrierChanged += m_hudViewModel.NotifyBarrierChanged;
            sceneBarrier.OnBarrierValueWeightChanged += m_hudViewModel.NotifyBarrierValueWeightChanged;
            m_hudViewModel.NotifyBarrierChanged(1.0f);
        }

        m_progressViewModel = new GameProgressViewModel();
        m_progressViewModel.ProgressData = progressDTO;
        m_progressViewModel.OnGameCleared += ShowResultClear;

        if (m_hudView == null)
        {
            m_hudView = FindAnyObjectByType<BattleHUDView>();
        }

        if (m_progressController == null)
        {
            m_progressController = FindAnyObjectByType<GameProgressController>();
        }

        if (m_hudView != null)
        {
            m_hudView.ViewModel = m_hudViewModel;
            m_hudView.ProgressViewModel = m_progressViewModel;

            if (swapManager != null)
            {
                var slotViews = m_hudView.SkillSlots;
                var characters = swapManager.Characters;
                
                if (characters != null)
                {
                    for (int i = 0; i < slotViews.Count && i < characters.Count; i++)
                    {
                        if (slotViews[i] == null)
                        {
                            continue;
                        }

                        ISkillSlotViewModel slotVM = new SkillSlotViewModel();
                        slotVM.Character = characters[i];
                        slotVM.SwapManager = swapManager;
                        
                        slotViews[i].ViewModel = slotVM;
                        slotViews[i].Initialize();
                    }
                }
            }

            m_hudView.Initialize();
        }

        var flowPanel = FindAnyObjectByType<GameFlowPanelView>(FindObjectsInactive.Include);
        if (flowPanel != null)
        {
            flowPanel.Initialize();
        }

        var upgradePanel = FindAnyObjectByType<UpgradePanelView>(FindObjectsInactive.Include);
        if (upgradePanel != null)
        {
            upgradePanel.Initialize(m_hudViewModel);
        }

        if (m_progressController != null)
        {
            m_progressController.ViewModel = m_progressViewModel;
            m_progressController.Init();
        }

        m_enemySpawner = FindAnyObjectByType<EnemySpawner>();
        if (m_enemySpawner != null)
        {
            m_enemySpawner.OnWaveChanged += m_hudViewModel.SetWave;
        }

        EnemyController.OnEnemyDead += HandleEnemyKill;
        EnemyController.OnDamageDealt += HandleDamageDealt;
    }

    private void HandleEnemyKill(string damagerID)
    {
        if (m_hudViewModel != null)
        {
            m_hudViewModel.AddKill();
        }
    }

    private void HandleDamageDealt(string damagerID, int amount)
    {
        if (m_hudViewModel != null)
        {
            m_hudViewModel.AddDamage(damagerID, amount);
        }
    }

    private void UpdateTimeScale(float speed)
    {
        if (Time.timeScale > 0f)
        {
            Time.timeScale = speed;
        }
        else
        {
            m_savedTimeScale = speed;
        }
    }

    private void PauseTime()
    {
        if (m_hudViewModel != null)
        {
            if (m_hudViewModel.BattleData != null)
            {
                m_savedTimeScale = m_hudViewModel.BattleData.BattleSpeed;
            }
        }

        Time.timeScale = 0f;
    }

    private void ResumeTime()
    {
        Time.timeScale = m_savedTimeScale;
    }

    private void ShowResultClear() => ShowResult(true);
    private void ShowResultFail() => ShowResult(false);

    private void ShowResult(bool isClear)
    {
        PauseTime();

        if (m_resultPanelView == null)
        {
            m_resultPanelView = FindAnyObjectByType<GameResultPanelView>(FindObjectsInactive.Include);
        }

        if (m_resultPanelView != null)
        {
            var resultDTO = new GameResultDTO
            {
                IsClear = isClear,
                CharacterDamages = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                CharacterIcons = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase)
            };

            if (m_hudViewModel is BattleHUDViewModel hudVM)
            {
                PlayerSwapManager swapManager = hudVM.SwapManager;
                if (swapManager != null && swapManager.Characters != null)
                {
                    for (int i = 0; i < swapManager.Characters.Count; i++)
                    {
                        var character = swapManager.Characters[i];
                        string charID = character.CharacterID;
                        if (!string.IsNullOrEmpty(charID))
                        {
                            resultDTO.CharacterDamages[charID] = 0;
                            resultDTO.CharacterIcons[charID] = character.UI_Icon;
                        }
                    }
                }

                var keys = new List<string>(hudVM.CharacterDamages.Keys);
                string mvpID = string.Empty;
                int maxDamage = -1;

                for (int i = 0; i < keys.Count; i++)
                {
                    string key = keys[i];
                    int damage = hudVM.CharacterDamages[key];
                    
                    if (!key.Equals("SHIP", StringComparison.OrdinalIgnoreCase))
                    {
                        resultDTO.CharacterDamages[key] = damage;

                        if (damage > maxDamage)
                        {
                            maxDamage = damage;
                            mvpID = key;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(mvpID) && swapManager != null && swapManager.Characters != null)
                {
                    for (int j = 0; j < swapManager.Characters.Count; j++)
                    {
                        if (swapManager.Characters[j].CharacterID.Equals(mvpID, StringComparison.OrdinalIgnoreCase))
                        {
                            resultDTO.MvpSprite = swapManager.Characters[j].UI_Icon;
                            resultDTO.MvpCharacterName = swapManager.Characters[j].CharacterName;
                            break;
                        }
                    }
                }

                // 스테이지 보상 추가 (클리어 시에만)
                if (isClear && m_itemDatabase != null)
                {
                    var allItems = m_itemDatabase.GetAllItems();
                    if (allItems != null && allItems.Count > 0)
                    {
                        int rewardCount = UnityEngine.Random.Range(3, 6);
                        for (int k = 0; k < rewardCount; k++)
                        {
                            var itemData = allItems[UnityEngine.Random.Range(0, allItems.Count)];
                            resultDTO.StageRewards.Add(new RewardItemDTO
                            {
                                ItemId = itemData.ItemId,
                                Amount = UnityEngine.Random.Range(10, 200),
                                ItemIcon = itemData.ItemIcon
                            });
                        }
                    }
                }
            }

            m_resultViewModel = new GameResultViewModel(resultDTO);
            m_resultViewModel.OnClaimDoubleReward += () => 
            {
            };
            m_resultViewModel.OnBackToMain += () => 
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

            m_resultPanelView.gameObject.SetActive(true);
            m_resultPanelView.Initialize(m_resultViewModel);
        }
    }

    public void StartGameTime()
    {
        ResumeTime();
    }

    private void Update()
    {
        if (m_hudViewModel != null)
        {
            if (Time.timeScale > 0f)
            {
                m_hudViewModel.UpdatePlayTime(Time.unscaledDeltaTime);
            }
        }
    }
}
