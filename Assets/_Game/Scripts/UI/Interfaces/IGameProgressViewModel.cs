using System;

public interface IGameProgressViewModel
{
    event Action<float> OnProgressChanged;
    event Action OnGameCleared;

    ProgressDTO ProgressData { get; set; }

    void UpdateProgress(float distanceStep);
}
