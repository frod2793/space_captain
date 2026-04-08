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

    private ActiveSkill m_boundSkill;

    public void Bind(ActiveSkill skill)
    {
        m_boundSkill = skill;
        m_skillNameText.text = skill.SkillName;

        m_skillButton.onClick.RemoveAllListeners();
        m_skillButton.onClick.AddListener(() => skill.ExecuteAsync().Forget());
    }

    private void Update()
    {
        if (m_boundSkill == null)
        {
            return;
        }

        m_cooldownImage.fillAmount = m_boundSkill.CooldownRatio;
        m_skillButton.interactable = m_boundSkill.IsReady;
    }
}
