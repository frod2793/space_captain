using UnityEngine;
using System;

#region 데이터 모델 (DTO)
/// <summary>
/// [설명]: 적 생성에 필요한 설정 데이터(스폰 간격, 프리팹 등)를 담는 데이터 전송 객체입니다.
/// </summary>
[Serializable]
public class EnemySpawnDTO
{
    [Tooltip("적이 생성되는 시간 간격(초)입니다.")]
    public float SpawnInterval = 2.0f;

    [Tooltip("생성할 적 프리팹입니다.")]
    public GameObject EnemyPrefab;
}
#endregion

#region 스폰 로직 (POCO)
/// <summary>
/// [설명]: 지정된 영역(Bounds) 내에서 랜덤한 스폰 위치를 계산하는 순수 C# 로직 클래스입니다.
/// </summary>
public class EnemySpawnLogic
{
    /// <summary>
    /// [설명]: 콜라이더의 Bounds 영역 내에서 랜덤한 X, Y 좌표를 계산합니다.
    /// </summary>
    /// <param name="area">스폰 가능한 영역의 Bounds</param>
    /// <returns>영역 내 랜덤 월드 좌표</returns>
    public Vector3 CalculateRandomSpawnPositionInBounds(Bounds area)
    {
        // Bounds의 최소(min)와 최대(max) 범위를 사용하여 랜덤 좌표 추출
        float randomX = UnityEngine.Random.Range(area.min.x, area.max.x);
        float randomY = UnityEngine.Random.Range(area.min.y, area.max.y);

        return new Vector3(randomX, randomY, 0);
    }
}
#endregion

#region 뷰 (View)
/// <summary>
/// [설명]: 자신의 콜라이더 영역 내에서 적을 랜덤하게 생성하는 스포너 뷰 클래스입니다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class EnemySpawner : MonoBehaviour
{
    #region 에디터 설정
    [Header("스폰 설정")]
    [SerializeField] private EnemySpawnDTO m_spawnSettings;
    #endregion

    #region 내부 필드
    private EnemySpawnLogic m_spawnLogic;
    private float m_currentTimer;
    private Collider2D m_spawnAreaCollider;
    #endregion

    #region 유니티 생명주기
    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        UpdateSpawnTimer();
    }
    #endregion

    #region 초기화 로직
    /// <summary>
    /// [설명]: 스폰 로직을 초기화하고 자신의 콜라이더 영역을 획득합니다.
    /// </summary>
    private void Initialize()
    {
        m_spawnLogic = new EnemySpawnLogic();
        
        // 자신의 콜라이더를 스폰 영역으로 사용
        m_spawnAreaCollider = GetComponent<Collider2D>();
        
        if (m_spawnAreaCollider != null)
        {
            m_spawnAreaCollider.isTrigger = true;
        }

        if (m_spawnSettings == null)
        {
            m_spawnSettings = new EnemySpawnDTO();
        }

        m_currentTimer = m_spawnSettings.SpawnInterval;
    }
    #endregion

    #region 내부 로직
    /// <summary>
    /// [설명]: 타이머를 업데이트하고 시간이 되면 적을 스폰합니다.
    /// </summary>
    private void UpdateSpawnTimer()
    {
        if (m_spawnSettings.EnemyPrefab == null) return;

        m_currentTimer -= Time.deltaTime;

        if (m_currentTimer <= 0f)
        {
            SpawnEnemy();
            m_currentTimer = m_spawnSettings.SpawnInterval;
        }
    }

    /// <summary>
    /// [설명]: 콜라이더 Bounds 내의 랜덤 위치에 적 오브젝트를 생성합니다.
    /// </summary>
    private void SpawnEnemy()
    {
        if (m_spawnLogic == null || m_spawnAreaCollider == null) return;

        // 콜라이더의 월드 Bounds를 전달하여 랜덤 좌표 획득
        Vector3 spawnPos = m_spawnLogic.CalculateRandomSpawnPositionInBounds(m_spawnAreaCollider.bounds);

        Instantiate(m_spawnSettings.EnemyPrefab, spawnPos, Quaternion.identity);
    }
    #endregion
}
#endregion
