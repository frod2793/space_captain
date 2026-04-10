using UnityEngine;
using UnityEngine.UI;
using System;

public class UpgradeButton : MonoBehaviour
{
    [SerializeField] private int m_characterIndex;
    [SerializeField] private Button m_button;

    public Action<int> OnSelect { get; set; }

    public void Initialize()
    {
        if (m_button != null)
        {
            m_button.onClick.RemoveAllListeners();
            m_button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        OnSelect?.Invoke(m_characterIndex);
    }
}
