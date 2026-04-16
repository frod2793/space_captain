using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

public class BattleHUDView : MonoBehaviour
{
    [Header("슬라이더")] [SerializeField] private Slider m_progressSlider;
    [SerializeField] private Slider m_hpSlider;
    [SerializeField] private Slider m_barrierSlider;
    [SerializeField] private Slider m_bossHpSlider;
    [SerializeField] private Slider m_expSlider;

    [Header("패널")]
    private List<ShipSkillButton> m_shipSkillButtons = new List<ShipSkillButton>();

    [Header("텍스트")] [SerializeField] private TMP_Text m_killCountText;
    [SerializeField] private TMP_Text m_speedText;
    [SerializeField] private TMP_Text m_waveText;
    [SerializeField] private TMP_Text m_levelText;
    [SerializeField] private TMP_Text m_playTimeText;
    [SerializeField] private TMP_Text m_barrierText;

    [Header("스킬 슬롯")] 
    [SerializeField] private List<SkillSlotView> m_skillSlots = new List<SkillSlotView>();
    [SerializeField] private Vector2[] m_fieldSlotPositions = new Vector2[3];
    [SerializeField] private Vector2[] m_reserveSlotPositions = new Vector2[2];

    public IReadOnlyList<SkillSlotView> SkillSlots => m_skillSlots;

    [Header("스왑 및 효과")] [SerializeField] private SkillCutInUI m_skillCutInUI;
    [SerializeField] private Image m_swapCooldownFill;

    public IBattleHUDViewModel ViewModel { get; set; }
    public IGameProgressViewModel ProgressViewModel { get; set; }
    private float m_savedTimeScale = 1f;
    private PlayerSwapManager m_cachedSwapManager;
    private bool m_isSwapCooldownActive;

    private Dictionary<PlayerCharacterController, SkillSlotView> m_characterToSlotMap =
        new Dictionary<PlayerCharacterController, SkillSlotView>();

    private Dictionary<PlayerCharacterController, int> m_characterToIndexMap =
        new Dictionary<PlayerCharacterController, int>();

    private const float COOLDOWN_EPSILON = 0.002f;

    private sealed class SlotXComparer : IComparer<SkillSlotView>
    {
        public static readonly SlotXComparer Instance = new SlotXComparer();

        public int Compare(SkillSlotView x, SkillSlotView y)
        {
            return x.Rect.anchoredPosition.x.CompareTo(y.Rect.anchoredPosition.x);
        }
    }

    private struct ViewState
    {
        public int KillCount;
        public int Level;
        public float ExpRatio;
        public int Wave;
        public int PlayTimeMinutes;
        public int PlayTimeSeconds;
        public float BattleSpeed;
        public float HpRatio;
        public float BarrierRatio;
        public int BarrierCurrent;
        public float ProgressRatio;
        public float SwapRatio;
    }

    private ViewState m_lastState = new ViewState
    {
        KillCount = -1, Level = -1, ExpRatio = -1f, Wave = -1, PlayTimeMinutes = -1, PlayTimeSeconds = -1,
        BattleSpeed = -1f, HpRatio = -1f, BarrierRatio = -1f, BarrierCurrent = -1, ProgressRatio = -1f, SwapRatio = -1f
    };

    public void Initialize()
    {
        if (ViewModel == null)
        {
            return;
        }

        UpdateKillCountUI(ViewModel.BattleData.TotalKillCount);
        UpdateLevelUI(ViewModel.BattleData.CurrentLevel + 1);
        UpdateExpUI(0f);
        UpdateWaveText(ViewModel.BattleData.CurrentWave);
        UpdatePlayTimeUI(ViewModel.BattleData.PlayTime);
        UpdateBattleSpeedUI(ViewModel.BattleData.BattleSpeed);

        if (ViewModel.SwapManager != null)
        {
            m_cachedSwapManager = ViewModel.SwapManager;
        }

        FindShipSkillButtons();
        BindEvents();
    }

