using System.Collections.Generic;
using UnityEngine;

public class ExplosiveProjectile : MonoBehaviour, IPoolable
{
    private Vector3 m_startPosition;
    private Vector3 m_impactPosition;
    private float m_speed;
    private float m_travelDistance;
    private float m_elapsedTime;
    private float m_arcHeight;
    private int m_damage;
    private string m_ownerID;
    private float m_explosionRadius;
    private ObjectPoolManager m_pool;
    private bool m_isFlying;
    private bool m_hasImpacted;
    private readonly HashSet<int> m_hitIds = new HashSet<int>();

    public void Initialize(
        Vector3 impactPosition,
        float speed,
        int damage,
        string ownerID,
        float explosionRadius,
        ObjectPoolManager pool)
    {
        m_startPosition = transform.position;
        m_impactPosition = impactPosition;
        m_speed = Mathf.Max(0f, speed);
        m_travelDistance = Vector3.Distance(m_startPosition, m_impactPosition);
        m_elapsedTime = 0f;
        m_arcHeight = Mathf.Min(m_travelDistance * 0.1f, 1f);
        m_damage = damage;
        m_ownerID = ownerID;
        m_explosionRadius = Mathf.Max(0f, explosionRadius);
        m_pool = pool;
        m_hasImpacted = false;
        m_isFlying = true;

        if (m_speed <= 0f || Vector3.Distance(transform.position, m_impactPosition) <= 0.001f)
        {
            Impact();
        }
    }

    public void OnSpawn()
    {
        m_isFlying = false;
        m_hasImpacted = false;
    }

    public void OnDespawn()
    {
        m_isFlying = false;
        m_hasImpacted = false;
    }

    private void Update()
    {
        if (!m_isFlying || m_hasImpacted)
        {
            return;
        }

        m_elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(m_elapsedTime * m_speed / m_travelDistance);
        float arcOffset = 4f * m_arcHeight * progress * (1f - progress);
        transform.position = Vector3.Lerp(m_startPosition, m_impactPosition, progress) + Vector3.up * arcOffset;

        if (progress >= 1f)
        {
            Impact();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (WeaponTargetQuery.TryGetEnemyTarget(collision, out IAttackTarget target))
        {
            Impact();
        }
    }

    private void Impact()
    {
        if (m_hasImpacted)
        {
            return;
        }

        m_hasImpacted = true;
        m_isFlying = false;
        ApplyExplosionDamage();
        Release();
    }

    private void ApplyExplosionDamage()
    {
        int colliderCount = WeaponTargetQuery.OverlapCircle(transform.position, m_explosionRadius, out Collider2D[] colliders);
        m_hitIds.Clear();

        for (int i = 0; i < colliderCount; i++)
        {
            if (!WeaponTargetQuery.TryGetEnemyTarget(colliders[i], out IAttackTarget target))
            {
                continue;
            }

            Component targetComponent = target as Component;
            int targetId = targetComponent != null
                ? targetComponent.GetInstanceID()
                : colliders[i].GetInstanceID();
            if (m_hitIds.Add(targetId))
            {
                target.TakeDamage(m_damage, m_ownerID);
            }
        }
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
