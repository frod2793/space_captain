using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

public class SkillSlotView : MonoBehaviour
{
    private Image m_skillIcon;
    [SerializeField] private Image m_cooldownImage;
    private Button m_skillButton;
    [SerializeField] private TMP_Text m_skillNameText;
    [SerializeField] private TMP_Text m_swapCooldownText;
    private Outline m_outline;

    private float m_currentCooldown;
    private string m_currentSwapText;
    private bool m_currentIsReady;
    private bool m_currentIsInteractable;
    private bool m_currentIsReserve;

    [Header("아웃라인 설정")]
    [SerializeField] private Color m_activeColor = Color.red;
    [SerializeField] private Color m_reserveColor = Color.blue;

    private RectTransform m_rect;
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

        m_cooldownImage.fillAmount = m_currentCooldown;
        
        m_skillButton.onClick.RemoveAllListeners();
        m_skillButton.onClick.AddListener(() => ViewModel.ExecuteAction());

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

        if (character.UI_Icon != null && m_skillIcon != null)
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
        if (!Mathf.Approximately(m_currentCooldown, cooldown))
        {
            m_currentCooldown = m_cooldownImage.fillAmount = cooldown;
        }

        if (m_currentSwapText != swapText)
        {
            m_swapCooldownText.text = m_currentSwapText = swapText;
        }

        if (m_currentIsReady != isReady || m_currentIsReserve != isReserve)
        {
            m_currentIsReady = isReady;
            m_currentIsReserve = isReserve;
            m_outline.enabled = isReady;
            m_outline.effectColor = isReserve ? m_reserveColor : m_activeColor;
            
            // 버튼 자체의 색상 변경을 막기 위해 이미지 색상을 화이트로 고정
            m_skillIcon.color = Color.white;
        }

        if (m_currentIsInteractable != isInteractable)
        {
            m_skillButton.interactable = m_currentIsInteractable = isInteractable;
        }
    }
}
