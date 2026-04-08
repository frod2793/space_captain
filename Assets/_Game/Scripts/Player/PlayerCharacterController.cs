using UnityEngine;
using System;
using DG.Tweening;

public class PlayerCharacterController : MonoBehaviour
{
    [SerializeField] private string m_characterID;
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    private Collider2D m_collider;

    private PlayerStatsDTO m_stats;
    private Barrier m_barrier;
    private float m_targetX;
    [SerializeField] private ActiveSkill m_activeSkill;

    public event Action<PlayerCharacterController> OnSelected;
    public event Action<float> OnHpChanged;
    public event Action<PlayerCharacterController> OnDead;
    
    public bool IsActive => m_stats != null && m_stats.IsActive;
    public bool IsDragging { get; set; }
    public PlayerStatsDTO Stats => m_stats;
    public Collider2D Collider => m_collider;
    public string CharacterID => m_characterID;
    public ActiveSkill Skill => m_activeSkill;

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
        if (m_stats == null || !m_stats.IsActive)
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
        if (m_stats == null || !m_stats.IsActive)
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
        if (m_stats == null || !m_stats.IsActive)
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
        m_spriteRenderer.DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo).OnComplete(() => {
            m_spriteRenderer.color = Color.white;
        });

        m_stats.CurrentHp = Mathf.Max(0, m_stats.CurrentHp - damage);
        float ratio = (float)m_stats.CurrentHp / m_stats.MaxHp;
        OnHpChanged?.Invoke(ratio);

        if (m_stats.CurrentHp <= 0)
        {
            ExecuteDeath();
        }
    }

    private void ExecuteDeath()
    {
        m_stats.IsActive = false;
        OnDead?.Invoke(this);
        
        m_spriteRenderer.enabled = false;
        m_collider.enabled = false;
    }

    public void PlayLevelUpEffect()
    {
        m_spriteRenderer.DOKill();
        m_spriteRenderer.DOColor(Color.yellow, 0.1f).SetLoops(6, LoopType.Yoyo).OnComplete(() => {
            m_spriteRenderer.color = Color.white;
        });
    }
}
