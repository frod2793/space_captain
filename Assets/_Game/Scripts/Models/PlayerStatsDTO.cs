using System;

[Serializable]
public class PlayerStatsDTO
{
    public string ID;
    public float MoveSpeed = 10f;
    public int AttackDamage = 20;
    public int MaxHp = 100;
    public int CurrentHp = 100;
    public bool IsActive = false;
    public float CurrentX = 0f;
    public int BulletCountBonus = 0;
    public float SpreadAngleBonus = 0f;
}
