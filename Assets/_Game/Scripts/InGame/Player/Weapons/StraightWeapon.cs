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
        float spread = ctx.SpreadAngle;
        for (int i = 0; i < bulletCount; i++)
        {
            float angleOffset = bulletCount > 1 ? -spread / 2f + spread / (bulletCount - 1) * i : 0f;
            Transform firePoint = ctx.FirePoints[i % ctx.FirePoints.Length];
            Vector3 position = firePoint != null ? firePoint.position : ctx.Origin;
            Quaternion rotation = Quaternion.Euler(0f, 0f, ctx.BaseAngle + angleOffset);
            GameObject bullet = ctx.Pool != null
                ? ctx.Pool.GetFromPool(ctx.Data.ProjectilePrefab, position, rotation)
                : Object.Instantiate(ctx.Data.ProjectilePrefab, position, rotation);

            if (bullet == null)
            {
                continue;
            }

            bullet.transform.localScale = Vector3.one * ctx.ScaleMultiplier * ctx.Data.ProjectileScale;
            if (bullet.TryGetComponent<BulletProjectile>(out var projectile))
            {
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
}
