using UnityEngine;
using System.Collections.Generic;

public class BattleSceneInitializer : MonoBehaviour
{
    [SerializeField] private BattleHUDView m_hudView;
    [SerializeField] private GameProgressController m_progressController;

    private IBattleHUDViewModel m_hudViewModel;
    private IGameProgressViewModel m_progressViewModel;
    private EnemySpawner m_enemySpawner;

    private void Awake()
    {
        InitializeScene();
    }

    private void OnDestroy()
    {
        EnemyController.OnEnemyDead -= HandleEnemyKill;
        
        if (m_enemySpawner != null && m_hudViewModel != null)
        {
            m_enemySpawner.OnWaveChanged -= m_hudViewModel.SetWave;
        }

        var masterShip = FindAnyObjectByType<MasterShip>();
        if (masterShip != null && m_hudViewModel != null)
        {
            m_hudViewModel.OnShipSkillExecuted -= masterShip.ExecuteGuidedMissile;
        }
    }

    private void InitializeScene()
    {
        var battleDTO = new BattleProgressDTO();
        var progressDTO = new ProgressDTO();
        var swapManager = FindAnyObjectByType<PlayerSwapManager>();

        if (swapManager != null)
        {
            var barrier = FindAnyObjectByType<Barrier>();
            var playerHpBar = FindAnyObjectByType<PlayerHpBar>();
            swapManager.Barrier = barrier;
            swapManager.PlayerHUD = playerHpBar;
        }

        m_hudViewModel = new BattleHUDViewModel();
        m_hudViewModel.BattleData = battleDTO;
        m_hudViewModel.SwapManager = swapManager;

        var masterShip = FindAnyObjectByType<MasterShip>();
        var sceneBarrier = FindAnyObjectByType<Barrier>();

        if (masterShip != null)
        {
            masterShip.OnHpChanged += m_hudViewModel.NotifyShipHpChanged;
            m_hudViewModel.OnShipSkillExecuted += masterShip.ExecuteGuidedMissile;
            m_hudViewModel.NotifyShipHpChanged(1.0f); 
        }

        if (sceneBarrier != null)
        {
            sceneBarrier.OnBarrierChanged += m_hudViewModel.NotifyBarrierChanged;
            sceneBarrier.OnBarrierValueWeightChanged += m_hudViewModel.NotifyBarrierValueWeightChanged;
            m_hudViewModel.NotifyBarrierChanged(1.0f);
        }

        m_progressViewModel = new GameProgressViewModel();
        m_progressViewModel.ProgressData = progressDTO;

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
    }

    private void HandleEnemyKill()
    {
        if (m_hudViewModel != null)
        {
            m_hudViewModel.AddKill();
        }
    }
}
