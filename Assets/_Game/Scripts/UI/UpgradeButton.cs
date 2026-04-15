using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class UpgradeButton : MonoBehaviour
{
    [SerializeField] private Button m_button;
    [SerializeField] private TMP_Text m_titleText;
    [SerializeField] private TMP_Text m_descriptionText;

    private int m_characterIndex;  // 0 = a , 1= b, 2 = c
    public Action<int> OnSelect { get; set; }

    public void Initialize()
    {
        if (m_button != null)
        {
            m_button.onClick.RemoveAllListeners();
            m_button.onClick.AddListener(HandleClick);
        }
    }

    public void SetData(UpgradeOptionDTO data)
    {
        if (data == null)
        {
            return;
        }

        m_characterIndex = data.CharacterIndex;

        if (m_titleText != null)
        {
            m_titleText.SetText(data.Title);
        }

        if (m_descriptionText != null)
        {
            m_descriptionText.SetText(data.Description);
        }
    }

    private void HandleClick()
    {
        OnSelect?.Invoke(m_characterIndex);
    }
}
