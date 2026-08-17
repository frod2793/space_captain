using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSlotView : MonoBehaviour
{
    [SerializeField] private Image m_iconImage;
    [SerializeField] private Image m_frameImage;
    [SerializeField] private Button m_button;
    [SerializeField] private TMP_Text m_nameText;

    private Action m_onClicked;

    private void Awake()
    {
        if (m_button != null)
        {
            m_button.onClick.AddListener(func_OnClicked);
        }
    }

    private void OnDestroy()
    {
        if (m_button != null)
        {
            m_button.onClick.RemoveAllListeners();
        }
    }

    /// <summary>아이콘이 null이면 빈 슬롯으로 그린다.</summary>
    public void Bind(Sprite icon, Color frameColor, Action onClicked)
    {
        m_onClicked = onClicked;

        if (m_iconImage != null)
        {
            m_iconImage.sprite = icon;
            m_iconImage.enabled = icon != null;
        }

        if (m_frameImage != null)
        {
            m_frameImage.color = frameColor;
        }
    }

    public void SetLabel(string text)
    {
        if (m_nameText != null)
        {
            m_nameText.text = text;
        }
    }

    private void func_OnClicked()
    {
        m_onClicked?.Invoke();
    }
}
