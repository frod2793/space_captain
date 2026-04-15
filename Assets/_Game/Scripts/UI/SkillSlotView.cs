using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using SpaceCaptain.Player;

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

    private SkillSlotUIState m_lastState;
    private Tweener m_outlineTween;

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
    public PlayerCharacterController BoundCharacter
    {
        get
        {
            if (ViewModel != null)
            {
                return ViewModel.Character;
            }
            return null;
        }
    }

    public void Initialize()
    {
        m_rect = GetComponent<RectTransform>();
        m_outline = GetComponent<Outline>();
        m_skillIcon = GetComponent<Image>();
        m_skillButton = GetComponent<Button>();

        if (m_skillButton != null)
        {
            m_skillButton.transition = Selectable.Transition.None;
            m_skillButton.onClick.RemoveAllListeners();
            m_skillButton.onClick.AddListener(() =>
            {
                if (ViewModel != null)
                {
                    ViewModel.ExecuteAction();
                }
            });
        }

        if (ViewModel == null)
        {
            return;
        }

        UpdateCharacterInfo(ViewModel.Character);

        ViewModel.OnStateUpdated += UpdateUI;
        ViewModel.RefreshState();
    }

    public void UpdateCharacter(PlayerCharacterController character)
    {
        if (ViewModel != null)
        {
            ViewModel.Character = character;
            UpdateCharacterInfo(character);

            if (character == null)
            {
                UpdateUI(new SkillSlotUIState(0f, string.Empty, CharacterSwapState.Dead, false));
            }
            else
            {
                ViewModel.RefreshState();
            }
        }
    }

    private void UpdateCharacterInfo(PlayerCharacterController character)
    {
        if (character == null)
        {
            if (m_skillNameText != null)
            {
                m_skillNameText.text = string.Empty;
            }
            if (m_skillIcon != null)
            {
                m_skillIcon.sprite = null;
            }
            if (m_cooldownImage != null)
            {
                m_cooldownImage.fillAmount = 0f;
            }
            if (m_swapCooldownText != null)
            {
                m_swapCooldownText.text = string.Empty;
            }
            return;
        }

        if (m_skillNameText != null)
        {
            m_skillNameText.text = character.CharacterName;
        }

        if (m_skillIcon != null)
        {
            if (character.UI_Icon != null)
            {
                m_skillIcon.sprite = character.UI_Icon;
            }
        }
    }

    private void OnDestroy()
    {
        HandleBlinking(false);

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

    private void UpdateUI(SkillSlotUIState state)
    {
        m_lastState = state;

        bool isDead = (state.Status == CharacterSwapState.Dead);
        float displayCooldown = isDead ? 0f : state.Cooldown;
        string displaySwapText = isDead ? string.Empty : state.SwapText;

        if (m_cooldownImage != null)
        {
            m_cooldownImage.fillAmount = displayCooldown;
        }

        if (m_swapCooldownText != null)
        {
            m_swapCooldownText.text = displaySwapText;
        }

        if (m_skillButton != null)
        {
            m_skillButton.interactable = !isDead;
            m_skillButton.transition = Selectable.Transition.None;
        }

        if (m_outline != null)
        {
            switch (state.Status)
            {
                case CharacterSwapState.Active:
                    m_outline.enabled = true;
                    m_outline.effectColor = m_activeColor;
                    if (m_skillIcon != null)
                    {
                        m_skillIcon.color = Color.white;
                    }
                    HandleBlinking(true);
                    break;

                case CharacterSwapState.Standby:
                    m_outline.enabled = true;
                    m_outline.effectColor = m_activeColor;
                    if (m_skillIcon != null)
                    {
                        m_skillIcon.color = Color.white;
                    }
                    HandleBlinking(false);
                    break;

                case CharacterSwapState.Reserve:
                    m_outline.enabled = true;
                    m_outline.effectColor = m_reserveColor;
                    if (m_skillIcon != null)
                    {
                        m_skillIcon.color = Color.white;
                    }
                    HandleBlinking(false);
                    break;

                case CharacterSwapState.Dead:
                default:
                    m_outline.enabled = true;
                    m_outline.effectColor = Color.gray;
                    if (m_skillIcon != null)
                    {
                        m_skillIcon.color = new Color(0.3f, 0.3f, 0.3f, 1.0f);
                    }
                    HandleBlinking(false);
                    break;
            }
        }
    }

    private void HandleBlinking(bool shouldBlink)
    {
        if (shouldBlink)
        {
            if (m_outlineTween == null)
            {
                if (m_outline != null)
                {
                    m_outlineTween = m_outline.DOFade(0.2f, 0.4f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutQuad);
                }
            }
        }
        else
        {
            if (m_outlineTween != null)
            {
                m_outlineTween.Kill();
                m_outlineTween = null;

                if (m_outline != null)
                {
                    Color color = m_outline.effectColor;
                    color.a = 1f;
                    m_outline.effectColor = color;
                }
            }
        }
    }
}
