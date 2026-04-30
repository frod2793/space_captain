using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameResultPanelView : MonoBehaviour
{
    [Header("결과 상태")]
    [SerializeField] private TextMeshProUGUI m_resultTitleText;
    
    [Header("MVP")]
    [FormerlySerializedAs("m_mvpIconImage")]
    [SerializeField] private Image m_mvpIllustrationImage;
    [SerializeField] private TextMeshProUGUI m_mvpNameText;
    
    [Header("데미지 로그")]
    [SerializeField] private RectTransform m_damageLogPanel;
    [SerializeField] private Button m_damageLogToggleButton;
    [FormerlySerializedAs("m_characterStatsContainer")]
    [SerializeField] private Transform m_damageLogContainer;
    [FormerlySerializedAs("m_characterStatPrefab")]
    [SerializeField] private GameObject m_damageLogPrefab;
    
    [Header("보상 목록")]
    [SerializeField] private Transform m_rewardContainer;
    [SerializeField] private GameObject m_rewardItemPrefab;
    
    [Header("조작 버튼")]
    [FormerlySerializedAs("m_retryButton")]
    [SerializeField] private Button m_doubleRewardButton;
    [FormerlySerializedAs("m_lobbyButton")]
    [SerializeField] private Button m_mainScreenButton;
    
    [Header("연출 설정")]
    [SerializeField] private float m_fadeDuration = 0.5f;
    [SerializeField] private float m_expandDuration = 0.3f;
    [SerializeField] private Vector2 m_damageLogCollapsedSize = new Vector2(0, 300);
    [SerializeField] private Vector2 m_damageLogExpandedSize = new Vector2(0, 800);

    private IGameResultViewModel m_viewModel;
    private CanvasGroup m_canvasGroup;

    private void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        if (m_canvasGroup == null)
        {
            m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
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
            m_doubleRewardButton.onClick.AddListener(() =>
            {
                m_doubleRewardButton.interactable = false;
                m_viewModel.ClaimDoubleReward();
            });
        }
        
        if (m_mainScreenButton != null)
        {
            m_mainScreenButton.onClick.AddListener(() =>
            {
                m_viewModel.BackToMain();
            });
        }

        if (m_damageLogToggleButton != null)
        {
            m_damageLogToggleButton.onClick.AddListener(() =>
            {
                m_viewModel.ToggleDamageLog();
            });
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
            m_resultTitleText.text = m_viewModel.IsClear ? "VICTORY" : "DEFEAT";
            m_resultTitleText.color = m_viewModel.IsClear ? Color.yellow : Color.red;
        }

        if (m_mvpIllustrationImage != null)
        {
            m_mvpIllustrationImage.sprite = m_viewModel.MvpSprite;
            m_mvpIllustrationImage.gameObject.SetActive(m_viewModel.MvpSprite != null);
        }

        if (m_mvpNameText != null)
        {
            m_mvpNameText.text = m_viewModel.MvpCharacterName;
        }

        if (m_viewModel.CharacterDamages != null && m_damageLogPrefab != null && m_damageLogContainer != null)
        {
            int maxDamage = 0;
            var values = new List<int>(m_viewModel.CharacterDamages.Values);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] > maxDamage)
                {
                    maxDamage = values[i];
                }
            }

            var keys = new List<string>(m_viewModel.CharacterDamages.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                int damage = m_viewModel.CharacterDamages[key];

                GameObject go = Instantiate(m_damageLogPrefab, m_damageLogContainer);
                DamageLogEntry entry = go.GetComponent<DamageLogEntry>();
                if (entry != null)
                {
                    entry.SetData(key, damage, maxDamage);
                }
            }
        }

        if (m_viewModel.StageRewards != null && m_rewardItemPrefab != null && m_rewardContainer != null)
        {
            for (int i = 0; i < m_viewModel.StageRewards.Count; i++)
            {
                var reward = m_viewModel.StageRewards[i];
                GameObject go = Instantiate(m_rewardItemPrefab, m_rewardContainer);
                RewardItemView itemView = go.GetComponent<RewardItemView>();
                if (itemView != null)
                {
                    itemView.SetData(reward.ItemIcon, reward.Amount);
                }
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
    }

    private void AnimateEntry()
    {
        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = 0;
        }
        
        transform.localScale = Vector3.one * 0.8f;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        if (m_canvasGroup != null)
        {
            seq.Append(m_canvasGroup.DOFade(1f, m_fadeDuration));
        }
        
        seq.Join(transform.DOScale(1f, m_fadeDuration).SetEase(Ease.OutBack));

        if (m_mvpIllustrationImage != null && m_mvpIllustrationImage.gameObject.activeSelf)
        {
            m_mvpIllustrationImage.transform.localScale = Vector3.zero;
            seq.Append(m_mvpIllustrationImage.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
        }

        if (m_rewardContainer != null && m_rewardContainer.childCount > 0)
        {
            for (int i = 0; i < m_rewardContainer.childCount; i++)
            {
                Transform child = m_rewardContainer.GetChild(i);
                child.localScale = Vector3.zero;
                seq.Append(child.DOScale(1f, 0.1f).SetEase(Ease.OutBounce));
            }
        }
    }
}
