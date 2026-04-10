using System;

[Serializable]
public class BattleProgressDTO
{
    public int TotalKillCount = 0;
    public int CurrentLevelKillCount = 0;
    public int CurrentLevel = 0;
    public float PlayTime = 0f;
    public float BattleSpeed = 1f;
    public int CurrentWave = 1;
}
