using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// [설명]: 활성화된 플레이어 캐릭터를 추적하며 체력 상태를 시각화하는 UI 뷰 클래스입니다.
/// </summary>
public class PlayerHUDView : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Slider m_hpSlider;
    
    [Header("추적 설정")]
    [Tooltip("대상 캐릭터로부터의 오프셋 거리입니다.")]
    [SerializeField] private Vector3 m_offset = new Vector3(0, -1.5f, 0);
    [Tooltip("위치 동기화 시의 부드러움 정도 (0이면 즉시)")]
    [SerializeField] private float m_smoothTime = 0.1f;
    [Tooltip("World Space Canvas 사용 여부 (체크 해제 시 Screen Space로 간주)")]
    [SerializeField] private bool m_useWorldSpace = true;

    private Transform m_target;
    private Vector3 m_currentVelocity;
    private Camera m_mainCamera;

    private void Awake()
    {
        m_mainCamera = Camera.main;

        // [추가]: 캔버스 렌더 모드 자동 감지
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            // 렌더 모드가 WorldSpace가 아니라면 ScreenSpace로 간주하여 오프셋 처리 방식 변경
            m_useWorldSpace = (parentCanvas.renderMode == RenderMode.WorldSpace);
        }
        
        // 초기에는 비활성화
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (m_target == null || m_mainCamera == null) return;

        Vector3 targetPos = m_target.position + m_offset;
        Vector3 finalPos;

        if (m_useWorldSpace)
        {
            finalPos = targetPos;
        }
        else
        {
            // Screen Space (Overlay/Camera) 대응
            finalPos = m_mainCamera.WorldToScreenPoint(targetPos);
        }
        
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
    /// [설명]: 추적할 대상 캐릭터를 설정합니다. 대상을 변경하면 UI를 즉시 활성화합니다.
    /// </summary>
    /// <param name="target">추적할 Transform</param>
    public void SetTarget(Transform target)
    {
        m_target = target;
        
        if (m_target != null)
        {
            gameObject.SetActive(true);
            
            // 타겟 변경 시 위치 즉시 초기화 (Damp 지연 방지)
            Vector3 targetPos = m_target.position + m_offset;
            transform.position = m_useWorldSpace ? targetPos : m_mainCamera.WorldToScreenPoint(targetPos);

            // [추가]: 초기 체력 상태를 즉시 반영 (0에서 채워지는 현상 방지)
            if (m_target.TryGetComponent<PlayerCharacterController>(out var controller))
            {
                if (m_hpSlider != null && controller.Stats != null)
                {
                    float ratio = (float)controller.Stats.CurrentHp / controller.Stats.MaxHp;
                    m_hpSlider.DOKill();
                    m_hpSlider.value = ratio;
                }
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// [설명]: 체력 슬라이더의 값을 부드럽게 갱신합니다.
    /// </summary>
    /// <param name="ratio">현재 체력 비율 (0.0 ~ 1.0)</param>
    public void UpdateHP(float ratio)
    {
        if (m_hpSlider != null)
        {
            m_hpSlider.DOKill();
            m_hpSlider.DOValue(ratio, 0.25f).SetEase(Ease.OutQuad);
        }
    }
}
