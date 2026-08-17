using UnityEngine;

public class GameProgressController : MonoBehaviour
{
    [Header("진행 설정")]
    [SerializeField] private float m_targetDistance = 2000f;
    [SerializeField] private float m_scrollSpeedMultiplier = 5.0f;

    public IGameProgressViewModel ViewModel { get; set; }
    private TopScrollContrl m_backgroundScroll;

    private void Awake()
    {
        Application.targetFrameRate = 120;
        m_backgroundScroll = FindFirstObjectByType<TopScrollContrl>();
    }

    public void Init()
    {
        if (ViewModel == null || ViewModel.ProgressData == null)
        {
            return;
        }

        ViewModel.ProgressData.TargetDistance = m_targetDistance;
    }

    private void Update()
    {
        if (m_backgroundScroll == null || ViewModel == null)
        {
            return;
        }

        float distanceStep = m_scrollSpeedMultiplier * Time.deltaTime;
        ViewModel.UpdateProgress(distanceStep);
    }
}
