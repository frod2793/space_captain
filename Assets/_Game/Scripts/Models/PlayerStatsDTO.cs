using System;

#region 데이터 모델 (DTO)
/// <summary>
/// [설명]: 플레이어 캐릭터의 상태 및 능력치를 담는 DTO입니다.
/// </summary>
[Serializable]
public class PlayerStatsDTO
{
    public string ID;
    public float MoveSpeed = 10f;
    public int AttackDamage = 20;
    public bool IsActive = false;
    public float CurrentX = 0f;
}
#endregion
