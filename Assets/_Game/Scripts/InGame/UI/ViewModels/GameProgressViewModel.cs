using System;

public class GameProgressViewModel : IGameProgressViewModel
{
    public event Action<float> OnProgressChanged;
    public event Action OnGameCleared;

    public ProgressDTO ProgressData { get; set; }

    public void UpdateProgress(float distanceStep)
    {
        if (ProgressData == null)
        {
            return;
        }

        ProgressData.CurrentDistance += distanceStep;
        OnProgressChanged?.Invoke(ProgressData.ProgressRatio);

        if (ProgressData.CurrentDistance >= ProgressData.TargetDistance)
        {
            OnGameCleared?.Invoke();
        }
    }
}
