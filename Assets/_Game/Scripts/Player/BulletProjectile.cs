using UnityEngine;

public class BulletProjectile : MonoBehaviour
{
    [SerializeField] private float m_speed = 15f;
    [SerializeField] private float m_lifeTime = 3f;

    private float m_timer;
    private ObjectPoolManager m_pool;

    public int Damage { get; set; }

    private void OnEnable()
    {
        m_timer = 0f;
        if (m_pool == null) m_pool = FindAnyObjectByType<ObjectPoolManager>();
    }

    private void Update()
    {
        transform.Translate(Vector3.up * m_speed * Time.deltaTime);

        m_timer += Time.deltaTime;
        if (m_timer >= m_lifeTime) Release();
    }

    public void SetSpeed(float speed)
    {
        m_speed = speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy")) Release();
    }

    private void Release()
    {
        if (m_pool != null) m_pool.ReturnToPool(gameObject);
        else Destroy(gameObject);
    }
}
