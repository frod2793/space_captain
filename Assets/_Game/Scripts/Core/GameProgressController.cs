using UnityEngine;

/// <summary>
/// [설명]: 게임 진행도를 관리하고 UIManager를 통해 UI를 갱신하는 컨트롤러입니다.
/// </summary>
public class GameProgressController : MonoBehaviour
{
    [Header("UI 매니저 연결")]
    [SerializeField] private UIManager m_uiManager;

    [Header("진행 설정")]
    [SerializeField] private float m_targetDistance = 2000f;
    [SerializeField] private float m_scrollSpeedMultiplier = 5.0f; 

    private ProgressDTO m_progressData;
    private TopScrollContrl m_backgroundScroll;

    private void Awake()
    {
        m_backgroundScroll = FindFirstObjectByType<TopScrollContrl>();
        
        // 데이터 초기화
        m_progressData = new ProgressDTO { TargetDistance = m_targetDistance };

        // 초기 화면 갱신
        if (m_uiManager != null)
        {
            m_uiManager.SetProgressRatio(m_progressData.ProgressRatio);
        }
    }

    private void Update()
    {
        if (m_backgroundScroll == null || m_progressData == null) return;

        // 1. 거리 누적
        float distanceStep = m_scrollSpeedMultiplier * Time.deltaTime; 
        m_progressData.CurrentDistance += distanceStep;

        // 2. UI 갱신 (직접 호출 또는 이벤트 방식)
        if (m_uiManager != null)
        {
            m_uiManager.SetProgressRatio(m_progressData.ProgressRatio);
        }

        // 3. 목표 도달 체크
        if (m_progressData.CurrentDistance >= m_progressData.TargetDistance)
        {
            // [임시]: 목표 도달 시 게임 오버 처리 (또는 클리어 UI)
            if (m_uiManager != null)
            {
                m_uiManager.ShowGameOver();
            }
        }
    }
}
