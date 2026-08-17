using System;
using UnityEngine;

public struct WeaponFireContext
{
    public Vector3 Origin;
    public float BaseAngle;
    public int Damage;
    public string OwnerID;
    public Transform[] FirePoints;
    public IAttackTarget Target;
    public ObjectPoolManager Pool;
    public WeaponDataSO Data;
    public int BulletCount;
    public float SpreadAngle;
    public float ScaleMultiplier;
    public Action<IAttackTarget> OnProjectileHit;
}

public interface IWeaponBehaviour
{
    void Fire(in WeaponFireContext ctx);
}
