using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameResultPanelView : MonoBehaviour
{
    [Header("결과 상태")] [SerializeField] private TextMeshProUGUI m_resultTitleText;

    [Header("MVP")] 
    [SerializeField] private Image m_mvpIllustrationImage;
    [SerializeField] private TextMeshProUGUI m_mvpNameText;

    [Header("데미지 로그")] 
    [SerializeField] private RectTransform m_damageLogPanel;
    [SerializeField] private Button m_damageLogToggleButton;

    [SerializeField] private Transform m_damageLogContainer;

    [SerializeField] private GameObject m_damageLogPrefab;

    [Header("보상 목록")] [SerializeField] private Transform m_rewardContainer;
    [SerializeField] private GameObject m_rewardItemPrefab;

    [Header("조작 버튼")] [SerializeField] private Button m_doubleRewardButton;

    [SerializeField] private Button m_mainScreenButton;

    [Header("연출 설정")] [SerializeField] private float m_expandDuration = 0.3f;
    [SerializeField] private Vector2 m_damageLogCollapsedSize = new Vector2(0, 300);
    [SerializeField] private Vector2 m_damageLogExpandedSize = new Vector2(0, 800);

    private IGameResultViewModel m_viewModel;
    private CanvasGroup m_canvasGroup;
    private readonly List<DamageLogEntry> m_cachedLogEntries = new List<DamageLogEntry>();

    private void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        if (m_canvasGroup == null)
        {
            m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        gameObject.SetActive(false);
    }

    public void Initialize(IGameResultViewModel viewModel)
    {
        m_viewModel = viewModel;
        m_viewModel.OnDamageLogToggled += HandleDamageLogToggle;

        BindButtons();
        SetupView();
        AnimateEntry();
    }

    private void OnDestroy()
    {
        if (m_viewModel != null)
        {
            m_viewModel.OnDamageLogToggled -= HandleDamageLogToggle;
        }
    }

    private void BindButtons()
    {
        if (m_doubleRewardButton != null)
        {
            m_doubleRewardButton.onClick.RemoveAllListeners();
            m_doubleRewardButton.onClick.AddListener(() =>
            {
                m_doubleRewardButton.interactable = false;
                m_viewModel.ClaimDoubleReward();
            });
        }

        if (m_mainScreenButton != null)
        {
            m_mainScreenButton.onClick.RemoveAllListeners();
            m_mainScreenButton.onClick.AddListener(() => { m_viewModel.BackToMain(); });
        }

        if (m_damageLogToggleButton != null)
        {
            m_damageLogToggleButton.onClick.RemoveAllListeners();
            m_damageLogToggleButton.onClick.AddListener(() => { m_viewModel.ToggleDamageLog(); });
        }
    }

    private void SetupView()
    {
        if (m_viewModel == null)
        {
            return;
        }

        if (m_resultTitleText != null)
        {
            m_resultTitleText.text = m_viewModel.IsClear ? "MISSION CLEAR" : "MISSION FAILED";
            m_resultTitleText.color = m_viewModel.IsClear ? Color.yellow : Color.red;
            m_resultTitleText.alpha = 0f;
        }

        if (m_mvpIllustrationImage != null)
        {
            m_mvpIllustrationImage.sprite = m_viewModel.MvpSprite;
            m_mvpIllustrationImage.gameObject.SetActive(m_viewModel.MvpSprite != null);
            
            Color color = m_mvpIllustrationImage.color;
            color.a = 0f;
            m_mvpIllustrationImage.color = color;
        }

        if (m_mvpNameText != null)
        {
            m_mvpNameText.text = m_viewModel.MvpCharacterName;
            m_mvpNameText.alpha = 0f;
        }

        if (m_viewModel.CharacterDamages != null && m_damageLogPrefab != null && m_damageLogContainer != null)
        {
            m_cachedLogEntries.Clear();

            var damageList = new List<KeyValuePair<string, int>>(m_viewModel.CharacterDamages);
            damageList.Sort((a, b) => b.Value.CompareTo(a.Value));

            int maxDamage = 0;
            for (int i = 0; i < damageList.Count; i++)
            {
                if (damageList[i].Value > maxDamage)
                {
                    maxDamage = damageList[i].Value;
                }
            }

            int displayCount = Mathf.Min(5, damageList.Count);
            for (int i = 0; i < displayCount; i++)
            {
                string key = damageList[i].Key;
                int damage = damageList[i].Value;

                GameObject go = Instantiate(m_damageLogPrefab, m_damageLogContainer);
                DamageLogEntry entry = go.GetComponent<DamageLogEntry>();
                if (entry != null)
                {
                    entry.SetData(key, damage, maxDamage);

                    if (m_viewModel.CharacterIcons.ContainsKey(key))
                    {
                        entry.SetPortrait(m_viewModel.CharacterIcons[key]);
                    }

                    if (i == 0 && !key.Equals("SHIP", System.StringComparison.OrdinalIgnoreCase))
                    {
                        entry.SetMvpActive(true);
                    }

                    go.transform.localScale = Vector3.zero;
                    m_cachedLogEntries.Add(entry);
                }
            }
        }

        if (m_viewModel.StageRewards != null && m_rewardItemPrefab != null && m_rewardContainer != null)
        {
            // 기존 아이템 제거 (초기화)
            foreach (Transform child in m_rewardContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < m_viewModel.StageRewards.Count; i++)
            {
                var reward = m_viewModel.StageRewards[i];
                GameObject go = Instantiate(m_rewardItemPrefab, m_rewardContainer);
                go.SetActive(true); // 명시적 활성화

                RewardItemView itemView = go.GetComponent<RewardItemView>();
                if (itemView != null)
                {
                    itemView.SetData(reward.ItemIcon, reward.Amount);
                }

                go.transform.localScale = Vector3.zero;
            }
        }
    }

    private void HandleDamageLogToggle(bool isExpanded)
    {
        if (m_damageLogPanel == null)
        {
            return;
        }

        Vector2 targetSize = isExpanded ? m_damageLogExpandedSize : m_damageLogCollapsedSize;
        m_damageLogPanel.DOSizeDelta(targetSize, m_expandDuration).SetEase(Ease.OutQuad).SetUpdate(true);

        for (int i = 0; i < m_cachedLogEntries.Count; i++)
        {
            m_cachedLogEntries[i].AnimateExpansion(isExpanded, m_expandDuration);
        }
    }

    private void AnimateEntry()
    {
        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = 0;
            m_canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
        }

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (m_resultTitleText != null)
        {
            m_resultTitleText.transform.localScale = Vector3.one * 0.5f;
            seq.Append(m_resultTitleText.DOFade(1f, 0.5f));
            seq.Join(m_resultTitleText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack));
        }

        if (m_mvpIllustrationImage != null && m_mvpIllustrationImage.gameObject.activeSelf)
        {
            Vector2 originalPos = m_mvpIllustrationImage.rectTransform.anchoredPosition;
            m_mvpIllustrationImage.rectTransform.anchoredPosition = originalPos + new Vector2(0f, 50f);

            seq.Append(m_mvpIllustrationImage.DOFade(1f, 0.8f));
            seq.Join(m_mvpIllustrationImage.rectTransform.DOAnchorPos(originalPos, 0.8f).SetEase(Ease.OutCubic));

            if (m_mvpNameText != null)
            {
                seq.Join(m_mvpNameText.DOFade(1f, 0.5f));
            }
        }

        if (m_cachedLogEntries.Count > 0)
        {
            seq.AppendInterval(0.2f);
            for (int i = 0; i < m_cachedLogEntries.Count; i++)
            {
                seq.Append(m_cachedLogEntries[i].transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            }
        }

        if (m_rewardContainer != null && m_rewardContainer.childCount > 0)
        {
            seq.AppendInterval(0.3f);
            for (int i = 0; i < m_rewardContainer.childCount; i++)
            {
                Transform child = m_rewardContainer.GetChild(i);
                seq.Append(child.DOScale(1f, 0.2f).SetEase(Ease.OutBounce));
                if (i > 0 && i % 8 == 0)
                {
                    seq.AppendInterval(0.05f);
                }
            }
        }

        if (m_doubleRewardButton != null)
        {
            seq.Append(m_doubleRewardButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).From(0f));
        }

        if (m_mainScreenButton != null)
        {
            seq.Join(m_mainScreenButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).From(0f));
        }
    }
}