using UnityEngine;

#region 내부 로직
/// <summary>
/// [설명]: 플레이어의 발사체(Bullet) 기본 로직입니다.
/// </summary>
public class BulletProjectile : MonoBehaviour
{
    #region 에디터 설정
    [SerializeField] private float m_speed = 15f;
    [SerializeField] private float m_lifeTime = 3f;
    #endregion

    #region 유니티 생명주기
    private void Start()
    {
        // 일정 시간 후 자동 소멸 (프로토타입용)
        Destroy(gameObject, m_lifeTime);
    }

    private void Update()
    {
        // 위쪽 방향으로 이동
        transform.Translate(Vector3.up * m_speed * Time.deltaTime);
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// [설명]: 발사체의 속도를 외부에서 설정합니다.
    /// </summary>
    public void SetSpeed(float speed)
    {
        m_speed = speed;
    }

    /// <summary>
    /// [설명]: 발사체의 데미지를 설정하거나 가져옵니다.
    /// </summary>
    public int Damage { get; set; }
    #endregion


}
#endregion
