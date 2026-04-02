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
    private ObjectPoolManager m_pool;

    private void OnEnable()
    {
        m_timer = 0f;
        if (m_pool == null) m_pool = UnityEngine.Object.FindAnyObjectByType<ObjectPoolManager>();
    }

    private void Update()
    {
        transform.Translate(Vector3.up * m_speed * Time.deltaTime);

        m_timer += Time.deltaTime;
        if (m_timer >= m_lifeTime) Release();
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
        // 1. 플레이어 캐릭터 피격
        if (other.TryGetComponent<PlayerCharacterController>(out var player))
        {
            player.TakeDamage(m_damage);
            Release();
        }
        // 2. 모선 피격
        else if (other.TryGetComponent<MasterShip>(out var ship))
        {
            ship.TakeDamage(m_damage);
            Release();
        }
    }

    private void Release()
    {
        if (m_pool != null) m_pool.ReturnToPool(gameObject);
        else Destroy(gameObject);
    }

    public void Initialize(int damage, float speed = 10f)
    {
        m_damage = damage;
        m_speed = speed;
    }
}
