using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackComponent : MonoBehaviour
{
    [SerializeField] private PlayerCharacterController m_owner;
    [SerializeField] private PlayerAttackType m_attackType;
    [SerializeField] private GameObject m_bulletPrefab;
    [SerializeField] private Transform[] m_firePoints;
    [SerializeField] private float m_fireRate = 0.5f;
    [SerializeField] private float m_bulletSpeed = 10f;
    [SerializeField] private float m_targetingRange = 10f;

    public EnemyController CurrentTarget { get; set; }
    private float m_fireTimer;

    private void Update()
    {
        if (m_owner == null || !m_owner.IsActive) return;

        UpdateTargeting();
        
        m_fireTimer += Time.deltaTime;
        if (m_fireTimer >= m_fireRate && (CurrentTarget != null || (m_owner != null && m_owner.IsDragging)))
        {
            m_fireTimer = 0f;
            Fire();
        }
    }

    private void Fire()
    {
        if (m_bulletPrefab == null) 
        {
            return;
        }

        float baseAngle = 0f;
        if (m_owner != null && !m_owner.IsDragging && CurrentTarget != null)
        {
            Vector3 direction = (CurrentTarget.transform.position - transform.position).normalized;
            baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        }
        else
        {
            baseAngle = transform.rotation.eulerAngles.z;
        }

        int totalBulletCount = 1;
        float spreadAngle = 0f;

        switch (m_attackType)
        {
            case PlayerAttackType.Single: 
                totalBulletCount = 1; 
                spreadAngle = 5f; 
                break;
            case PlayerAttackType.Double: 
                totalBulletCount = 2; 
                spreadAngle = 0f; 
                break;
            case PlayerAttackType.Spread: 
                totalBulletCount = 3; 
                spreadAngle = 60f; 
                break;
        }

        float baseSpread = spreadAngle; 

        if (m_owner != null && m_owner.Stats != null)
        {
            totalBulletCount += m_owner.Stats.BulletCountBonus;
            spreadAngle = spreadAngle + m_owner.Stats.SpreadAngleBonus;
        }

        int firePointCount = m_firePoints != null ? m_firePoints.Length : 0;
        if (firePointCount == 0)
        {
            return;
        }

        for (int i = 0; i < totalBulletCount; i++)
        {
            float angleOffset = totalBulletCount > 1 ? -spreadAngle / 2f + (spreadAngle / (totalBulletCount - 1)) * i : 0f;
            
            Transform firePoint = m_firePoints[i % firePointCount];
            Vector3 spawnPos = firePoint.position;

            if (m_attackType == PlayerAttackType.Spread && baseSpread > 0)
            {
                float k = Mathf.Clamp01(spreadAngle / baseSpread);
                
                Vector3 localPos = transform.InverseTransformPoint(firePoint.position);
                localPos.x *= k; 
                spawnPos = transform.TransformPoint(localPos);

                int indexInPoints = i % firePointCount;
                if (indexInPoints == 0 || indexInPoints == 2)
                {
                    angleOffset *= k; 
                }
            }

            float finalAngle = baseAngle + angleOffset;
            CreateBullet(spawnPos, finalAngle);
        }
    }

    private void UpdateTargeting()
    {
        if (CurrentTarget == null || CurrentTarget.gameObject.activeInHierarchy == false ||
            Vector2.Distance(transform.position, CurrentTarget.transform.position) > m_targetingRange)
        {
            CurrentTarget = FindNearestEnemy();
        }
    }

    private EnemyController FindNearestEnemy()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        EnemyController nearest = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null || !enemies[i].gameObject.activeInHierarchy) continue;
            float dist = Vector2.Distance(transform.position, enemies[i].transform.position);
            if (dist < minDistance && dist <= m_targetingRange)
            {
                minDistance = dist;
                nearest = enemies[i];
            }
        }
        return nearest;
    }

    private void CreateBullet(Vector3 position, float angle)
    {
        var pool = FindAnyObjectByType<ObjectPoolManager>();
        GameObject bulletObj;
        
        if (pool != null) bulletObj = pool.GetFromPool(m_bulletPrefab, position, Quaternion.Euler(0, 0, angle));
        else bulletObj = Instantiate(m_bulletPrefab, position, Quaternion.Euler(0, 0, angle));

        if (bulletObj.TryGetComponent<BulletProjectile>(out var projectile))
        {
            projectile.SetSpeed(m_bulletSpeed);
            if (m_owner != null && m_owner.Stats != null) projectile.Damage = m_owner.Stats.AttackDamage;
        }
    }
}

public enum PlayerAttackType
{
    Single,
    Double,
    Spread
}
