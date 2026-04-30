using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DamageLogEntry : MonoBehaviour
{
    [SerializeField] private Image m_characterIcon;
    [SerializeField] private TextMeshProUGUI m_characterNameText;
    [SerializeField] private TextMeshProUGUI m_damageText;
    [SerializeField] private Slider m_damageSlider;

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
    }
}
