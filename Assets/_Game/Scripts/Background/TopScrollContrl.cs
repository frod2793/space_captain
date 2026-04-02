using UnityEngine;

/// <summary>
/// [설명]: 배경 스크롤에 필요한 설정 데이터를 담는 데이터 전송 객체입니다.
/// </summary>
[System.Serializable]
public class BackgroundScrollDTO
{
    [Tooltip("배경이 이동하는 속도입니다.")]
    public float ScrollSpeed = 5.0f;

    [Tooltip("배경 이미지 한 장의 세로 높이입니다. (자동 탐지 실패 시 기본값으로 사용됩니다)")]
    public float BackgroundHeight = 20.0f;
}

/// <summary>
/// [설명]: 유니티에 의존하지 않고 배경의 위치 이동 및 무한 루프 계산을 담당하는 순수 C# 로직 클래스입니다.
/// </summary>
public class BackgroundScrollLogic
{
    /// <summary>
    /// [설명]: 주어진 위치에서 아래 방향으로 이동한 후의 새로운 위치를 계산합니다.
    /// 배경이 화면 하단(임계값)을 벗어나면 다시 최상단으로 이동시킵니다. (3개 배경 기준)
    /// </summary>
    public float GetNextPositionY(float currentY, float deltaTime, float speed, float height)
    {
        // 1. 아래로 이동 계산
        float nextY = currentY - (speed * deltaTime);

        // 2. 임계값 확인 및 루프 처리 (3개의 배경이므로 height * 3.0f 만큼 점프)
        if (nextY <= -height)
        {
            nextY += height * 3.0f;
        }

        return nextY;
    }
}

/// <summary>
/// [설명]: 세로 비율 화면에서 3개의 배경 이미지를 무한히 스크롤하는 뷰 클래스입니다.
/// 이미지 사이즈를 자동으로 탐지하여 스크롤 높이를 설정합니다.
/// </summary>
public class TopScrollContrl : MonoBehaviour
{
    [Header("배경 설정")]
    [Tooltip("첫 번째 배경 이미지의 Transform입니다. 사이즈 탐지의 기준이 됩니다.")]
    [SerializeField] private Transform m_background1;

    [Tooltip("두 번째 배경 이미지의 Transform입니다.")]
    [SerializeField] private Transform m_background2;

    [Tooltip("세 번째 배경 이미지의 Transform입니다.")]
    [SerializeField] private Transform m_background3;

    [Header("스크롤 설정")]
    [SerializeField] private BackgroundScrollDTO m_scrollSettings;

    private BackgroundScrollLogic m_scrollLogic;

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        UpdateScrolling();
    }

    /// <summary>
    /// [설명]: 스크롤 로직을 초기화하고 배경 이미지의 사이즈를 자동 탐지하여 위치를 설정합니다.
    /// </summary>
    private void Initialize()
    {
        m_scrollLogic = new BackgroundScrollLogic();

        if (m_scrollSettings == null)
        {
            m_scrollSettings = new BackgroundScrollDTO();
        }

        // 1. 이미지 사이즈 자동 탐지
        DetectBackgroundHeight();

        float height = m_scrollSettings.BackgroundHeight;

        // 2. 초기 위치 보정 (세 배경이 위아래로 나란히 배치되도록 설정)
        if (m_background1 != null) m_background1.localPosition = new Vector3(0, 0, 0);
        if (m_background2 != null) m_background2.localPosition = new Vector3(0, height, 0);
        if (m_background3 != null) m_background3.localPosition = new Vector3(0, height * 2.0f, 0);
    }

    /// <summary>
    /// [설명]: 첫 번째 배경 오브젝트에서 SpriteRenderer를 찾아 실제 월드 높이를 탐지합니다.
    /// </summary>
    private void DetectBackgroundHeight()
    {
        if (m_background1 == null) return;
        
        if (m_background1.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            // 실제 렌더링되는 스프라이트의 월드 크기(Height)를 가져옴
            m_scrollSettings.BackgroundHeight = spriteRenderer.bounds.size.y;
#if UNITY_EDITOR
            Debug.Log($"[TopScrollContrl] 배경 높이 자동 탐지 성공: {m_scrollSettings.BackgroundHeight}");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("[TopScrollContrl] SpriteRenderer를 찾을 수 없어 에디터의 수동 설정값을 사용합니다.");
#endif
        }
    }

    /// <summary>
    /// [설명]: 매 프레임마다 3개 배경의 위치를 업데이트합니다.
    /// </summary>
    private void UpdateScrolling()
    {
        if (m_background1 == null || m_background2 == null || m_background3 == null) return;

        float dt = Time.deltaTime;
        float speed = m_scrollSettings.ScrollSpeed;
        float height = m_scrollSettings.BackgroundHeight;

        // 각 배경의 위치 계산 및 적용
        m_background1.localPosition = new Vector3(0, m_scrollLogic.GetNextPositionY(m_background1.localPosition.y, dt, speed, height), 0);
        m_background2.localPosition = new Vector3(0, m_scrollLogic.GetNextPositionY(m_background2.localPosition.y, dt, speed, height), 0);
        m_background3.localPosition = new Vector3(0, m_scrollLogic.GetNextPositionY(m_background3.localPosition.y, dt, speed, height), 0);
    }
}
