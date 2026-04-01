using System;

/// <summary>
/// [설명]: 게임 진행 거리를 관리하는 순수 데이터 객체입니다.
/// </summary>
[Serializable]
public class ProgressDTO
{
    public float CurrentDistance;
    public float TargetDistance = 1000f; // 목표 거리 (스테이지 길이)
    
    public float ProgressRatio => Math.Clamp(CurrentDistance / TargetDistance, 0f, 1f);
}
