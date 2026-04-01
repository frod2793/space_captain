using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// [설명]: 보스 캐릭터의 핵심 데이터(체력, 공격력, 이동 속도 등)를 담는 순수 데이터 객체입니다.
/// </summary>
[Serializable]
public class BossDTO
{
    public string Name = "Elite Guardian";
    public float MoveSpeed = 1.5f;
    public int AttackDamage = 20;
    public int MaxHp = 500;
    public int CurrentHp = 500;
    public bool IsDead = false;
}

/// <summary>
/// [설명]: 보스의 이동 연산 및 데미지 계산 등 핵심 비즈니스 로직을 담당하는 클래스입니다.
/// </summary>
public class BossLogic
{
    private BossDTO m_data;

    public BossLogic(BossDTO data)
    {
        m_data = data;
    }

    /// <summary>
    /// [설명]: 현재 위치와 타겟 위치를 바탕으로 다음 이동 좌표를 계산합니다.
    /// </summary>
    public Vector3 CalculateNextPosition(Vector3 currentPos, Vector3 targetPos, float deltaTime)
    {
        if (m_data == null || m_data.IsDead) return currentPos;

        Vector3 direction = (targetPos - currentPos).normalized;
        return currentPos + (direction * m_data.MoveSpeed * deltaTime);
    }

    /// <summary>
    /// [설명]: 데미지를 처리하고 남은 체력 비율을 반환합니다.
    /// </summary>
    public float OnDamaged(int damage)
    {
        if (m_data == null || m_data.IsDead) return 0f;
        
        m_data.CurrentHp = Math.Max(0, m_data.CurrentHp - damage);
        if (m_data.CurrentHp <= 0) m_data.IsDead = true;

        return (float)m_data.CurrentHp / m_data.MaxHp;
    }
}

/// <summary>
/// [설명]: 보스 오브젝트의 시각적 제어 및 입력을 담당하는 메인 컨트롤러 클래스
/// 활성 플레이어를 우선적으로 타겟팅하며 사격 패턴을 실행
/// </summary>
public class BossController : MonoBehaviour
{
    [Header("보스 데이터")]
    [SerializeField] private BossDTO m_bossData;

    [Header("시각 효과")]
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private GameObject m_explosionPrefab;
    [SerializeField] private BossHUDView m_bossHUD;

    [Header("전투 설정")]
    [SerializeField] private GameObject m_bulletPrefab;
    [SerializeField] private float m_fireRate = 1.0f;
    [SerializeField] private Transform m_firePoint;

    /// <summary>
    /// [설명]: 보스가 파괴되었을 때 발생하는 이벤트입니다.
    /// </summary>
    public event Action OnDefeated;

