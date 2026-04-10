using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SkillCutInUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private Image m_characterPortrait;
    [SerializeField] private TMP_Text m_skillNameText;
    [SerializeField] private RectTransform m_performanceRect;
    [SerializeField] private GameObject m_laserAddonEffect;

    private void Awake()
    {
        m_canvasGroup.alpha = 0f;
        m_canvasGroup.gameObject.SetActive(false);
        if (m_laserAddonEffect != null)
        {
            m_laserAddonEffect.SetActive(false);
        }
    }

    public void Show(string characterID, string skillName, SkillPerformanceType type = SkillPerformanceType.Default)
    {
        m_canvasGroup.gameObject.SetActive(true);
        m_canvasGroup.alpha = 0f;
        m_skillNameText.text = skillName;
        m_skillNameText.alpha = 1f;
        m_characterPortrait.color = Color.white;
        m_characterPortrait.transform.localScale = Vector3.one;

        m_canvasGroup.DOKill();
        m_performanceRect.DOKill();
        m_characterPortrait.DOKill();
        m_characterPortrait.transform.DOKill();
        m_skillNameText.DOKill();

        if (m_laserAddonEffect != null)
        {
            m_laserAddonEffect.transform.DOKill();
        }

        bool isLaserStyle = (type == SkillPerformanceType.Laser);

        if (isLaserStyle)
        {
            if (m_laserAddonEffect != null)
            {
                m_laserAddonEffect.SetActive(true);
                m_laserAddonEffect.transform.localScale = Vector3.zero;
                var image = m_laserAddonEffect.GetComponent<Image>();
                if (image != null)
                {
                    var c = image.color;
                    c.a = 0f;
                    image.color = c;
                }
            }

            Sequence laserSeq = DOTween.Sequence().SetUpdate(true);
            
            m_performanceRect.anchoredPosition = new Vector2(-1500f, 0f);
            m_canvasGroup.blocksRaycasts = false;

            laserSeq.AppendInterval(0.3f);
            laserSeq.Append(m_canvasGroup.DOFade(1f, 0.2f).SetUpdate(true));
            laserSeq.Join(m_performanceRect.DOAnchorPos(Vector2.zero, 0.4f).SetEase(Ease.OutCubic).SetUpdate(true));
            laserSeq.Append(m_characterPortrait.transform.DOShakePosition(0.5f, 10f, 20).SetUpdate(true));
            laserSeq.AppendCallback(() =>
            {
                if (m_laserAddonEffect != null)
                {
                    var image = m_laserAddonEffect.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
                    }

                    m_laserAddonEffect.transform.localScale = new Vector3(0f, 1f, 1f);
                    m_laserAddonEffect.transform.DOScaleX(1f, 0.2f).SetEase(Ease.OutExpo).SetUpdate(true);
                }
            });
            laserSeq.AppendInterval(0.6f);
            
            laserSeq.Append(m_characterPortrait.DOFade(0f, 0.2f).SetUpdate(true));
            laserSeq.Join(m_skillNameText.DOFade(0f, 0.2f).SetUpdate(true));
            if (m_laserAddonEffect != null)
            {
                laserSeq.Join(m_laserAddonEffect.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true));
            }
            
            laserSeq.Append(m_performanceRect.DOAnchorPos(new Vector2(1500f, 0f), 0.4f).SetEase(Ease.InCubic).SetUpdate(true));
            laserSeq.Append(m_canvasGroup.DOFade(0f, 0.2f).SetUpdate(true));
            
            laserSeq.OnComplete(() =>
            {
                if (m_laserAddonEffect != null)
                {
                    m_laserAddonEffect.SetActive(false);
                }
                m_canvasGroup.gameObject.SetActive(false);
            });
        }
        else
        {
            m_canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
            m_performanceRect.anchoredPosition = new Vector2(-1000f, 0f);
            m_performanceRect.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);

            DOVirtual.DelayedCall(1.2f, () =>
            {
                if (m_canvasGroup != null)
                {
                    m_canvasGroup.DOFade(0f, 0.3f).SetUpdate(true).OnComplete(() =>
                    {
                        if (m_canvasGroup != null) m_canvasGroup.gameObject.SetActive(false);
                    });
                }
            }).SetUpdate(true);
        }
    }
}
