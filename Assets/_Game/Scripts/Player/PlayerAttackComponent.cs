using UnityEngine;

#region 데이터 모델 (DTO)
public enum PlayerAttackType
{
    Single,     // 단발
    Double,     // 더블 배럴
    Spread      // 3발 방사
}
#endregion

#region 뷰 (View)
/// <summary>
/// [설명]: 플레이어 캐릭터의 공격을 담당하는 컴포넌트입니다.
/// 활성 상태(IsActive)일 때만 동작합니다.
/// </summary>
public class PlayerAttackComponent : MonoBehaviour
{
    #region 에디터 설정
    [Header("설정")]
    [SerializeField] private PlayerCharacterController m_owner;
    [SerializeField] private PlayerAttackType m_attackType = PlayerAttackType.Single;
    [SerializeField] private GameObject m_bulletPrefab;
    
    [Header("성능")]
    [SerializeField] private float m_fireRate = 0.2f;
    [SerializeField] private float m_bulletSpeed = 15f;
    [SerializeField] private float m_targetingRange = 15f; // [추가]: 자동 타겟팅 사거리

    [Header("발사 위치")]
    [SerializeField] private Transform[] m_firePoints; // 0: 중앙, 1: 왼쪽, 2: 오른쪽
    #endregion

    #region 프로퍼티
    public Transform CurrentTarget { get; set; } // [추가]: 현재 조준 중인 타겟 (스왑 시 상속용)
    #endregion

    #region 내부 필드
    private float m_fireTimer;
    #endregion

    #region 유니티 생명주기
    private void Awake()
    {
        if (m_owner == null) m_owner = GetComponent<PlayerCharacterController>();
    }

    private void Update()
    {
        if (m_owner == null || !m_owner.IsActive) return;

        // [개선]: 타겟 정보를 매 프레임 업데이트하여 사격 여부 판단 근거로 사용
        UpdateCurrentTarget();

        // [수정]: 사격 조건 판단 (드래그 중이거나, 사거리 내에 타겟이 있을 때만 타이머 진행)
        bool shouldFire = m_owner.IsDragging || (CurrentTarget != null);
        
        if (shouldFire)
        {
            m_fireTimer += Time.deltaTime;
            if (m_fireTimer >= m_fireRate)
            {
                Fire();
                m_fireTimer = 0f;
            }
        }
        else
        {
            // 타겟이 없으면 타이머를 초기화하거나 대기 상태 유지 (여기서는 타이머 유지하여 적 발견 시 즉시 사격)
            // m_fireTimer = m_fireRate; // 적 발견 시 즉시 사격하고 싶다면 주석 해제
        }
    }

    /// <summary>
    /// [설명]: 에디터에서 사거리를 시각적으로 확인하기 위한 기즈모를 그립니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, m_targetingRange);
    }
    #endregion

    #region 내부 로직
    private void Fire()
    {
        if (m_bulletPrefab == null) return;

        // [수정]: 타겟 업데이트는 Update에서 수행하므로 여기선 사용만 함
        float baseAngle = 0f;

        // [수정]: 드래그 중이 아닐 때만 추적된 타겟 방향으로 조준
        if (m_owner != null && !m_owner.IsDragging && CurrentTarget != null)
        {
            Vector3 direction = (CurrentTarget.position - m_firePoints[0].position).normalized;
            baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        }

        switch (m_attackType)
        {
            case PlayerAttackType.Single:
                CreateBullet(m_firePoints[0].position, baseAngle);
                break;

            case PlayerAttackType.Double:
                if (m_firePoints.Length >= 2)
                {
                    CreateBullet(m_firePoints[0].position, baseAngle);
                    CreateBullet(m_firePoints[1].position, baseAngle);
                }
                break;

            case PlayerAttackType.Spread:
                CreateBullet(m_firePoints[0].position, baseAngle);
                CreateBullet(m_firePoints[0].position, baseAngle - 15f);
                CreateBullet(m_firePoints[0].position, baseAngle + 15f);
                break;
        }
    }

    /// <summary>
    /// [설명]: 사거리 내에서 가장 가까운 적을 탐색합니다.
    /// </summary>
    private Transform FindNearestEnemy()
    {
        Collider2D[] observers = Physics2D.OverlapCircleAll(transform.position, m_targetingRange);
        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (var col in observers)
        {
            if (col.CompareTag("Enemy") || col.GetComponent<EnemyController>() != null)
            {
                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = col.transform;
                }
            }
        }
        return nearest;
    }

    /// <summary>
    /// [설명]: 실시간으로 타겟 상태를 확인하고 필요 시 재탐색합니다.
    /// </summary>
    private void UpdateCurrentTarget()
    {
        if (CurrentTarget == null || !CurrentTarget.gameObject.activeInHierarchy || 
            Vector2.Distance(transform.position, CurrentTarget.position) > m_targetingRange)
        {
            CurrentTarget = FindNearestEnemy();
        }
    }

    private void CreateBullet(Vector3 position, float angle)
    {
        GameObject bulletObj = Instantiate(m_bulletPrefab, position, Quaternion.Euler(0, 0, angle));
        if (bulletObj.TryGetComponent<BulletProjectile>(out var projectile))
        {
            projectile.SetSpeed(m_bulletSpeed);
            
            // [추가]: 플레이어의 현재 데미지 수치를 총알에 주입
            if (m_owner != null && m_owner.Stats != null)
            {
                projectile.Damage = m_owner.Stats.AttackDamage;
            }
        }
    }
    #endregion
}
#endregion
