using UnityEngine;
using System.Collections.Generic;

public class PlayerAttackComponent : MonoBehaviour
{
    [SerializeField] private PlayerCharacterController m_owner;
    [SerializeField] private WeaponDataSO m_weapon;
    [SerializeField] private PlayerAttackType m_attackType;
    [SerializeField] private GameObject m_bulletPrefab;
    [SerializeField] private Transform[] m_firePoints;
    [SerializeField] private float m_fireRate = 0.5f;
    [SerializeField] private float m_bulletSpeed = 10f;
    [SerializeField] private float m_targetingRange = 10f;

    public IAttackTarget CurrentTarget { get; set; }
    private IWeaponBehaviour m_behaviour;
    private ObjectPoolManager m_pool;
    private float m_fireTimer;
    private float m_warmupTimer;
    private IAttackTarget m_previousTarget;

    private void Awake()
    {
        m_pool = FindAnyObjectByType<ObjectPoolManager>();
        m_behaviour = CreateBehaviour();
    }

    private void Update()
    {
        if (m_owner == null)
        {
            return;
        }

        UpdateTargeting();
        if (!ReferenceEquals(CurrentTarget, m_previousTarget))
        {
            m_previousTarget = CurrentTarget;
            m_warmupTimer = 0f;
        }

        m_fireTimer += Time.deltaTime;

        bool canFire = m_owner.Stats != null && m_owner.Stats.CurrentHp > 0 && m_owner.IsOnField &&
            (CurrentTarget != null || (m_owner.IsActive && m_owner.IsDragging));
        if (!canFire)
        {
            m_warmupTimer = 0f;
            return;
        }

        if (m_weapon == null || m_behaviour == null)
        {
            if (m_fireTimer >= m_fireRate)
            {
                m_fireTimer = 0f;
                FireLegacy();
            }
            return;
        }

        float fireRate = Mathf.Max(0f, m_weapon.FireRate);
        if (m_weapon.WarmupTime > 0f)
        {
            m_warmupTimer += Time.deltaTime;
            float maxFireRate = m_weapon.MaxFireRate > 0f ? m_weapon.MaxFireRate : m_weapon.FireRate;
            fireRate = Mathf.Lerp(m_weapon.FireRate, maxFireRate,
                Mathf.Clamp01(m_warmupTimer / m_weapon.WarmupTime));
        }

        if (m_fireTimer < fireRate)
        {
            return;
        }

        m_fireTimer = 0f;
        Fire();
    }

    private void Fire()
    {
        float baseAngle = 0f;
        if ((!m_owner.IsActive || !m_owner.IsDragging) && CurrentTarget != null && CurrentTarget.TargetTransform != null)
        {
            Vector3 direction = (CurrentTarget.TargetTransform.position - transform.position).normalized;
            baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        }
        else
        {
            baseAngle = transform.rotation.eulerAngles.z;
        }

        int totalBulletCount = m_weapon.BulletCount;
        float spreadAngle = m_weapon.SpreadAngle;
        if (m_owner.Stats != null)
        {
            totalBulletCount += m_owner.Stats.BulletCountBonus;
            spreadAngle += m_owner.Stats.SpreadAngleBonus;
        }

        var context = new WeaponFireContext
        {
            Origin = transform.position,
            BaseAngle = baseAngle,
            Damage = m_owner.Stats != null
                ? Mathf.CeilToInt(Mathf.Max(0f, m_owner.Stats.AttackDamage * m_weapon.DamageMultiplier))
                : 0,
            OwnerID = m_owner.CharacterID,
            FirePoints = m_firePoints,
            Target = CurrentTarget,
            Pool = m_pool,
            Data = m_weapon,
            BulletCount = totalBulletCount,
            SpreadAngle = spreadAngle,
            ScaleMultiplier = m_owner.IsActive ? 1f : 0.5f
        };

        m_behaviour.Fire(context);
    }

