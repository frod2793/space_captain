using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SpaceCaptain.Player;

public class PlayerCharacterController : MonoBehaviour
{
    [SerializeField] private string m_characterID;
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private Sprite m_uiIcon;
    private Collider2D m_collider;

    private PlayerStatsDTO m_stats;
    private Barrier m_barrier;
    private float m_targetX;
    [SerializeField] private ActiveSkill m_activeSkill;
    private float m_swapCooldownEndTime = 0f;
    private bool m_isDying = false;

    public event Action<float> OnHpChanged;
    public event Action<PlayerCharacterController> OnDead;

    public bool IsActive => m_stats != null && m_stats.IsActive;
    public bool IsOnField => gameObject.activeSelf;
    public bool IsDying => m_isDying;
    public bool IsDragging { get; set; }
    public PlayerStatsDTO Stats => m_stats;
    public Collider2D Collider => m_collider;
    public Sprite UI_Icon => m_uiIcon;
    public string CharacterID => m_characterID;
    public string CharacterName => (m_activeSkill != null) ? m_activeSkill.CharacterName : m_characterID;
    public ActiveSkill Skill => m_activeSkill;

    [SerializeField] private CharacterSwapState m_swapState = CharacterSwapState.Reserve;
    public CharacterSwapState SwapState
    {
        get { return m_swapState; }
        set { m_swapState = value; }
    }

    public float RemainingSwapCooldown => Mathf.Max(0f, m_swapCooldownEndTime - Time.time);

    private void Awake()
    {
        m_collider = GetComponent<Collider2D>();
        if (m_activeSkill == null)
        {
            m_activeSkill = GetComponent<ActiveSkill>();
        }
    }

    private void Update()
    {
        HandleMovementUpdate();
    }

    public void Initialize(PlayerStatsDTO stats)
    {
        m_stats = stats;
        m_targetX = transform.position.x;

        if (m_activeSkill != null)
        {
            m_activeSkill.Initialize(this);
        }
    }

    public void SetBarrier(Barrier barrier)
    {
        m_barrier = barrier;
    }

    public void SetActive(bool isActive)
    {
        if (m_stats == null)
        {
            return;
        }

        m_stats.IsActive = isActive;
    }

    private void HandleMovementUpdate()
    {
        if (!m_stats.IsActive)
        {
            return;
        }

        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x - m_targetX) > 0.001f)
        {
            pos.x = Mathf.Lerp(pos.x, m_targetX, Time.deltaTime * m_stats.MoveSpeed);
            transform.position = pos;
            m_stats.CurrentX = pos.x;
        }
    }

    public void MoveToX(float x, bool immediate = false)
    {
        if (!m_stats.IsActive)
        {
            return;
        }

        m_targetX = x;

        if (immediate)
        {
            Vector3 pos = transform.position;
            pos.x = x;
            transform.position = pos;
            m_stats.CurrentX = x;
        }
    }

    public void TakeDamage(int damage)
    {
        if (m_stats == null || !IsOnField || m_isDying)
        {
            return;
        }

        if (m_barrier != null)
        {
            damage = m_barrier.ResolveDamage(damage);
        }

        if (damage <= 0)
        {
            return;
        }

        m_spriteRenderer.DOKill();
        m_spriteRenderer.DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo).OnComplete(() =>
        {
            m_spriteRenderer.color = Color.white;
        });

        m_stats.CurrentHp = Mathf.Max(0, m_stats.CurrentHp - damage);
        float ratio = (float)m_stats.CurrentHp / m_stats.MaxHp;
        OnHpChanged?.Invoke(ratio);

        if (m_stats.CurrentHp <= 0)
        {
            HandleDeathSequence().Forget();
        }
    }

    private async Cysharp.Threading.Tasks.UniTaskVoid HandleDeathSequence()
    {
        if (m_isDying)
        {
            return;
        }

        m_isDying = true;
        m_stats.IsActive = false;

        if (m_spriteRenderer != null)
        {
            m_spriteRenderer.DOKill();
            
            await transform.DOShakePosition(0.5f, 0.2f, 20, 90, false, true).GetAwaiter();
        }

        ExecuteDeath();
    }


    public void Heal(int amount)
    {
        if (m_stats == null || m_stats.CurrentHp <= 0)
        {
            return;
        }

        m_stats.CurrentHp = Mathf.Min(m_stats.MaxHp, m_stats.CurrentHp + amount);
        float ratio = (float)m_stats.CurrentHp / m_stats.MaxHp;
        OnHpChanged?.Invoke(ratio);
    }

    private void ExecuteDeath()
    {
        m_stats.IsActive = false;
        m_swapState = CharacterSwapState.Dead;
        m_spriteRenderer.enabled = false;
        m_collider.enabled = false;

        OnDead?.Invoke(this);
    }

    public void RestoreComponents()
    {
        m_isDying = false;
        if (m_spriteRenderer != null)
        {
            m_spriteRenderer.DOKill();
            m_spriteRenderer.enabled = true;
            m_spriteRenderer.color = Color.white;
        }

        if (m_collider != null)
        {
            m_collider.enabled = true;
        }
    }

    public void PlayCooldownFeedback()
    {
        if (m_spriteRenderer == null)
        {
            return;
        }

        m_spriteRenderer.DOKill();
        m_spriteRenderer.DOColor(Color.red, 0.05f).SetLoops(4, LoopType.Yoyo).OnComplete(() =>
        {
            m_spriteRenderer.color = Color.white;
        });
    }

    public void PlayLevelUpEffect()
    {
        m_spriteRenderer.DOKill();
        m_spriteRenderer.DOColor(Color.yellow, 0.1f).SetLoops(6, LoopType.Yoyo).OnComplete(() =>
        {
            m_spriteRenderer.color = Color.white;
        });
    }

    public void SetSwapCooldown(float duration)
    {
        m_swapCooldownEndTime = Time.time + duration;
    }
}