    private BossLogic m_logic;
    private Transform m_currentTarget;
    private MasterShip m_masterShip;
    private List<PlayerCharacterController> m_players = new List<PlayerCharacterController>();
    private UIManager m_uiManager;
    private float m_fireTimer;

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        BindHUD();
    }

    private void Update()
    {
        if (m_bossData.IsDead) return;

        UpdateTargeting();
        UpdateMovement();
        UpdateShooting();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null) HandleCollision(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null) HandleCollision(collision.gameObject);
    }

    /// <summary>
    /// [설명]: 보스의 핵심 데이터 및 의존 객체들을 초기화합니다.
    /// </summary>
    private void Init()
    {
        if (m_bossData == null) m_bossData = new BossDTO();
        m_logic = new BossLogic(m_bossData);

        if (m_spriteRenderer == null) m_spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        m_masterShip = UnityEngine.Object.FindAnyObjectByType<MasterShip>();
        m_players.AddRange(UnityEngine.Object.FindObjectsByType<PlayerCharacterController>(FindObjectsSortMode.None));
        m_uiManager = UnityEngine.Object.FindAnyObjectByType<UIManager>();

        if (m_bossHUD == null)
        {
            m_bossHUD = UnityEngine.Object.FindFirstObjectByType<BossHUDView>(FindObjectsInactive.Include);
        }
    }

    /// <summary>
    /// [설명]: 캐릭터를 추적하는 HUD 인터페이스를 연결합니다.
    /// </summary>
    private void BindHUD()
    {
        if (m_bossHUD != null)
        {
            m_bossHUD.SetTarget(transform);
            m_bossHUD.UpdateHP(1.0f);
        }

        if (m_uiManager != null)
        {
            m_uiManager.UpdateBossHpBar(1.0f);
        }
    }

    /// <summary>
    /// [설명]: 타겟팅 로직을 수행합니다. (현재 활성화된 플레이어 한정)
    /// </summary>
    private void UpdateTargeting()
    {
        PlayerCharacterController activePlayer = m_players.Find(p => p != null && p.IsActive);

        if (activePlayer != null)
        {
            m_currentTarget = activePlayer.transform;
        }
        else
        {
            m_currentTarget = null;
        }
    }

    /// <summary>
    /// [설명]: 타겟 방향으로 이동 및 회전을 처리합니다.
    /// </summary>
    private void UpdateMovement()
    {
        if (m_currentTarget == null || m_logic == null) return;

        Vector3 nextPos = m_logic.CalculateNextPosition(
            transform.position,
            m_currentTarget.position,
            Time.deltaTime);

        transform.position = nextPos;

        Vector3 direction = (m_currentTarget.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    /// <summary>
    /// [설명]: 사격 타이머를 관리하고 주기적으로 발사합니다.
    /// </summary>
    private void UpdateShooting()
    {
        if (m_currentTarget == null || m_bulletPrefab == null) return;

        m_fireTimer += Time.deltaTime;
        if (m_fireTimer >= m_fireRate)
        {
            m_fireTimer = 0f;
            Shoot();
        }
    }

    /// <summary>
    /// [설명]: 설정된 발사 지점에서 탄환을 생성합니다.
    /// </summary>
    private void Shoot()
    {
        if (m_firePoint == null) return;

        GameObject bulletObj = Instantiate(m_bulletPrefab, m_firePoint.position, m_firePoint.rotation);
        if (bulletObj != null && bulletObj.TryGetComponent<EnemyBullet>(out var enemyBullet))
        {
            enemyBullet.Initialize(m_bossData.AttackDamage);
        }
    }

    /// <summary>
    /// [설명]: 객체 충돌 시 후속 처리를 담당합니다.
    /// </summary>
    private void HandleCollision(GameObject other)
    {
        if (m_bossData.IsDead) return;

        if (other.TryGetComponent<PlayerCharacterController>(out var player))
        {
            player.TakeDamage(m_bossData.AttackDamage);
        }
        else if (other.TryGetComponent<BulletProjectile>(out var bullet))
        {
            TakeDamage(bullet.Damage);
            Destroy(bullet.gameObject);
        }
    }

    /// <summary>
    /// [설명]: 보스 파괴 연출을 수행하고 객체를 제거합니다.
    /// </summary>
    private void DestroyBoss()
    {
        m_bossData.IsDead = true;

        if (m_explosionPrefab != null)
        {
            Instantiate(m_explosionPrefab, transform.position, Quaternion.identity);
        }

        if (m_uiManager != null) m_uiManager.UpdateBossHpBar(0f);
        
        OnDefeated?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>
    /// [설명]: 데미지를 입고 피격 연출을 활성화
    /// </summary>
    /// <param name="amount">입는 데미지 수치</param>
    public void TakeDamage(int amount)
    {
        if (m_bossData.IsDead) return;

        // 피격 시각 효과 (Flash)
        if (m_spriteRenderer != null)
        {
            m_spriteRenderer.DOKill();
            m_spriteRenderer.DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo).OnComplete(() => {
                if (m_spriteRenderer != null) m_spriteRenderer.color = Color.white;
            });
        }

        float hpRatio = m_logic.OnDamaged(amount);

        if (m_bossHUD != null) m_bossHUD.UpdateHP(hpRatio);
        if (m_uiManager != null) m_uiManager.UpdateBossHpBar(hpRatio);

        if (m_bossData.IsDead)
        {
            if (m_bossHUD != null) m_bossHUD.gameObject.SetActive(false);
            DestroyBoss();
        }
    }
}
