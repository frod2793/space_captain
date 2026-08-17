using UnityEngine;
using UnityEngine.Serialization;
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public enum SkillPerformanceType { Default, Laser }

public class ActiveSkill : MonoBehaviour
{
    [SerializeField] private string m_characterName;
    [SerializeField] private float m_cooldownTime = 10f;
    [SerializeField] private float m_performanceDuration = 2.4f;
    [SerializeField] private SkillPerformanceType m_performanceType = SkillPerformanceType.Default;
    [SerializeField] private SkillLaser m_skillEffectPrefab;

    private float m_currentCooldown = 0f;
    private bool m_isExecuting = false;
    private PlayerCharacterController m_owner;

    public string CharacterName => m_characterName;
    public float CooldownRatio => Mathf.Clamp01(m_currentCooldown / m_cooldownTime);
    public bool IsReady => m_currentCooldown <= 0f && !m_isExecuting;

    public void Initialize(PlayerCharacterController owner)
    {
        m_owner = owner;
    }

    public void UpdateCooldown(float deltaTime)
    {
        if (m_currentCooldown > 0f)
        {
            m_currentCooldown -= deltaTime;
        }
    }

    public async UniTask ExecuteAsync()
    {
        if (m_owner == null) 
        {
            return;
        }
        m_isExecuting = true;

        if (m_skillEffectPrefab != null)
        {
            float originalTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0.1f;

                SkillCutInUI cutInUI = FindAnyObjectByType<SkillCutInUI>();
                if (cutInUI != null)
                {
                    cutInUI.Show(m_owner.CharacterID, m_owner.CharacterName, m_performanceType);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(m_performanceDuration), ignoreTimeScale: true);
                await UniTask.Delay(TimeSpan.FromSeconds(1f), ignoreTimeScale: true);

                var effect = Instantiate(m_skillEffectPrefab, m_owner.transform.position, m_owner.transform.rotation, m_owner.transform);
                effect.transform.localPosition = Vector3.zero;
                effect.Trigger(m_owner);
            }
            finally
            {
                if (Time.timeScale > 0f)
                {
                    Time.timeScale = originalTimeScale;
                }
            }
        }

        m_currentCooldown = m_cooldownTime;
        m_isExecuting = false;

    }
}
