using UnityEngine;
using System;
using DG.Tweening;

[Serializable]
public class EnemyDTO
{
    public float MoveSpeed = 3.0f;
    public int AttackDamage = 10;
    public int MaxHp = 10;
    public int CurrentHp = 10;
    public bool IsDead = false;
}

public class EnemyLogic
{
    private EnemyDTO m_data;

    public EnemyLogic(EnemyDTO data)
    {
        m_data = data;
    }

    public Vector3 CalculateNextPosition(Vector3 currentPos, Vector3 targetPos, float deltaTime)
    {
        if (m_data == null || m_data.IsDead) return currentPos;

        Vector3 direction = (targetPos - currentPos).normalized;
        return currentPos + (direction * m_data.MoveSpeed * deltaTime);
    }

    public void OnDamaged(int damage)
    {
        if (m_data == null || m_data.IsDead) return;
        m_data.CurrentHp = Math.Max(0, m_data.CurrentHp - damage);
        if (m_data.CurrentHp <= 0) m_data.IsDead = true;
    }
}

public class EnemyController : MonoBehaviour, IPoolable
{
    public static event Action OnEnemyDead;

    [SerializeField] private EnemyDTO m_enemyData;
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private GameObject m_explosionPrefab;

    private EnemyLogic m_logic;
    private MasterShip m_targetMasterShip;
    private Action<GameObject> m_onRelease;

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        UpdateMovement();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void HandleCollision(GameObject other)
    {
        if (other.TryGetComponent<MasterShip>(out var masterShip))
        {
            if (m_enemyData.IsDead) return;
            
            masterShip.TakeDamage(m_enemyData.AttackDamage);
            DestroyEnemy();
        }
        else if (other.TryGetComponent<PlayerCharacterController>(out var player))
        {
            if (m_enemyData.IsDead) return;

            player.TakeDamage(m_enemyData.AttackDamage);
            DestroyEnemy();
        }
        else if (other.TryGetComponent<BulletProjectile>(out var bullet))
        {
            if (m_enemyData.IsDead) return;

            TakeDamage(bullet.Damage);
            Destroy(bullet.gameObject);
        }
    }

    private void Initialize()
    {
        if (m_enemyData == null) m_enemyData = new EnemyDTO();
        m_logic = new EnemyLogic(m_enemyData);

        if (m_spriteRenderer == null) m_spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        m_targetMasterShip = UnityEngine.Object.FindAnyObjectByType<MasterShip>();
    }

    public void OnSpawn()
    {
        if (m_enemyData != null)
        {
            m_enemyData.CurrentHp = m_enemyData.MaxHp;
            m_enemyData.IsDead = false;
        }
    }

    public void OnDespawn()
    {
    }

    public void SetPoolReleaseAction(Action<GameObject> releaseAction)
    {
        m_onRelease = releaseAction;
    }

    public void TakeDamage(int amount)
    {
        if (m_logic == null || m_enemyData.IsDead) return;

        if (m_spriteRenderer != null)
        {
            m_spriteRenderer.DOKill();
            m_spriteRenderer.DOColor(Color.red, 0.1f).SetLoops(2, LoopType.Yoyo).OnComplete(() => {
                if (m_spriteRenderer != null) m_spriteRenderer.color = Color.white;
            });
        }

        m_logic.OnDamaged(amount);

        if (m_enemyData.IsDead)
        {
            OnEnemyDead?.Invoke();
            ExecuteDeathEffectAndRelease();
        }
    }

    private void OnEnable()
    {
        if (m_enemyData != null) m_enemyData.IsDead = false;
    }

    private void UpdateMovement()
    {
        if (m_targetMasterShip == null || m_logic == null || m_enemyData.IsDead) return;

        Vector3 nextPos = m_logic.CalculateNextPosition(transform.position, m_targetMasterShip.transform.position, Time.deltaTime);
        transform.position = nextPos;

        Vector3 direction = (m_targetMasterShip.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    private void ExecuteDeathEffectAndRelease()
    {
        if (m_explosionPrefab != null) Instantiate(m_explosionPrefab, transform.position, Quaternion.identity);

        m_onRelease?.Invoke(gameObject);

        var pool = UnityEngine.Object.FindAnyObjectByType<ObjectPoolManager>();
        if (pool != null) pool.ReturnToPool(gameObject);
        else Destroy(gameObject);
    }

    private void DestroyEnemy()
    {
        if (m_enemyData == null || m_enemyData.IsDead) return;
        m_enemyData.IsDead = true;
        
        ExecuteDeathEffectAndRelease();
    }
}
