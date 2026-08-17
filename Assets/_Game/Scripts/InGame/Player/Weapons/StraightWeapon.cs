using UnityEngine;

public class StraightWeapon : IWeaponBehaviour
{
    public void Fire(in WeaponFireContext ctx)
    {
        if (ctx.Data == null || ctx.Data.ProjectilePrefab == null || ctx.BulletCount <= 0 || ctx.FirePoints == null || ctx.FirePoints.Length == 0)
        {
            return;
        }

        int bulletCount = ctx.BulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angleOffset = ProjectileLauncher.AngleOffset(i, bulletCount, ctx.SpreadAngle);
            Vector3 position = ProjectileLauncher.SpawnPosition(ctx, i);
            Quaternion rotation = Quaternion.Euler(0f, 0f, ctx.BaseAngle + angleOffset);

            GameObject bullet = ProjectileLauncher.Spawn(ctx, position, rotation);

            if (bullet == null)
            {
                continue;
            }

            if (!bullet.TryGetComponent<BulletProjectile>(out var projectile))
            {
                ProjectileLauncher.Discard(ctx, bullet);
                continue;
            }

            projectile.SetSpeed(ctx.Data.ProjectileSpeed);
            projectile.SetRange(ctx.Data.Range);
            projectile.OwnerID = ctx.OwnerID;
            projectile.Damage = ctx.Damage;
            projectile.MaxTargets = ctx.Data.MaxTargets;
            projectile.PierceDamageRate = ctx.Data.PierceDamageRate;
            projectile.DamageFalloffRate = ctx.Data.DamageFalloffRate;
            projectile.OnHit = ctx.OnProjectileHit;
        }
    }
}
