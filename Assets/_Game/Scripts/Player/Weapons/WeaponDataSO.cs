using UnityEngine;

public enum WeaponBehaviourType
{
    Straight,
    Beam,
    Explosive,
    Chain
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "SpaceCaptain/WeaponData")]
public class WeaponDataSO : ScriptableObject
{
    public string WeaponID;
    public string DisplayName;
    public WeaponBehaviourType Behaviour;
    public GameObject ProjectilePrefab;

    [Header("공통")]
    public float FireRate = 0.5f;
    public int BulletCount = 1;
    public float SpreadAngle;
    public float ProjectileSpeed = 15f;
    public float Range = 10f;
    public float DamageMultiplier = 1f;

    [Header("최대 대상 — 저격총 / 검")]
    public int MaxTargets = 1;
    public float PierceDamageRate = 1f;

    [Header("거리 감쇠 — 샷건")]
    public float DamageFalloffRate;

    [Header("연사 — 기관총")]
    public float WarmupTime;
    public float MaxFireRate;

    public float ProjectileScale = 1f;

    [Header("빔 — 레이저")]
    public float BeamWidth = 1f;
    public float BeamRange = 20f;
    public GameObject BeamVisualPrefab;

    [Header("폭발 — 유탄")]
    public float ExplosionRadius;

    [Header("연쇄 — 지팡이")]
    public int ChainCount;
    public float ChainRange = 3f;
    public float ChainDamageRate = 0.7f;
}
