using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotView : MonoBehaviour
{
    [SerializeField] private Image m_cooldownImage;
    [SerializeField] private TMP_Text m_skillNameText;
    [SerializeField] private TMP_Text m_swapCooldownText;
    [SerializeField] private Color m_activeColor = Color.red;
    [SerializeField] private Color m_reserveColor = Color.blue;

    private Image m_skillIcon;
    private Button m_skillButton;
    private Outline m_outline;
    private RectTransform m_rect;

    private float m_currentCooldown;
    private string m_currentSwapText;
    private bool m_currentIsReady;
    private bool m_currentIsInteractable;
    private bool m_currentIsReserve;

    public RectTransform Rect
    {
        get
        {
            if (m_rect == null)
            {
                m_rect = GetComponent<RectTransform>();
            }
            return m_rect;
        }
    }

    public ISkillSlotViewModel ViewModel { get; set; }
    public PlayerCharacterController BoundCharacter => ViewModel?.Character;

    public void Initialize()
    {
        m_rect = GetComponent<RectTransform>();
        m_outline = GetComponent<Outline>();
        m_skillIcon = GetComponent<Image>();
        m_skillButton = GetComponent<Button>();
        
        if (ViewModel == null)
        {
            return;
        }

        UpdateCharacterInfo(ViewModel.Character);

        if (m_cooldownImage != null)
        {
            m_cooldownImage.fillAmount = m_currentCooldown;
        }
        
        if (m_skillButton != null)
        {
            m_skillButton.onClick.RemoveAllListeners();
            m_skillButton.onClick.AddListener(() => ViewModel.ExecuteAction());
        }

        ViewModel.OnStateUpdated += UpdateUI;
        ViewModel.RefreshState();
    }

    public void UpdateCharacter(PlayerCharacterController character)
    {
        if (ViewModel != null)
        {
            ViewModel.Character = character;
            UpdateCharacterInfo(character);
            ViewModel.RefreshState();
        }
    }

    private void UpdateCharacterInfo(PlayerCharacterController character)
    {
        if (character == null)
        {
            return;
        }

        if (m_skillNameText != null)
        {
            m_skillNameText.text = character.CharacterName;
        }

        if (m_skillIcon != null && character.UI_Icon != null)
        {
            m_skillIcon.sprite = character.UI_Icon;
        }
    }

    private void OnDestroy()
    {
        if (ViewModel != null)
        {
            ViewModel.OnStateUpdated -= UpdateUI;
        }
    }

    private void Update()
    {
        if (ViewModel != null)
        {
            ViewModel.RefreshState();
        }
    }

    private void UpdateUI(float cooldown, string swapText, bool isReady, bool isInteractable, bool isReserve)
    {
        if (m_cooldownImage != null && !Mathf.Approximately(m_currentCooldown, cooldown))
        {
            m_currentCooldown = m_cooldownImage.fillAmount = cooldown;
        }

        if (m_swapCooldownText != null && m_currentSwapText != swapText)
        {
            m_swapCooldownText.text = m_currentSwapText = swapText;
        }

        bool isAppearanceChanged = (m_currentIsReady != isReady || m_currentIsReserve != isReserve);
        bool isInteractionChanged = (m_currentIsInteractable != isInteractable);

        if (isAppearanceChanged)
        {
            m_currentIsReady = isReady;
            m_currentIsReserve = isReserve;
            
            if (m_outline != null)
            {
                m_outline.enabled = isReady;
                m_outline.effectColor = isReserve ? m_reserveColor : m_activeColor;
            }
        }

        if (isInteractionChanged || isAppearanceChanged)
        {
            m_currentIsInteractable = isInteractable;
            
            if (m_skillButton != null)
            {
                m_skillButton.interactable = isInteractable;
            }

            if (m_skillIcon != null)
            {
                float alpha = isInteractable ? 1.0f : 0.5f;
                m_skillIcon.color = new Color(1f, 1f, 1f, alpha);
            }
        }
    }
}
