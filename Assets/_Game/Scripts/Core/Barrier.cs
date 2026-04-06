using UnityEngine;
using System;
using DG.Tweening;

[Serializable]
public class BarrierDTO
{
    public int MaxBarrier = 500;
    public int CurrentBarrier = 500;
    public bool IsBroken = false;
}

public class BarrierLogic
{
    private BarrierDTO m_data;

    public BarrierLogic(BarrierDTO data)
    {
        m_data = data;
    }

    public int ResolveDamage(int damage)
    {
        if (m_data == null || m_data.IsBroken || m_data.CurrentBarrier <= 0)
        {
            return damage;
        }

        if (m_data.CurrentBarrier >= damage)
        {
            m_data.CurrentBarrier -= damage;
            if (m_data.CurrentBarrier <= 0)
            {
                m_data.IsBroken = true;
            }
            return 0;
        }
        else
        {
            int remainingDamage = damage - m_data.CurrentBarrier;
            m_data.CurrentBarrier = 0;
            m_data.IsBroken = true;
            return remainingDamage;
        }
    }

    public float GetBarrierRatio()
    {
        if (m_data == null || m_data.MaxBarrier <= 0)
        {
            return 0f;
        }
        return (float)m_data.CurrentBarrier / m_data.MaxBarrier;
    }

    public bool CheckIsBroken() => m_data != null && m_data.IsBroken;
}

public class Barrier : MonoBehaviour
{
    [SerializeField] private BarrierDTO m_barrierData;
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private Collider2D m_collider;

    private BarrierLogic m_logic;
    private Color m_originalColor;

    public event Action<float> OnBarrierChanged;
    public event Action<int, int> OnBarrierValueWeightChanged;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        if (m_logic != null)
        {
            OnBarrierChanged?.Invoke(m_logic.GetBarrierRatio());
            OnBarrierValueWeightChanged?.Invoke(m_barrierData.CurrentBarrier, m_barrierData.MaxBarrier);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null)
        {
            HandleCollision(collision.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.gameObject != null)
        {
            HandleCollision(collision.gameObject);
        }
    }

    private void HandleCollision(GameObject other)
    {
        if (m_logic == null || m_logic.CheckIsBroken())
        {
            return;
        }

        if (other.TryGetComponent<EnemyController>(out var enemy))
        {
            if (enemy.IsActiveTarget)
            {
                ResolveDamage(10);
                enemy.TakeDamage(9999);
            }
        }
        else if (other.TryGetComponent<EnemyBullet>(out var bullet))
        {
            ResolveDamage(10);
            bullet.SendMessage("Release", SendMessageOptions.DontRequireReceiver);
        }
        else if (other.TryGetComponent<BossController>(out var boss))
        {
            if (boss.IsActiveTarget)
            {
                ResolveDamage(20);
            }
        }
    }

    private void Initialize()
    {
        if (m_barrierData == null)
        {
            m_barrierData = new BarrierDTO();
        }

        m_logic = new BarrierLogic(m_barrierData);
        
        if (m_collider == null)
        {
            m_collider = GetComponent<Collider2D>();
        }
        
        if (m_spriteRenderer == null)
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (m_spriteRenderer != null)
        {
            m_originalColor = m_spriteRenderer.color;
        }
    }

    public int ResolveDamage(int damage)
    {
        if (m_logic == null || m_logic.CheckIsBroken())
        {
            return damage;
        }

        int remainingDamage = m_logic.ResolveDamage(damage);
        
        if (remainingDamage < damage)
        {
            OnBarrierChanged?.Invoke(m_logic.GetBarrierRatio());
            OnBarrierValueWeightChanged?.Invoke(m_barrierData.CurrentBarrier, m_barrierData.MaxBarrier);
            PlayDamageEffect();
        }

        if (m_logic.CheckIsBroken())
        {
            HandleBreak();
        }

        return remainingDamage;
    }

    private void PlayDamageEffect()
    {
        if (m_spriteRenderer != null)
        {
            m_spriteRenderer.DOKill();
            m_spriteRenderer.color = m_originalColor;
            m_spriteRenderer.DOColor(Color.cyan, 0.05f).SetLoops(2, LoopType.Yoyo);
        }
    }

    private void HandleBreak()
    {
        if (m_collider != null)
        {
            m_collider.enabled = false;
        }

        gameObject.SetActive(false);
    }
}