    private IWeaponBehaviour CreateBehaviour()
    {
        if (m_weapon == null)
        {
            return null;
        }

        switch (m_weapon.Behaviour)
        {
            case WeaponBehaviourType.Beam:
                return new BeamWeapon();
            case WeaponBehaviourType.Explosive:
                return new ExplosiveWeapon();
            case WeaponBehaviourType.Chain:
                return new ChainWeapon();
            default:
                return new StraightWeapon();
        }
    }

    public void SetWeapon(WeaponDataSO weapon)
    {
        if (weapon == null)
        {
            return;
        }

        m_weapon = weapon;
        m_behaviour = CreateBehaviour();
        m_warmupTimer = 0f;
    }

    private void UpdateTargeting()
    {
        float currentTargetingRange = m_weapon != null ? Mathf.Max(0f, m_weapon.Range) : m_targetingRange;
        currentTargetingRange *= m_owner.IsActive ? 1.0f : 0.5f;

        if (CurrentTarget == null || CurrentTarget.IsActiveTarget == false || CurrentTarget.TargetTransform == null ||
            Vector2.Distance(transform.position, CurrentTarget.TargetTransform.position) > currentTargetingRange)
        {
            CurrentTarget = FindNearestEnemy(currentTargetingRange);
        }
    }

    private IAttackTarget FindNearestEnemy(float range)
    {
        var targets = new List<IAttackTarget>();
        targets.AddRange(FindObjectsByType<EnemyController>(FindObjectsSortMode.None));
        targets.AddRange(FindObjectsByType<BossController>(FindObjectsSortMode.None));

        IAttackTarget nearest = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null || !targets[i].IsActiveTarget)
            {
                continue;
            }

            float dist = Vector2.Distance(transform.position, targets[i].TargetTransform.position);
            if (dist < minDistance && dist <= range)
            {
                minDistance = dist;
                nearest = targets[i];
            }
        }
        return nearest;
    }

    private void FireLegacy()
    {
        if (m_bulletPrefab == null || m_firePoints == null || m_firePoints.Length == 0)
        {
            return;
        }

        float baseAngle = CurrentTarget != null && (!m_owner.IsActive || !m_owner.IsDragging)
            ? Mathf.Atan2((CurrentTarget.TargetTransform.position - transform.position).y, (CurrentTarget.TargetTransform.position - transform.position).x) * Mathf.Rad2Deg - 90f
            : transform.rotation.eulerAngles.z;
        int count = m_attackType == PlayerAttackType.Double ? 2 : m_attackType == PlayerAttackType.Spread ? 3 : 1;
        float spread = m_attackType == PlayerAttackType.Spread ? 60f : 0f;
        if (m_owner.Stats != null)
        {
            count += m_owner.Stats.BulletCountBonus;
            spread += m_owner.Stats.SpreadAngleBonus;
        }

        for (int i = 0; i < count; i++)
        {
            float offset = count > 1 ? -spread / 2f + spread / (count - 1) * i : 0f;
            GameObject bullet = m_pool != null
                ? m_pool.GetFromPool(m_bulletPrefab, m_firePoints[i % m_firePoints.Length].position, Quaternion.Euler(0f, 0f, baseAngle + offset))
                : Instantiate(m_bulletPrefab, m_firePoints[i % m_firePoints.Length].position, Quaternion.Euler(0f, 0f, baseAngle + offset));
            if (bullet != null && bullet.TryGetComponent<BulletProjectile>(out var projectile))
            {
                projectile.ResetForLegacyFire();
                bullet.transform.localScale = Vector3.one * (m_owner.IsActive ? 1f : 0.5f);
                projectile.SetSpeed(m_bulletSpeed);
                projectile.OwnerID = m_owner.CharacterID;
                projectile.Damage = m_owner.Stats != null ? m_owner.Stats.AttackDamage : 0;
                projectile.MaxTargets = 1;
                projectile.PierceDamageRate = 1f;
                projectile.DamageFalloffRate = 0f;
                projectile.OnHit = null;
            }
        }
    }
}

public enum PlayerAttackType
{
    Single,
    Double,
    Spread
}
