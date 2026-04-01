using System;

/// <summary>
/// [설명]: 플레이어 캐릭터의 상태 및 능력치를 담는 DTO입니다.
/// </summary>
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
}
