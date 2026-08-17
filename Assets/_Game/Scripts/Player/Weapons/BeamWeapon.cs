using UnityEngine;

public class BeamWeapon : IWeaponBehaviour
{
    private readonly System.Collections.Generic.HashSet<int> m_hitIds = new System.Collections.Generic.HashSet<int>();

    public void Fire(in WeaponFireContext ctx)
    {
        if (ctx.Data == null)
        {
            return;
        }

        float width = Mathf.Max(0f, ctx.Data.BeamWidth);
        float range = Mathf.Max(0f, ctx.Data.BeamRange > 0f ? ctx.Data.BeamRange : ctx.Data.Range);
        Vector2 direction = Quaternion.Euler(0f, 0f, ctx.BaseAngle) * Vector2.up;
        int hitCount = WeaponTargetQuery.BoxCast(
            ctx.Origin,
            new Vector2(width, 0.1f),
            ctx.BaseAngle,
            direction,
            range,
            out RaycastHit2D[] hits);
        m_hitIds.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            if (!WeaponTargetQuery.TryGetEnemyTarget(hits[i].collider, out IAttackTarget target))
            {
                continue;
            }

            Component targetComponent = target as Component;
            int targetId = targetComponent != null ? targetComponent.GetInstanceID() : hits[i].collider.GetInstanceID();
            if (m_hitIds.Add(targetId))
            {
                target.TakeDamage(ctx.Damage, ctx.OwnerID);
            }
        }

        SkillLaser.SpawnWeaponVisual(
            ctx.Pool,
            ctx.Data.BeamVisualPrefab,
            ctx.Origin,
            direction,
            range,
            width,
            Color.cyan);
    }
}
