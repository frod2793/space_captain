using UnityEngine;

/// <summary>
/// [설명]: 적(보스 포함)이 발사하는 탄환의 기본 로직입니다.
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float m_speed = 10f;
    [SerializeField] private float m_lifeTime = 5f;

    private int m_damage;
    private float m_timer;
    private Rigidbody2D m_rb;
    private ObjectPoolManager m_pool;

    private void OnEnable()
    {
        m_timer = 0f;

        if (m_pool == null)
        {
            m_pool = UnityEngine.Object.FindAnyObjectByType<ObjectPoolManager>();
        }

        m_rb = GetComponent<Rigidbody2D>();
        if (m_rb != null)
        {
            m_rb.bodyType = RigidbodyType2D.Kinematic;
            m_rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            m_rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void FixedUpdate()
    {
        Vector3 nextPos = transform.position + (transform.up * m_speed * Time.fixedDeltaTime);
        
        if (m_rb != null)
        {
            m_rb.MovePosition(nextPos);
        }
        else
        {
            transform.position = nextPos;
        }

        m_timer += Time.fixedDeltaTime;
        if (m_timer >= m_lifeTime)
        {
            Release();
        }
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
        Barrier barrier = other.GetComponentInParent<Barrier>();
        if (barrier != null)
        {
            barrier.ResolveDamage(m_damage);
            Release();
            return;
        }

        if (other.TryGetComponent<PlayerCharacterController>(out var player))
        {
            player.TakeDamage(m_damage);
            Release();
        }
        else if (other.TryGetComponent<MasterShip>(out var ship))
        {
            ship.TakeDamage(m_damage);
            Release();
        }
    }

    private void Release()
    {
        if (m_pool != null)
        {
            m_pool.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(int damage, float speed = 10f)
    {
        m_damage = damage;
        m_speed = speed;
    }
}
