using System.Collections.Generic;
using UnityEngine;

public class BulletProjectile : MonoBehaviour, IPoolable
{
    [SerializeField] private float m_speed = 15f;
    [SerializeField] private float m_lifeTime = 3f;
    [SerializeField] private float m_maxRange = 10f;

    private float m_timer;
    private ObjectPoolManager m_pool;
    private Vector3 m_startPosition;
    private float m_defaultSpeed;
    private float m_defaultRange;
    private int m_hitCount;
    private int m_configuredDamage;
    private readonly HashSet<int> m_hitIds = new HashSet<int>();

    public int Damage
    {
        get => m_damage;
        set
        {
            m_damage = value;
            m_configuredDamage = value;
        }
    }

    private int m_damage;
    public string OwnerID { get; set; }
    public int MaxTargets { get; set; } = 1;
    public float PierceDamageRate { get; set; } = 1f;
    public float DamageFalloffRate { get; set; }
    public System.Action<IAttackTarget> OnHit { get; set; }

    private void Awake()
    {
        m_defaultSpeed = m_speed;
        m_defaultRange = m_maxRange;
    }

    private void OnEnable()
    {
        ResetHitState();
        m_timer = 0f;
        m_startPosition = transform.position;

        if (m_pool == null)
        {
            m_pool = FindAnyObjectByType<ObjectPoolManager>();
        }
    }

    public void OnSpawn()
    {
        ResetHitState();
        RestoreConfiguredDamage();
    }

    public void OnDespawn()
    {
        ResetForLegacyFire();
        ResetHitState();
        RestoreConfiguredDamage();
        MaxTargets = 1;
        PierceDamageRate = 1f;
        DamageFalloffRate = 0f;
        OnHit = null;
    }

    public void ResetForLegacyFire()
    {
        m_speed = m_defaultSpeed;
        m_maxRange = m_defaultRange;
    }

    private void Update()
    {
        transform.Translate(Vector3.up * m_speed * Time.deltaTime);

        m_timer += Time.deltaTime;

        if (m_timer >= m_lifeTime || Vector3.Distance(m_startPosition, transform.position) >= m_maxRange)
        {
            Release();
        }
    }

    public void SetSpeed(float speed)
    {
        m_speed = speed;
    }

    public void SetRange(float range)
    {
        if (range > 0f && !float.IsInfinity(range))
        {
            m_maxRange = range;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!WeaponTargetQuery.TryGetEnemyTarget(collision, out IAttackTarget target))
        {
            return;
        }

        Component targetComponent = target as Component;
        int targetId = targetComponent != null ? targetComponent.GetInstanceID() : collision.GetInstanceID();
        if (!m_hitIds.Add(targetId))
        {
            return;
        }

        // ponytail: 선형 감쇠. 실제 총기 곡선과 다르므로 체감이 안 맞으면 곡선(AnimationCurve)으로 교체
        float progress = Mathf.Clamp01(Vector3.Distance(m_startPosition, transform.position) / m_maxRange);
        int damage = Mathf.CeilToInt(m_damage * Mathf.Lerp(1f, 1f - DamageFalloffRate, progress));
        target.TakeDamage(damage, OwnerID);
        OnHit?.Invoke(target);

        if (++m_hitCount >= Mathf.Max(1, MaxTargets) && MaxTargets >= 0)
        {
            Release();
            return;
        }

        m_damage = Mathf.CeilToInt(m_damage * PierceDamageRate);
    }

    private void ResetHitState()
    {
        m_hitCount = 0;
        m_hitIds.Clear();
    }

    private void RestoreConfiguredDamage()
    {
        m_damage = m_configuredDamage;
    }

    private void Release()
    {
        if (m_pool != null)
        {
            m_pool.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
