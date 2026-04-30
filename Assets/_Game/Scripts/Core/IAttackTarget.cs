using UnityEngine;

public interface IAttackTarget
{
    Transform TargetTransform { get; }
    bool IsActiveTarget { get; }
    void TakeDamage(int damage, string damagerID = null);
}
