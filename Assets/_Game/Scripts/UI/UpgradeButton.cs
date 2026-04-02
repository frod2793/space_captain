using UnityEngine;
using UnityEngine.UI;
using System;

public class UpgradeButton : MonoBehaviour
{
    [SerializeField] private int m_characterIndex; // 0 = a , 1= b, 2 = c
    [SerializeField] private Button m_button;

    public void Initialize(Action<int> onSelect)
    {
        m_button.onClick.RemoveAllListeners();
        m_button.onClick.AddListener(() => onSelect?.Invoke(m_characterIndex));
    }
}
