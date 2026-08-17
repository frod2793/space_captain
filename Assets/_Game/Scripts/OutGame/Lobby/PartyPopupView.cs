using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PartyPopupView : MonoBehaviour
{
    [Header("편성 패널")]
    [SerializeField] private CharacterSlotView[] m_slotViews;
    [SerializeField] private TMP_Text m_combatPowerText;
    [SerializeField] private Button m_autoArrangeButton;
    [SerializeField] private Button m_closeButton;

    [Header("선택 패널")]
    [SerializeField] private GameObject m_selectPanel;
    [SerializeField] private Transform m_gridContainer;
    [SerializeField] private CharacterSlotView m_cellPrefab;
    [SerializeField] private Button m_selectCloseButton;

    [Header("테두리 색")]
    [SerializeField] private Color m_fieldColor = Color.red;
    [SerializeField] private Color m_reserveColor = Color.green;
    [SerializeField] private Color m_emptyColor = Color.white;

    private IPartyViewModel m_viewModel;
    private CanvasGroup m_canvasGroup;
    private RectTransform m_popupTransform;
    private readonly List<CharacterSlotView> m_cells = new List<CharacterSlotView>();

    private void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        m_popupTransform = GetComponent<RectTransform>();
    }

    public void Initialize(IPartyViewModel viewModel)
    {
        m_viewModel = viewModel;

        if (m_viewModel == null)
        {
            return;
        }

        m_viewModel.OnDeckChanged += Refresh;
        m_viewModel.OnSelectRequested += ShowSelectPanel;
        m_viewModel.OnSelectClosed += HideSelectPanel;

        if (m_autoArrangeButton != null)
        {
            m_autoArrangeButton.onClick.AddListener(func_OnAutoArrangeClicked);
        }

        if (m_closeButton != null)
        {
            m_closeButton.onClick.AddListener(func_OnCloseClicked);
        }

        if (m_selectCloseButton != null)
        {
            m_selectCloseButton.onClick.AddListener(func_OnSelectCloseClicked);
        }

        BuildGrid();
        Refresh();
        HideSelectPanel();
    }

    private void OnDestroy()
    {
        if (m_viewModel != null)
        {
            m_viewModel.OnDeckChanged -= Refresh;
            m_viewModel.OnSelectRequested -= ShowSelectPanel;
            m_viewModel.OnSelectClosed -= HideSelectPanel;
        }

        if (m_autoArrangeButton != null)
        {
            m_autoArrangeButton.onClick.RemoveAllListeners();
        }

        if (m_closeButton != null)
        {
            m_closeButton.onClick.RemoveAllListeners();
        }

        if (m_selectCloseButton != null)
        {
            m_selectCloseButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 선택 그리드는 보유 목록이 바뀌지 않으므로 한 번만 생성하고
    /// 이후에는 테두리 색만 다시 칠한다.
    /// </summary>
    private void BuildGrid()
    {
        if (m_gridContainer == null || m_cellPrefab == null)
        {
            return;
        }

        for (int i = 0; i < m_cells.Count; i++)
        {
            if (m_cells[i] != null)
            {
                Destroy(m_cells[i].gameObject);
            }
        }
        m_cells.Clear();

        IReadOnlyList<CharacterDataSO> all = m_viewModel.AllCharacters;

        for (int i = 0; i < all.Count; i++)
        {
            CharacterSlotView cell = Instantiate(m_cellPrefab, m_gridContainer);
            m_cells.Add(cell);
        }
    }

    private void Refresh()
    {
        RefreshSlots();
        RefreshGrid();
        RefreshCombatPower();
    }

    private void RefreshSlots()
    {
        if (m_slotViews == null)
        {
            return;
        }

        IReadOnlyList<CharacterDataSO> deck = m_viewModel.Deck;

        for (int i = 0; i < m_slotViews.Length; i++)
        {
            if (m_slotViews[i] == null)
            {
                continue;
            }

            CharacterDataSO data = i < deck.Count ? deck[i] : null;
            int slot = i;

            m_slotViews[i].Bind(
                data != null ? data.UI_Icon : null,
                GetSlotColor(slot),
                () => m_viewModel.BeginSelect(slot));

            m_slotViews[i].SetLabel(data != null ? data.CharacterName : string.Empty);
        }
    }

    private void RefreshGrid()
    {
        IReadOnlyList<CharacterDataSO> all = m_viewModel.AllCharacters;

        for (int i = 0; i < m_cells.Count && i < all.Count; i++)
        {
            if (m_cells[i] == null)
            {
                continue;
            }

            CharacterDataSO data = all[i];
            string id = data.CharacterID;

            m_cells[i].Bind(
                data.UI_Icon,
                GetDeckColor(data),
                () => m_viewModel.PickCharacter(id));

            m_cells[i].SetLabel(data.CharacterName);
        }
    }

    private void RefreshCombatPower()
    {
        if (m_combatPowerText != null)
        {
            m_combatPowerText.text = m_viewModel.CombatPower.ToString("N0");
        }
    }

    /// <summary>편성 슬롯 자체의 색. 0~2 필드, 3~4 예비.</summary>
    private Color GetSlotColor(int slot)
    {
        return slot < PartyViewModel.FIELD_SIZE ? m_fieldColor : m_reserveColor;
    }

    /// <summary>선택 그리드 칸의 색. 편성 상태에 따라 달라진다.</summary>
    private Color GetDeckColor(CharacterDataSO data)
    {
        IReadOnlyList<CharacterDataSO> deck = m_viewModel.Deck;

        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i] == data)
            {
                return i < PartyViewModel.FIELD_SIZE ? m_fieldColor : m_reserveColor;
            }
        }

        return m_emptyColor;
    }

    private void ShowSelectPanel()
    {
        if (m_selectPanel != null)
        {
            m_selectPanel.SetActive(true);
        }
    }

    private void HideSelectPanel()
    {
        if (m_selectPanel != null)
        {
            m_selectPanel.SetActive(false);
        }
    }

    public void Show()
    {
        KillTweens();
        gameObject.SetActive(true);

        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = 0;
            m_canvasGroup.DOFade(1, 0.3f).SetEase(Ease.OutQuad);
        }

        if (m_popupTransform != null)
        {
            m_popupTransform.localScale = Vector3.one * 0.8f;
            m_popupTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    /// <summary>
    /// Show/Hide가 0.2~0.3초 안에 겹치면 Hide의 OnComplete가 뒤늦게 터져
    /// 방금 연 팝업을 꺼버린다. 진행 중인 트윈을 먼저 죽인다.
    /// </summary>
    private void KillTweens()
    {
        if (m_canvasGroup != null)
        {
            m_canvasGroup.DOKill();
        }

        if (m_popupTransform != null)
        {
            m_popupTransform.DOKill();
        }
    }

    public void Hide()
    {
        KillTweens();

        if (m_viewModel != null)
        {
            m_viewModel.Commit();
            // 슬롯을 고른 채로 닫으면 PendingSlot이 남아 다음에 열 때 오배치된다
            m_viewModel.CancelSelect();
        }

        HideSelectPanel();

        if (m_canvasGroup != null)
        {
            m_canvasGroup.DOFade(0, 0.2f).SetEase(Ease.InQuad);
        }

        if (m_popupTransform != null)
        {
            m_popupTransform.DOScale(Vector3.one * 0.8f, 0.2f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void func_OnAutoArrangeClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.AutoArrange();
        }
    }

    private void func_OnCloseClicked()
    {
        Hide();
    }

    private void func_OnSelectCloseClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.CancelSelect();
        }
    }
}
