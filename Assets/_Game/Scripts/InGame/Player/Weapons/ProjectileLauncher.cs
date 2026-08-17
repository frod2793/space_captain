using UnityEngine;

/// <summary>
/// 발사체를 꺼내는 공통 절차. 부채꼴 각도 계산, 발사 위치, 풀 생성, 스케일까지가 같아서
/// 거동마다 반복하지 않도록 한 곳에 모은다.
/// 어떤 컴포넌트를 어떻게 초기화할지는 거동이 각자 정한다.
/// 정적 클래스라 발사당 할당이 없다.
/// </summary>
public static class ProjectileLauncher
{
    /// <summary>bulletCount 발을 부채꼴로 나눌 때 index번째 탄의 각도 오프셋.</summary>
    public static float AngleOffset(int index, int bulletCount, float spreadAngle)
    {
        if (bulletCount <= 1)
        {
            return 0f;
        }

        return -spreadAngle / 2f + spreadAngle / (bulletCount - 1) * index;
    }

    /// <summary>index번째 탄이 나갈 위치. 발사 지점이 없으면 Origin을 쓴다.</summary>
    public static Vector3 SpawnPosition(in WeaponFireContext ctx, int index)
    {
        if (ctx.FirePoints == null || ctx.FirePoints.Length == 0)
        {
            return ctx.Origin;
        }

        Transform firePoint = ctx.FirePoints[index % ctx.FirePoints.Length];
        return firePoint != null ? firePoint.position : ctx.Origin;
    }

    /// <summary>
    /// 풀에서 꺼내 활성화하고 스케일까지 맞춘 오브젝트. 실패하면 null.
    /// 풀이 없으면 Instantiate로 떨어진다.
    /// </summary>
    public static GameObject Spawn(in WeaponFireContext ctx, Vector3 position, Quaternion rotation)
    {
        if (ctx.Data == null || ctx.Data.ProjectilePrefab == null)
        {
            return null;
        }

        GameObject projectile = ctx.Pool != null
            ? ctx.Pool.GetFromPool(ctx.Data.ProjectilePrefab, position, rotation)
            : Object.Instantiate(ctx.Data.ProjectilePrefab, position, rotation);

        if (projectile == null)
        {
            return null;
        }

        // 풀 경로는 이미 활성화하지만 Instantiate 폴백은 프리팹 상태를 따라간다
        if (!projectile.activeSelf)
        {
            projectile.SetActive(true);
        }

        projectile.transform.localScale = Vector3.one * ctx.ScaleMultiplier * ctx.Data.ProjectileScale;
        return projectile;
    }

    /// <summary>기대한 컴포넌트가 없어 쓸 수 없는 오브젝트를 되돌린다.</summary>
    public static void Discard(in WeaponFireContext ctx, GameObject projectile)
    {
        if (projectile == null)
        {
            return;
        }

        if (ctx.Pool != null)
        {
            ctx.Pool.ReturnToPool(projectile);
        }
        else
        {
            Object.Destroy(projectile);
        }
    }
}
