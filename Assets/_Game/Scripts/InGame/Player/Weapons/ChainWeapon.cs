using System.Collections.Generic;
using UnityEngine;

public class ChainWeapon : IWeaponBehaviour
{
    private readonly System.Collections.Generic.HashSet<int> m_hitIds = new System.Collections.Generic.HashSet<int>();

    // 상태가 없으므로 공유한다. 발사마다 new 하면 초당 할당이 쌓인다.
    private static readonly StraightWeapon s_straight = new StraightWeapon();
    public void Fire(in WeaponFireContext ctx)
    {
        if (ctx.Data == null || ctx.Data.ProjectilePrefab == null || ctx.FirePoints == null || ctx.FirePoints.Length == 0)
        {
            return;
        }

        WeaponFireContext context = ctx;
        var primary = new WeaponFireContext
        {
            Origin = ctx.Origin,
            BaseAngle = ctx.BaseAngle,
            Damage = ctx.Damage,
            OwnerID = ctx.OwnerID,
            FirePoints = ctx.FirePoints,
            Target = ctx.Target,
            Pool = ctx.Pool,
            Data = ctx.Data,
            BulletCount = 1,
            SpreadAngle = 0f,
            ScaleMultiplier = ctx.ScaleMultiplier,
            OnProjectileHit = target => HandlePrimaryHit(context, target)
        };
        s_straight.Fire(primary);
    }

    private void HandlePrimaryHit(WeaponFireContext context, IAttackTarget firstTarget)
    {
        if (!IsPrimaryTarget(firstTarget))
        {
            return;
        }

        m_hitIds.Clear();
        m_hitIds.Add(GetTargetId(firstTarget));
        IAttackTarget current = firstTarget;
        int damage = Mathf.CeilToInt(context.Damage * context.Data.ChainDamageRate);
        Vector3 segmentStart = firstTarget.TargetTransform.position;

        for (int hop = 0; hop < Mathf.Max(0, context.Data.ChainCount); hop++)
        {
            current = FindNearestNextTarget(current.TargetTransform.position, context.Data.ChainRange, m_hitIds);
            if (!WeaponTargetQuery.IsEnemyTarget(current))
            {
                break;
            }

            m_hitIds.Add(GetTargetId(current));
            current.TakeDamage(damage, context.OwnerID);
            Vector3 segment = current.TargetTransform.position - segmentStart;
            float segmentLength = segment.magnitude;
            SkillLaser.SpawnWeaponVisual(
                context.Pool,
                context.Data.BeamVisualPrefab,
                segmentStart,
                segmentLength > 0.001f ? segment / segmentLength : Vector3.up,
                segmentLength,
                0.2f,
                Color.magenta);
            segmentStart = current.TargetTransform.position;
            damage = Mathf.CeilToInt(damage * context.Data.ChainDamageRate);
        }
    }

    private static int GetTargetId(IAttackTarget target)
    {
        Component component = target as Component;
        return component != null ? component.GetInstanceID() : target.TargetTransform.GetInstanceID();
    }

    private static bool IsPrimaryTarget(IAttackTarget target)
    {
        if (target == null || target.TargetTransform == null)
        {
            return false;
        }

        return (target.TargetTransform.CompareTag("Enemy") || target.TargetTransform.CompareTag("Boss")) &&
            (target is EnemyController || target is BossController);
    }

    private static IAttackTarget FindNearestNextTarget(Vector3 origin, float range, HashSet<int> hitIds)
    {
        int colliderCount = WeaponTargetQuery.OverlapCircle(origin, Mathf.Max(0f, range), out Collider2D[] colliders);
        IAttackTarget nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < colliderCount; i++)
        {
            if (!WeaponTargetQuery.TryGetEnemyTarget(colliders[i], out IAttackTarget target))
            {
                continue;
            }

            Component targetComponent = target as Component;
            int targetId = targetComponent != null ? targetComponent.GetInstanceID() : colliders[i].GetInstanceID();
            if (hitIds.Contains(targetId))
            {
                continue;
            }

            float distance = (target.TargetTransform.position - origin).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = target;
            }
        }

        return nearest;
    }
}
