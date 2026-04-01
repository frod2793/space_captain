using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// [설명]: 보스 캐릭터를 추적하며 체력 상태를 실시간으로 시각화하는 UI 뷰 클래스
/// </summary>
public class BossHpBar : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Slider m_hpSlider;
    
    [Header("추적 설정")]
    [Tooltip("대상 보스로부터의 오프셋 거리입니다.")]
    [SerializeField] private Vector3 m_offset = new Vector3(0, 2.0f, 0);
    [Tooltip("위치 동기화 시의 부드러움 정도 (0이면 즉시)")]
    [SerializeField] private float m_smoothTime = 0.1f;
    [Tooltip("World Space Canvas 사용 여부")]
    [SerializeField] private bool m_useWorldSpace = true;

    private Transform m_target;
    private Vector3 m_currentVelocity;
    private Camera m_mainCamera;

    private void Awake()
    {
        Init();
    }

    private void LateUpdate()
    {
        if (m_target == null) return;

        // 매 프레임 메인 카메라 유효성 확인
        if (m_mainCamera == null) m_mainCamera = Camera.main;
        if (m_mainCamera == null) return;

        UpdatePosition();
    }

    /// <summary>
    /// [설명]: 캔버스 렌더 모드를 감지하여 추적 모드를 초기화합니다.
    /// </summary>
    private void Init()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            m_useWorldSpace = (parentCanvas.renderMode == RenderMode.WorldSpace);
        }
        
        gameObject.SetActive(false);
    }

    /// <summary>
    /// [설명]: 카메라 공간 좌표를 변환하여 UI 위치를 타겟 캐릭터로 동기화
    /// </summary>
    private void UpdatePosition()
    {
        Vector3 targetPos = m_target.position + m_offset;
        Vector3 finalPos;
        
        if (m_useWorldSpace)
        {
            finalPos = targetPos;
        }
        else
        {
            // 호출 시점에 카메라가 아직 로드되지 않은 경우 대응
            if (m_mainCamera == null) m_mainCamera = Camera.main;
            if (m_mainCamera == null) return; 

            finalPos = m_mainCamera.WorldToScreenPoint(targetPos);
        }

        // SmoothDamp를 사용한 부드러운 위치 추종
        if (m_smoothTime > 0)
        {
            transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref m_currentVelocity, m_smoothTime);
        }
        else
        {
            transform.position = finalPos;
        }
    }

    /// <summary>
    /// [설명]: 추적할 대상 캐릭터를 설정하고 UI를 활성화합니다.
    /// </summary>
    /// <param name="target">추적할 보스의 Transform</param>
    public void SetTarget(Transform target)
    {
        m_target = target;
        if (m_target != null)
        {
            gameObject.SetActive(true);
            UpdatePosition();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// [설명]: 체력 슬라이더 상태를 동기화합니다.
    /// </summary>
    /// <param name="ratio">남은 체력 비율 (0~1)</param>
    public void UpdateHP(float ratio)
    {
        if (m_hpSlider != null)
        {
            m_hpSlider.DOKill();
            m_hpSlider.DOValue(ratio, 0.2f).SetEase(Ease.OutQuad);
        }
    }
}
