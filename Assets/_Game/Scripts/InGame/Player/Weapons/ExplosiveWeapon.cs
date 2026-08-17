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
        float spread = ctx.SpreadAngle;
        Vector2 direction = Quaternion.Euler(0f, 0f, ctx.BaseAngle) * Vector2.up;

        for (int i = 0; i < bulletCount; i++)
        {
            float angleOffset = bulletCount > 1
                ? -spread / 2f + spread / (bulletCount - 1) * i
                : 0f;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angleOffset) * direction;
            Transform firePoint = ctx.FirePoints != null && ctx.FirePoints.Length > 0
                ? ctx.FirePoints[i % ctx.FirePoints.Length]
                : null;
            Vector3 position = firePoint != null ? firePoint.position : ctx.Origin;
            Vector3 impactPosition = ctx.Target != null && ctx.Target.IsActiveTarget
                ? ctx.Target.TargetTransform.position
                : position + (Vector3)shotDirection * ctx.Data.Range;
            Quaternion rotation = Quaternion.Euler(0f, 0f, ctx.BaseAngle + angleOffset);
            GameObject projectileObject = ctx.Pool != null
                ? ctx.Pool.GetFromPool(ctx.Data.ProjectilePrefab, position, rotation)
                : Object.Instantiate(ctx.Data.ProjectilePrefab, position, rotation);

            if (projectileObject == null)
            {
                continue;
            }

            if (!projectileObject.TryGetComponent<ExplosiveProjectile>(out var projectile))
            {
                if (ctx.Pool != null)
                {
                    ctx.Pool.ReturnToPool(projectileObject);
                }
                else
                {
                    Object.Destroy(projectileObject);
                }
                continue;
            }

            projectileObject.SetActive(true);
            projectileObject.transform.localScale = Vector3.one * ctx.ScaleMultiplier * ctx.Data.ProjectileScale;
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
