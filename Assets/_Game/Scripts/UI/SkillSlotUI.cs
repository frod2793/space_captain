using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image m_skillIcon;
    [SerializeField] private Image m_cooldownImage;
    [SerializeField] private Button m_skillButton;
    [SerializeField] private TMP_Text m_skillNameText;
    [SerializeField] private TMP_Text m_swapCooldownText;
    [SerializeField] private Image m_outlineImage;

    private ActiveSkill m_boundSkill;
    public PlayerCharacterController BoundCharacter => m_boundSkill != null ? m_boundSkill.GetComponent<PlayerCharacterController>() : null;

    public void Bind(PlayerCharacterController character)
    {
        if (character == null || character.Skill == null) return;

        m_boundSkill = character.Skill;
        if (m_skillNameText != null) m_skillNameText.text = character.CharacterName;
        if (m_skillIcon != null && character.UI_Icon != null)
        {
            m_skillIcon.sprite = character.UI_Icon;
        }

        m_skillButton.onClick.RemoveAllListeners();
        m_skillButton.onClick.AddListener(() => OnSkillButtonClicked());
    }

    private void OnSkillButtonClicked()
    {
        if (m_boundSkill == null) return;

        var character = m_boundSkill.GetComponent<PlayerCharacterController>();
        var swapManager = FindAnyObjectByType<PlayerSwapManager>();

        if (character != null && swapManager != null)
        {
            swapManager.ExecuteCharacterActionAsync(character).Forget();
        }
    }

    private void Update()
    {
        if (m_boundSkill == null)
        {
            return;
        }

        m_cooldownImage.fillAmount = m_boundSkill.CooldownRatio;

        var swapManager = FindAnyObjectByType<PlayerSwapManager>();
        var character = m_boundSkill.GetComponent<PlayerCharacterController>();
        if (swapManager != null && character != null && m_swapCooldownText != null)
        {
            bool isReserve = character != swapManager.ActiveCharacter;
            float individualCD = character.RemainingSwapCooldown;
            m_swapCooldownText.text = (isReserve && individualCD > 0.01f) ? $"{individualCD:F1}" : string.Empty;
        }

        if (m_outlineImage != null)
        {
            m_outlineImage.enabled = m_boundSkill.IsReady;
        }

        bool isDead = character != null && character.Stats != null && character.Stats.CurrentHp <= 0;
        m_skillButton.interactable = !isDead;
    }
}
