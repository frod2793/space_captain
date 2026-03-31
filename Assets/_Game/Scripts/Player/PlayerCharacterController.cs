using UnityEngine;
using System;

#region 뷰 (View)
/// <summary>
/// [설명]: 플레이어 캐릭터 개별 오브젝트를 제어하는 클래스입니다.
/// </summary>
public class PlayerCharacterController : MonoBehaviour
{
    #region 에디터 설정
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    #endregion

    #region 내부 필드
    private PlayerStatsDTO m_stats;
    private float m_targetX;
    #endregion

    #region 프로퍼티
    public event Action<PlayerCharacterController> OnSelected;
    public bool IsActive => m_stats != null && m_stats.IsActive;
    public bool IsDragging { get; set; } // 현재 조작(드래그) 중인지 여부
    public PlayerStatsDTO Stats => m_stats; // [추가]: 외부에서 능력치에 접근 가능하도록 프로퍼티 제공
    #endregion

    #region 초기화
    public void Initialize(PlayerStatsDTO stats)
    {
        m_stats = stats;
        m_targetX = transform.position.x;
    }
    #endregion

    #region 유니티 생명주기
    private void Update()
    {
        if (m_stats == null) return;

        // 활성 상태일 때만 타겟 위치로 보간 이동 (X축만)
        if (m_stats.IsActive)
        {
            Vector3 pos = transform.position;
            // 타겟과의 거리가 충분히 가깝지 않을 때만 이동 수행 (불필요한 지터 방지)
            if (Mathf.Abs(pos.x - m_targetX) > 0.001f)
            {
                // [참고]: 1:1 조작감을 위해 MoveToX(x, true) 호출 시 즉시 이동하며,
                // 그 외 상황(스왑 등)에서는 MoveSpeed에 비례하여 부드럽게 이동합니다.
                pos.x = Mathf.Lerp(pos.x, m_targetX, Time.deltaTime * m_stats.MoveSpeed);
                transform.position = pos;
                m_stats.CurrentX = pos.x;
            }
        }
    }
    #endregion

    #region 공개 메서드
    public void SetActive(bool isActive)
    {
        if (m_stats == null) return;
        m_stats.IsActive = isActive;
    }

    /// <summary>
    /// [설명]: 지정된 X 좌표로 이동합니다.
    /// </summary>
    /// <param name="x">이동할 타겟 X 좌표</param>
    /// <param name="immediate">즉시 이동할지 여부 (true면 즉각 반응)</param>
    public void MoveToX(float x, bool immediate = false)
    {
        if (m_stats == null || !m_stats.IsActive) return;
        m_targetX = x;
        
        if (immediate)
        {
            Vector3 pos = transform.position;
            pos.x = x;
            transform.position = pos;
            m_stats.CurrentX = x;
        }
    }
    #endregion


}
#endregion
