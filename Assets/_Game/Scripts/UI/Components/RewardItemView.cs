using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardItemView : MonoBehaviour
{
    [SerializeField] private Image m_itemIconImage;
    [SerializeField] private TextMeshProUGUI m_amountText;

    public void SetData(Sprite icon, int amount)
    {
        if (m_itemIconImage != null)
        {
            m_itemIconImage.sprite = icon;
            m_itemIconImage.gameObject.SetActive(icon != null);
            
            if (icon != null)
            {
                m_itemIconImage.SetNativeSize();
            }
        }

        if (m_amountText != null)
        {
            m_amountText.text = amount > 1 ? $"x{amount}" : string.Empty;
        }
    }
}
