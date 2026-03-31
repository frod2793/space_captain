using UnityEngine;

#region 내부 로직
/// <summary>
/// [설명]: 적(보스 포함)이 발사하는 탄환의 기본 로직입니다.
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    #region 에디터 설정
    [SerializeField] private float m_speed = 10f;
    [SerializeField] private float m_lifeTime = 5f;
    #endregion

    #region 내부 필드
    private int m_damage;
    #endregion

    #region 유니티 생명주기
    private void Start()
    {
        Destroy(gameObject, m_lifeTime);
    }

    private void Update()
    {
        // 자신의 '위쪽(Up)' 방향으로 전진
        transform.Translate(Vector3.up * m_speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }
    #endregion

    #region 내부 로직
    private void HandleCollision(GameObject other)
    {
        // 1. 플레이어 캐릭터 피격
        if (other.TryGetComponent<PlayerCharacterController>(out var player))
        {
            player.TakeDamage(m_damage);
            Destroy(gameObject);
        }
        // 2. 모선 피격
        else if (other.TryGetComponent<MasterShip>(out var ship))
        {
            ship.TakeDamage(m_damage);
            Destroy(gameObject);
        }
    }
    #endregion

    #region 공개 메서드
    public void Initialize(int damage, float speed = 10f)
    {
        m_damage = damage;
        m_speed = speed;
    }
    #endregion
}
#endregion
