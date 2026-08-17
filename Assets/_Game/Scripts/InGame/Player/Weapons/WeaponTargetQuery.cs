using UnityEngine;

public static class WeaponTargetQuery
{
    private const int InitialBufferSize = 32;
    private static RaycastHit2D[] s_raycastHits = new RaycastHit2D[InitialBufferSize];
    private static Collider2D[] s_colliders = new Collider2D[InitialBufferSize];

    public static int BoxCast(
        Vector2 origin,
        Vector2 size,
        float angle,
        Vector2 direction,
        float distance,
        out RaycastHit2D[] hits)
    {
        int count;
        do
        {
            count = Physics2D.BoxCastNonAlloc(origin, size, angle, direction, s_raycastHits, distance);
            if (count < s_raycastHits.Length)
            {
                hits = s_raycastHits;
                return count;
            }

            System.Array.Resize(ref s_raycastHits, s_raycastHits.Length * 2);
        } while (true);
    }

    public static int OverlapCircle(Vector2 origin, float radius, out Collider2D[] colliders)
    {
        int count;
        do
        {
            count = Physics2D.OverlapCircleNonAlloc(origin, radius, s_colliders);
            if (count < s_colliders.Length)
            {
                colliders = s_colliders;
                return count;
            }

            System.Array.Resize(ref s_colliders, s_colliders.Length * 2);
        } while (true);
    }

    public static bool TryGetEnemyTarget(Collider2D collider, out IAttackTarget target)
    {
        target = null;
        if (collider == null)
        {
            return false;
        }

        target = collider.GetComponent<IAttackTarget>() ?? collider.GetComponentInParent<IAttackTarget>();
        if (!IsEnemyTarget(target))
        {
            target = null;
            return false;
        }

        if (!target.IsActiveTarget || target.TargetTransform == null)
        {
            target = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 파괴된 UnityEngine.Object를 살아 있다고 판정하지 않는다.
    /// IAttackTarget은 인터페이스라 != null 비교가 참조 비교로 떨어져
    /// Unity의 파괴 검사를 타지 않는다. 그대로 두면 죽은 적에 접근해
    /// MissingReferenceException이 난다.
    /// </summary>
    public static bool IsAlive(IAttackTarget target)
    {
        if (target is Object unityObject)
        {
            return unityObject != null;
        }

        return target != null;
    }

    public static bool IsEnemyTarget(IAttackTarget target)
    {
        return IsAlive(target) && target.IsActiveTarget && target.TargetTransform != null &&
            (target.TargetTransform.CompareTag("Enemy") || target.TargetTransform.CompareTag("Boss")) &&
            (target is EnemyController || target is BossController);
    }
}
