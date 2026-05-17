using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UserProfilePopupView : MonoBehaviour
{
    [SerializeField] private TMP_Text m_uidText;
    [SerializeField] private Image m_profileImage;
    [SerializeField] private Button m_closeButton;
    
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
        
        if (m_closeButton != null)
        {
            m_closeButton.onClick.AddListener(func_OnCloseButtonClicked);
        }

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

        // ProfileIconID를 기반으로 실제 Sprite를 로드하는 로직은 향후 리소스 매니저와 연동 가능
        // 현재는 아이디만 로그로 확인하거나 기본 처리
    }

    public void Show()
    {
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

    private void OnDestroy()
    {
        if (m_closeButton != null)
        {
            m_closeButton.onClick.RemoveAllListeners();
        }
    }
}