    private void FindShipSkillButtons()
    {
        m_shipSkillButtons.Clear();
        m_shipSkillButtons.AddRange(FindObjectsByType<ShipSkillButton>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    private void BindEvents()
    {
        ViewModel.OnTotalKillCountChanged += UpdateKillCountUI;
        ViewModel.OnLevelChanged += UpdateLevelUI;
        ViewModel.OnExpRatioChanged += UpdateExpUI;
        ViewModel.OnWaveChanged += UpdateWaveText;
        ViewModel.OnPlayTimeChanged += UpdatePlayTimeUI;
        ViewModel.OnBattleSpeedChanged += UpdateBattleSpeedUI;
        ViewModel.OnShipHpChanged += UpdateHpBar;
        ViewModel.OnBarrierChanged += UpdateBarrierBar;
        ViewModel.OnBarrierValueWeightChanged += UpdateBarrierText;

        if (ProgressViewModel != null)
        {
            ProgressViewModel.OnProgressChanged += SetProgressRatio;
        }

        if (m_cachedSwapManager != null)
        {
            m_cachedSwapManager.OnCharactersInitialized += SyncAllSkillSlots;
            m_cachedSwapManager.OnSwapStarted += HandleSwapStarted;
            m_cachedSwapManager.OnSwapCompleted += HandleSwapCompleted;
            m_cachedSwapManager.OnSwapCooldownChanged += UpdateSwapCooldownUI;
        }

        SyncAllSkillSlots();

        for (int i = 0; i < m_shipSkillButtons.Count; i++)
        {
            var skillButton = m_shipSkillButtons[i];
            if (skillButton != null && skillButton.Button != null)
            {
                int skillIndex = skillButton.SkillIndex;
                skillButton.Button.onClick.AddListener(() => ViewModel.ExecuteShipSkill(skillIndex));
            }
        }
    }

    private void UnbindEvents()
    {
        if (ViewModel != null)
        {
            ViewModel.OnTotalKillCountChanged -= UpdateKillCountUI;
            ViewModel.OnLevelChanged -= UpdateLevelUI;
            ViewModel.OnExpRatioChanged -= UpdateExpUI;
            ViewModel.OnWaveChanged -= UpdateWaveText;
            ViewModel.OnPlayTimeChanged -= UpdatePlayTimeUI;
            ViewModel.OnBattleSpeedChanged -= UpdateBattleSpeedUI;
            ViewModel.OnShipHpChanged -= UpdateHpBar;
            ViewModel.OnBarrierChanged -= UpdateBarrierBar;
            ViewModel.OnBarrierValueWeightChanged -= UpdateBarrierText;

            if (ProgressViewModel != null)
            {
                ProgressViewModel.OnProgressChanged -= SetProgressRatio;
            }

            if (m_cachedSwapManager != null)
            {
                m_cachedSwapManager.OnCharactersInitialized -= SyncAllSkillSlots;
                m_cachedSwapManager.OnSwapStarted -= HandleSwapStarted;
                m_cachedSwapManager.OnSwapCompleted -= HandleSwapCompleted;
                m_cachedSwapManager.OnSwapCooldownChanged -= UpdateSwapCooldownUI;
            }

            for (int i = 0; i < m_shipSkillButtons.Count; i++)
            {
                var skillButton = m_shipSkillButtons[i];
                if (skillButton != null && skillButton.Button != null)
                {
                    skillButton.Button.onClick.RemoveAllListeners();
                }
            }
        }
    }

    private Vector2 GetPositionByIndex(int index)
    {
        if (index < 0)
        {
            return Vector2.zero;
        }

        if (index < m_fieldSlotPositions.Length)
        {
            return m_fieldSlotPositions[index];
        }

        int reserveIdx = index - m_fieldSlotPositions.Length;
        if (m_reserveSlotPositions != null && reserveIdx >= 0 && reserveIdx < m_reserveSlotPositions.Length)
        {
            return m_reserveSlotPositions[reserveIdx];
        }

        return Vector2.zero;
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    public void SetProgressRatio(float ratio)
    {
        if (m_lastState.ProgressRatio != ratio)
        {
            m_progressSlider.value = m_lastState.ProgressRatio = ratio;
        }
    }

    public void UpdateHpBar(float ratio)
    {
        if (m_lastState.HpRatio != ratio)
        {
            m_hpSlider.value = m_lastState.HpRatio = ratio;
        }
    }

    public void UpdateBarrierBar(float ratio)
    {
        if (m_lastState.BarrierRatio != ratio)
        {
            m_lastState.BarrierRatio = ratio;
            m_barrierSlider.gameObject.SetActive(ratio > 0f);
            m_barrierSlider.value = ratio;
        }
    }

    public void UpdateBarrierText(int current, int max)
    {
        if (m_lastState.BarrierCurrent != current)
        {
            m_lastState.BarrierCurrent = current;
            m_barrierText.gameObject.SetActive(current > 0);
            m_barrierText.text = $"{current} / {max}";
        }
    }

    public void UpdateBossHpBar(float ratio)
    {
        if (m_bossHpSlider != null)
        {
            m_bossHpSlider.gameObject.SetActive(ratio > 0);
            m_bossHpSlider.value = ratio;
        }
    }

    private void UpdateKillCountUI(int totalKills)
    {
        if (m_lastState.KillCount != totalKills)
        {
            m_lastState.KillCount = totalKills;
            m_killCountText.SetText("잡은놈수: {0}", totalKills);
        }
    }

    private void UpdateLevelUI(int level)
    {
        if (m_lastState.Level != level)
        {
            m_lastState.Level = level;
            m_levelText.SetText("LV.{0}", level);
        }
    }

    private void UpdateExpUI(float ratio)
    {
        if (m_lastState.ExpRatio != ratio)
        {
            m_lastState.ExpRatio = ratio;
            m_expSlider.DOKill();
            m_expSlider.DOValue(ratio, 0.3f).SetEase(Ease.OutQuad);
        }
    }

    private void UpdateWaveText(int wave)
    {
        if (m_lastState.Wave != wave)
        {
            m_lastState.Wave = wave;
            m_waveText.SetText("{0} 웨이브", wave);
        }
    }

    private void UpdatePlayTimeUI(float playTime)
    {
        int minutes = Mathf.FloorToInt(playTime / 60f);
        int seconds = Mathf.FloorToInt(playTime % 60f);

        if (m_lastState.PlayTimeMinutes != minutes || m_lastState.PlayTimeSeconds != seconds)
        {
            m_lastState.PlayTimeMinutes = minutes;
            m_lastState.PlayTimeSeconds = seconds;
            m_playTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void UpdateBattleSpeedUI(float speed)
    {
        if (m_lastState.BattleSpeed != speed)
        {
            m_lastState.BattleSpeed = speed;
            m_speedText.text = $"x{speed:F1}";

            if (Time.timeScale > 0)
            {
                Time.timeScale = speed;
            }
            else
            {
                m_savedTimeScale = speed;
            }
        }
    }

    private void UpdateSwapCooldownUI(float ratio)
    {
        if (Mathf.Abs(m_lastState.SwapRatio - ratio) > COOLDOWN_EPSILON)
        {
            m_lastState.SwapRatio = m_swapCooldownFill.fillAmount = ratio;

            bool isActive = ratio > 0;
            if (m_isSwapCooldownActive != isActive)
            {
                m_isSwapCooldownActive = isActive;
                m_swapCooldownFill.gameObject.SetActive(isActive);
            }
        }
    }

    private void HandleSwapStarted(PlayerCharacterController entering, PlayerCharacterController leaving)
    {
        if (!m_characterToSlotMap.TryGetValue(entering, out var enteringSlot) ||
            !m_characterToSlotMap.TryGetValue(leaving, out var leavingSlot))
        {
            return;
        }

        if (!m_characterToIndexMap.TryGetValue(entering, out int enteringIdx) ||
            !m_characterToIndexMap.TryGetValue(leaving, out int leavingIdx))
        {
            return;
        }

        enteringSlot.Rect.DOKill();
        leavingSlot.Rect.DOKill();

        float duration = m_cachedSwapManager.SwapDuration;

        enteringSlot.Rect.DOAnchorPos(GetPositionByIndex(leavingIdx), duration).SetEase(Ease.InOutCubic);
        leavingSlot.Rect.DOAnchorPos(GetPositionByIndex(enteringIdx), duration).SetEase(Ease.InOutCubic);
    }

    private void HandleSwapCompleted(PlayerCharacterController activeCharacter)
    {
        Invoke(nameof(SyncAllSkillSlots), 0f);
    }

    public void UI_ToggleBattleSpeed()
    {
        if (ViewModel != null)
        {
            ViewModel.ToggleBattleSpeed();
        }
    }

    private void SyncAllSkillSlots()
    {
        if (m_cachedSwapManager == null || m_cachedSwapManager.Characters == null)
        {
            return;
        }

        m_characterToSlotMap.Clear();
        m_characterToIndexMap.Clear();

        m_skillSlots.Sort(SlotXComparer.Instance);

        var characters = m_cachedSwapManager.Characters;
        for (int i = 0; i < m_skillSlots.Count; i++)
        {
            var slot = m_skillSlots[i];
            if (slot == null)
            {
                continue;
            }

            if (i < characters.Count)
            {
                var targetCharacter = characters[i];
                if (targetCharacter != null)
                {
                    if (slot.ViewModel == null)
                    {
                        slot.ViewModel = new SkillSlotViewModel { SwapManager = m_cachedSwapManager };
                        slot.Initialize();
                    }

                    slot.UpdateCharacter(targetCharacter);
                    m_characterToSlotMap[targetCharacter] = slot;
                    m_characterToIndexMap[targetCharacter] = i;
                }
            }

            slot.Rect.anchoredPosition = GetPositionByIndex(i);
        }
    }
}