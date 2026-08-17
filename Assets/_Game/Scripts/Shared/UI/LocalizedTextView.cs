using TMPro;
using UnityEngine;
using SpaceCaptain.Systems.Localization;

namespace SpaceCaptain.UI.Components
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedTextView : MonoBehaviour
    {
        [SerializeField] private string m_translationKey;

        private TextMeshProUGUI m_textComponent;
        private LocalizationManager m_localizationManager;

        private void Awake()
        {
            m_textComponent = GetComponent<TextMeshProUGUI>();
        }

        public void Setup(LocalizationManager localizationManager)
        {
            if (m_localizationManager != null)
            {
                m_localizationManager.OnLanguageChanged -= UpdateText;
            }

            m_localizationManager = localizationManager;

            if (m_localizationManager != null)
            {
                m_localizationManager.OnLanguageChanged += UpdateText;
                UpdateText(m_localizationManager.CurrentLanguage);
            }
        }

        private void OnDestroy()
        {
            if (m_localizationManager != null)
            {
                m_localizationManager.OnLanguageChanged -= UpdateText;
            }
        }

        private void UpdateText(LanguageType languageType)
        {
            if (m_textComponent != null && m_localizationManager != null)
            {
                m_textComponent.text = m_localizationManager.GetTranslation(m_translationKey);
            }
        }

        public void SetKey(string newKey)
        {
            m_translationKey = newKey;
            
            if (m_localizationManager != null)
            {
                UpdateText(m_localizationManager.CurrentLanguage);
            }
        }
    }
}
