using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class DamageLogEntry : MonoBehaviour
{
    [Header("기본 UI")]
    [Tooltip("캐릭터 아이콘")]
    [SerializeField] private Image m_characterIcon;
    [Tooltip("캐릭터 이름")]
    [SerializeField] private TextMeshProUGUI m_characterNameText;
    [Tooltip("총 데미지 수치")]
    [SerializeField] private TextMeshProUGUI m_damageText;
    [Tooltip("데미지 비중 슬라이더")]
    [SerializeField] private Slider m_damageSlider;

    [SerializeField] private Image m_MvpImage;

    [Header("확장 UI")]
    [Tooltip("MVP 크라운 마크")]
    [SerializeField] private GameObject m_mvpCrownMark;
    [Tooltip("상세 정보 그룹 (CanvasGroup)")]
    [SerializeField] private CanvasGroup m_detailGroup;
    [Tooltip("데미지 비중(%) 텍스트")]
    [SerializeField] private TextMeshProUGUI m_detailPercentageText;

    [Header("설정")]
    [Tooltip("애니메이션 대상 RectTransform")]
    [SerializeField] private RectTransform m_rectTransform;
    [Tooltip("축소 시 높이")]
    [SerializeField] private float m_collapsedHeight = 80f;
    [Tooltip("확장 시 높이")]
    [SerializeField] private float m_expandedHeight = 150f;
    [Tooltip("확장 시 내부 요소 배율")]
    [SerializeField] private float m_expandScaleMultiplier = 1.15f;

    private void Awake()
    {
        if (m_characterNameText != null)
        {
            Color c = m_characterNameText.color;
            c.a = 0f;
            m_characterNameText.color = c;
        }
    }

    public void SetData(string characterID, int damage, int maxDamage)
    {
        if (m_characterNameText != null)
        {
            m_characterNameText.text = characterID.ToUpper();
        }

        if (m_damageText != null)
        {
            m_damageText.text = $"{damage:N0}";
        }

        if (m_damageSlider != null)
        {
            m_damageSlider.value = maxDamage > 0 ? (float)damage / maxDamage : 0f;
        }

        if (m_detailGroup != null)
        {
            m_detailGroup.alpha = 0f;
        }

        if (m_MvpImage != null)
        {
            m_MvpImage.gameObject.SetActive(false);
        }
    }

    public void SetPortrait(Sprite portrait)
    {
        if (m_characterIcon != null)
        {
            if (portrait != null)
            {
                m_characterIcon.sprite = portrait;
                m_characterIcon.gameObject.SetActive(true);
                
                // 프리팹의 알파값이 0일 경우를 대비해 1로 복구
                Color c = m_characterIcon.color;
                c.a = 1f;
                m_characterIcon.color = c;
            }
            else
            {
                m_characterIcon.gameObject.SetActive(false);
            }
        }
    }

    public void SetMvpActive(bool isActive)
    {
        if (m_MvpImage != null)
        {
            m_MvpImage.gameObject.SetActive(isActive);
        }
    }

    public void SetPercentage(float percentage)
    {
        if (m_detailPercentageText != null)
        {
            m_detailPercentageText.text = $"{percentage:F1}%";
        }
    }

    public void AnimateExpansion(bool isExpanded, float duration)
    {
        if (m_rectTransform == null)
        {
            m_rectTransform = GetComponent<RectTransform>();
        }

        float targetHeight = isExpanded ? m_expandedHeight : m_collapsedHeight;
        m_rectTransform.DOSizeDelta(new Vector2(m_rectTransform.sizeDelta.x, targetHeight), duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        float targetScale = isExpanded ? m_expandScaleMultiplier : 1f;
        
        if (m_characterIcon != null)
        {
            m_characterIcon.transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        }
            
        if (m_damageSlider != null)
        {
            m_damageSlider.transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        }
            
        if (m_damageText != null)
        {
            m_damageText.transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        if (m_characterNameText != null)
        {
            m_characterNameText.DOFade(isExpanded ? 1f : 0f, duration).SetEase(Ease.InOutSine).SetUpdate(true);
            m_characterNameText.transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        if (m_detailGroup != null)
        {
            m_detailGroup.DOFade(isExpanded ? 1f : 0f, duration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }
    }
}
