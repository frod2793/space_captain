using UnityEngine;
using System;

[Serializable]
public class ProgressDTO
{
    public float CurrentDistance;
    public float TargetDistance;
    public float ProgressRatio => Mathf.Clamp01(CurrentDistance / TargetDistance);
}
