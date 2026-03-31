using UnityEngine;
using System;
using DG.Tweening;

#region 뷰 (View)
/// <summary>
/// [설명]: 플레이어 캐릭터 개별 오브젝트의 시각적 요소 및 생명주기를 제어하는 클래스입니다.
/// </summary>
public class PlayerCharacterController : MonoBehaviour
{
    #region 에디터 설정
    [Header("시각 요소")]
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private Collider2D m_collider;
    #endregion

    #region 내부 필드
    private PlayerStatsDTO m_stats;
    private float m_targetX;
    #endregion

    #region 프로퍼티
    public event Action<PlayerCharacterController> OnSelected;
    /// <summary>
    /// [설명]: 체력 비율이 변경될 때 발생하는 이벤트입니다. (0.0 ~ 1.0)
    /// </summary>
    public event Action<float> OnHpChanged;
    /// <summary>
    /// [설명]: 캐릭터가 사망(파괴)했을 때 발생하는 이벤트입니다.
    /// </summary>
    public event Action<PlayerCharacterController> OnDead;
    
    public bool IsActive => m_stats != null && m_stats.IsActive;
    public bool IsDragging { get; set; }
    public PlayerStatsDTO Stats => m_stats;
    public Collider2D Collider => m_collider;
    #endregion

    #region 유니티 생명주기
    private void Awake()
    {
        if (m_collider == null)
        {
            m_collider = GetComponent<Collider2D>();
        }
    }

    private void Update()
    {
        HandleMovementUpdate();
    }
    #endregion

    #region 초기화 및 바인딩 로직
    /// <summary>
    /// [설명]: 캐릭터의 통계 데이터와 초기 위치를 설정합니다.
    /// </summary>
    public void Initialize(PlayerStatsDTO stats)
    {
        m_stats = stats;
        m_targetX = transform.position.x;
    }

    /// <summary>
    /// [설명]: 활성화 상태를 변경합니다. (대기 상태 시 무적 판정 및 이동 제한)
    /// </summary>
    public void SetActive(bool isActive)
    {
        if (m_stats == null) return;
        m_stats.IsActive = isActive;
    }
    #endregion

    #region 내부 로직
    /// <summary>
    /// [설명]: 활성 상태일 때 타겟 좌표로의 부드러운 이동을 처리합니다.
    /// </summary>
    private void HandleMovementUpdate()
    {
        if (m_stats == null || !m_stats.IsActive) return;

        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x - m_targetX) > 0.001f)
        {
            pos.x = Mathf.Lerp(pos.x, m_targetX, Time.deltaTime * m_stats.MoveSpeed);
            transform.position = pos;
            m_stats.CurrentX = pos.x;
        }
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// [설명]: 지정된 X 좌표로 이동 명령을 내립니다.
    /// </summary>
    /// <param name="x">타겟 좌표</param>
    /// <param name="immediate">즉시 이동 여부</param>
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

    /// <summary>
    /// [설명]: 외부로부터 데미지를 입습니다. 비활성화(대기) 상태에서는 무적입니다.
    /// </summary>
    /// <param name="damage">입는 데미지 수치</param>
    public void TakeDamage(int damage)
    {
        // [수석 개발자 팁]: 활성화된 캐릭터만 데미지를 처리하여 대기 캐릭터를 보호합니다.
        if (m_stats == null || !m_stats.IsActive) return;

        // 피격 Flash 연출
        if (m_spriteRenderer != null)
        {
            m_spriteRenderer.DOKill();
            m_spriteRenderer.DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo).OnComplete(() => {
                if (m_spriteRenderer != null) m_spriteRenderer.color = Color.white;
            });
        }

        m_stats.CurrentHp = Mathf.Max(0, m_stats.CurrentHp - damage);
        Debug.LogWarning($"[플레이어 피격]: {m_stats.ID} 캐릭터가 {damage} 데미지를 입음. 남은 HP: {m_stats.CurrentHp}");

        float ratio = (float)m_stats.CurrentHp / m_stats.MaxHp;
        OnHpChanged?.Invoke(ratio);

        if (m_stats.CurrentHp <= 0)
        {
            ExecuteDeath();
        }
    }

    /// <summary>
    /// [설명]: 캐릭터 사망 시 시각적/물리적 비활성화를 수행합니다.
    /// </summary>
    private void ExecuteDeath()
    {
        Debug.LogError($"[캐릭터 사망]: {m_stats.ID} 캐릭터가 파괴되었습니다.");
        m_stats.IsActive = false;
        OnDead?.Invoke(this);
        
        if (m_spriteRenderer != null) m_spriteRenderer.enabled = false;
        if (m_collider != null) m_collider.enabled = false;
    }
    #endregion
}
#endregion
