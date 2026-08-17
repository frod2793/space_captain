using UnityEngine;

public class ExplosiveWeapon : IWeaponBehaviour
{
    public void Fire(in WeaponFireContext ctx)
    {
        if (ctx.Data == null || ctx.Data.ProjectilePrefab == null || ctx.BulletCount <= 0)
        {
            return;
        }

        int bulletCount = ctx.BulletCount;
        Vector2 direction = Quaternion.Euler(0f, 0f, ctx.BaseAngle) * Vector2.up;

        for (int i = 0; i < bulletCount; i++)
        {
            float angleOffset = ProjectileLauncher.AngleOffset(i, bulletCount, ctx.SpreadAngle);
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angleOffset) * direction;
            Vector3 position = ProjectileLauncher.SpawnPosition(ctx, i);

            // 표적이 있으면 그 자리에, 없으면 사거리 끝에 떨어뜨린다
            Vector3 impactPosition = ctx.Target != null && ctx.Target.IsActiveTarget
                ? ctx.Target.TargetTransform.position
                : position + (Vector3)shotDirection * ctx.Data.Range;

            Quaternion rotation = Quaternion.Euler(0f, 0f, ctx.BaseAngle + angleOffset);
            GameObject projectileObject = ProjectileLauncher.Spawn(ctx, position, rotation);

            if (projectileObject == null)
            {
                continue;
            }

            if (!projectileObject.TryGetComponent<ExplosiveProjectile>(out var projectile))
            {
                ProjectileLauncher.Discard(ctx, projectileObject);
                continue;
            }

            projectile.Initialize(
                impactPosition,
                ctx.Data.ProjectileSpeed,
                ctx.Damage,
                ctx.OwnerID,
                ctx.Data.ExplosionRadius,
                ctx.Pool);
        }
    }
}
