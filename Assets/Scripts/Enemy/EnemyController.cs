using UnityEngine;
using System;

#region 데이터 모델 (DTO)
/// <summary>
/// [설명]: 적 캐릭터의 핵심 데이터(속도, 공격력, 체력 등)를 담는 데이터 전송 객체입니다.
/// </summary>
[Serializable]
public class EnemyDTO
{
    public float MoveSpeed = 3.0f;
    public int AttackDamage = 10;
    public int MaxHp = 10;
    public int CurrentHp = 10;
    public bool IsDead = false;
}
#endregion

#region 적 로직 (POCO)
/// <summary>
/// [설명]: 적의 이동 및 상태 변화 로직을 담당하는 순수 C# 클래스입니다.
/// </summary>
public class EnemyLogic
{
    private EnemyDTO m_data;

    public EnemyLogic(EnemyDTO data)
    {
        m_data = data;
    }

    /// <summary>
    /// [설명]: 목표 지점을 향해 이동할 때의 다음 위치를 계산합니다.
    /// </summary>
    /// <param name="currentPos">현재 위치</param>
    /// <param name="targetPos">목표 위치</param>
    /// <param name="deltaTime">프레임 시간</param>
    /// <returns>이동 후의 새로운 위치</returns>
    public Vector3 CalculateNextPosition(Vector3 currentPos, Vector3 targetPos, float deltaTime)
    {
        if (m_data == null || m_data.IsDead) return currentPos;

        Vector3 direction = (targetPos - currentPos).normalized;
        return currentPos + (direction * m_data.MoveSpeed * deltaTime);
    }

    /// <summary>
    /// [설명]: 적이 데미지를 입었을 때 처리합니다.
    /// </summary>
    public void OnDamaged(int damage)
    {
        if (m_data == null || m_data.IsDead) return;
        m_data.CurrentHp = Math.Max(0, m_data.CurrentHp - damage);
        if (m_data.CurrentHp <= 0) m_data.IsDead = true;
    }
}
#endregion

#region 뷰 (View)
/// <summary>
/// [설명]: 실제 적 오브젝트를 제어하고 모선과의 상호작용을 처리하는 뷰 클래스입니다.
/// </summary>
public class EnemyController : MonoBehaviour
{
    #region 에디터 설정
    [Header("적 설정")]
    [SerializeField] private EnemyDTO m_enemyData;

    [Header("효과 설정")]
    [SerializeField] private GameObject m_explosionPrefab;
    #endregion

    #region 내부 필드
    private EnemyLogic m_logic;
    private MasterShip m_targetMasterShip;
    #endregion

    #region 유니티 생명주기
    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        UpdateMovement();
    }

    /// <summary>
    /// [설명]: 적이 모선이나 플레이어 총알과 충돌했을 때의 처리입니다.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 모선과 충돌 시 처리
        if (collision.TryGetComponent<MasterShip>(out var masterShip))
        {
            masterShip.TakeDamage(m_enemyData.AttackDamage);
            DestroyEnemy();
        }
        // 2. 플레이어 총알과 충돌 시 처리 (데미지 로직 단일화)
        else if (collision.TryGetComponent<BulletProjectile>(out var bullet))
        {
            TakeDamage(10); // 데미지 수치 적용
            Destroy(bullet.gameObject); // 총알 제거 주체를 적(Enemy)으로 설정
        }
    }
    #endregion

    #region 초기화 및 바인딩 로직
    /// <summary>
    /// [설명]: 데이터와 로직을 연결하고 목표 대상(모선)을 찾습니다.
    /// </summary>
    private void Initialize()
    {
        if (m_enemyData == null) m_enemyData = new EnemyDTO();
        m_logic = new EnemyLogic(m_enemyData);

        // 씬 내의 모선을 찾습니다. (유니티 API 사용)
        m_targetMasterShip = UnityEngine.Object.FindAnyObjectByType<MasterShip>();
    }
    #endregion

    #region 공개 메서드
    public void TakeDamage(int amount)
    {
        if (m_logic == null) return;
        m_logic.OnDamaged(amount);

        if (m_enemyData.IsDead)
        {
            DestroyEnemy();
        }
    }
    #endregion

    #region 내부 로직
    /// <summary>
    /// [설명]: 매 프레임마다 모선을 향해 이동합니다.
    /// </summary>
    private void UpdateMovement()
    {
        if (m_targetMasterShip == null || m_logic == null || m_enemyData.IsDead) return;

        Vector3 nextPos = m_logic.CalculateNextPosition(
            transform.position, 
            m_targetMasterShip.transform.position, 
            Time.deltaTime);
            
        transform.position = nextPos;

        // 목표를 향해 회전
        Vector3 direction = (m_targetMasterShip.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f); // 스프라이트 위쪽 방향 보정
    }

    private void DestroyEnemy()
    {
        if (m_explosionPrefab != null)
        {
            Instantiate(m_explosionPrefab, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }
    #endregion
}
#endregion
