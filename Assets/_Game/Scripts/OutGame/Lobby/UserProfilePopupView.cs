using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UserProfilePopupView : MonoBehaviour
{
    [SerializeField] private TMP_Text m_uidText;
    [SerializeField] private Image m_profileImage;
    
    private CanvasGroup m_canvasGroup;
    private RectTransform m_popupTransform;
    private IUserProfileViewModel m_viewModel;

    private void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        m_popupTransform = GetComponent<RectTransform>();
    }

    public void Initialize(IUserProfileViewModel viewModel)
    {
        m_viewModel = viewModel;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (m_viewModel == null)
        {
            return;
        }

        if (m_uidText != null)
        {
            m_uidText.text = $"UID: {m_viewModel.UID}";
        }
        
    }

    /// <summary>
    /// Show/Hide가 겹치면 Hide의 OnComplete가 뒤늦게 터져 방금 연 팝업을 꺼버린다.
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

    public void Hide()
    {
        KillTweens();

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

    public void func_OnCloseButtonClicked()
    {
        if (m_viewModel != null)
        {
            m_viewModel.RequestClose();
        }
    }
    
}
