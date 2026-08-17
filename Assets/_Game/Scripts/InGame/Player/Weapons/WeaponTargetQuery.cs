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

    public static bool IsEnemyTarget(IAttackTarget target)
    {
        return target != null && target.IsActiveTarget && target.TargetTransform != null &&
            (target.TargetTransform.CompareTag("Enemy") || target.TargetTransform.CompareTag("Boss")) &&
            (target is EnemyController || target is BossController);
    }
}
