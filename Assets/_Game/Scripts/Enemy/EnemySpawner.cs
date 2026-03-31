using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

#region 데이터 모델 (DTO)
/// <summary>
/// [설명]: 웨이브 진행에 따른 적 생성 규칙(증가율 등)을 정의하는 데이터 전송 객체입니다.
/// </summary>
[Serializable]
public class WaveConfigDTO
{
    [Header("기본 설정")]
    public GameObject EnemyPrefab;
    public int BaseEnemyCount = 5;
    public float BaseSpawnInterval = 2.0f;

    [Header("증가율 설정 (웨이브당)")]
    [Tooltip("웨이브마다 적의 개체수가 곱해질 비율입니다.")]
    public float CountGrowthRate = 1.2f;
    [Tooltip("웨이브마다 생성 간격이 곱해질 비율입니다.")]
    public float IntervalReductionRate = 0.9f;
    [Tooltip("최소 생성 간격 제한입니다.")]
    public float MinimumInterval = 0.5f;
    [Tooltip("웨이브 종료 후 다음 웨이브 시작까지의 휴식 시간입니다.")]
    public float RestDuration = 3.0f;
}
#endregion

#region 스폰 로직 (POCO)
/// <summary>
/// [설명]: 웨이브 진행 상태 및 스폰 위치 계산을 담당하는 순수 C# 로직 클래스입니다.
/// </summary>
public class EnemySpawnLogic
{
    private WaveConfigDTO m_config;
    private int m_currentWaveIndex = 0;

    public EnemySpawnLogic(WaveConfigDTO config)
    {
        m_config = config;
    }

    /// <summary>
    /// [설명]: 현재 웨이브 번호에 따른 적 생성 수량을 계산합니다.
    /// </summary>
    public int GetEnemyCountForWave(int waveIndex)
    {
        return Mathf.FloorToInt(m_config.BaseEnemyCount * Mathf.Pow(m_config.CountGrowthRate, waveIndex));
    }

    /// <summary>
    /// [설명]: 현재 웨이브 번호에 따른 스폰 간격을 계산합니다.
    /// </summary>
    public float GetSpawnIntervalForWave(int waveIndex)
    {
        float interval = m_config.BaseSpawnInterval * Mathf.Pow(m_config.IntervalReductionRate, waveIndex);
        return Mathf.Max(m_config.MinimumInterval, interval);
    }

    /// <summary>
    /// [설명]: 콜라이더의 Bounds 영역 내에서 랜덤한 X, Y 좌표를 계산합니다.
    /// </summary>
    public Vector3 CalculateRandomSpawnPositionInBounds(Bounds area)
    {
        float randomX = UnityEngine.Random.Range(area.min.x, area.max.x);
        float randomY = UnityEngine.Random.Range(area.min.y, area.max.y);
        return new Vector3(randomX, randomY, 0);
    }
}
#endregion

#region 뷰 (View)
/// <summary>
/// [설명]: 증가율 기반 웨이브 시스템과 오브젝트 풀링을 활용한 적 스포너 클래스입니다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class EnemySpawner : MonoBehaviour
{
    #region 에디터 설정
    [SerializeField] private WaveConfigDTO m_waveConfig;

    [Header("스폰 최적화")]
    [Tooltip("적이 겹치지 않게 생성되도록 체크하는 반경입니다.")]
    [SerializeField] private float m_spawnOverlapRadius = 1.0f;
    [Tooltip("빈 공간을 찾기 위한 최대 시도 횟수입니다.")]
    [SerializeField] private int m_maxSpawnAttempts = 5;
    #endregion

    #region 내부 필드
    private EnemySpawnLogic m_spawnLogic;
    private EnemyPool m_enemyPool;
    
    private float m_currentTimer;
    private int m_currentWaveIndex = 0;
    private int m_remainingEnemiesInWave;
    private float m_currentSpawnInterval;
    private int m_activeEnemyCount;

    private Collider2D m_spawnAreaCollider;
    #endregion

    #region 유니티 생명주기
    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        HandleWaveLogic();
    }
    #endregion

    #region 초기화 로직
    private void Initialize()
    {
        if (m_waveConfig == null) m_waveConfig = new WaveConfigDTO();
        
        m_spawnLogic = new EnemySpawnLogic(m_waveConfig);
        
        // 풀링 초기화
        if (m_waveConfig.EnemyPrefab != null)
        {
            m_enemyPool = new EnemyPool(m_waveConfig.EnemyPrefab, 10, 50, transform);
        }

        m_spawnAreaCollider = GetComponent<Collider2D>();
        if (m_spawnAreaCollider != null) m_spawnAreaCollider.isTrigger = true;

        StartNextWave();
    }

    private void StartNextWave()
    {
        m_remainingEnemiesInWave = m_spawnLogic.GetEnemyCountForWave(m_currentWaveIndex);
        m_currentSpawnInterval = m_spawnLogic.GetSpawnIntervalForWave(m_currentWaveIndex);
        m_currentTimer = m_currentSpawnInterval;
    }
    #endregion

    #region 내부 로직
    private void HandleWaveLogic()
    {
        if (m_enemyPool == null || m_spawnAreaCollider == null) return;

        // 현재 웨이브에서 생성할 적이 남았다면 타이머 업데이트
        if (m_remainingEnemiesInWave > 0)
        {
            m_currentTimer -= Time.deltaTime;
            if (m_currentTimer <= 0f)
            {
                SpawnEnemy();
                m_currentTimer = m_currentSpawnInterval;
                m_remainingEnemiesInWave--;

                // 웨이브의 모든 적 생성이 끝났을 때의 처리는 하지 않음 (모든 적 처치 대기)
                if (m_remainingEnemiesInWave <= 0)
                {
#if UNITY_EDITOR
                    Debug.Log($"[Wave {m_currentWaveIndex + 1}] 모든 적 생성 완료. 남은 적 처치 대기 중...");
#endif
                }
            }
        }
    }

    /// <summary>
    /// [설명]: 적이 처치(풀 반환)될 때 호출되어 웨이브 종료 여부를 확인합니다.
    /// </summary>
    private void OnEnemyDestroyed()
    {
        m_activeEnemyCount--;

        // 모든 적이 생성되었고, 필드에 적이 없다면 다음 웨이브 단계로 진행
        if (m_remainingEnemiesInWave <= 0 && m_activeEnemyCount <= 0)
        {
            m_currentWaveIndex++;
            WaitAndStartNextWaveAsync().Forget(); // [수정]: Invoke 대신 UniTask 사용
        }
    }

    /// <summary>
    /// [설명]: 설정된 휴식 시간만큼 대기한 후 다음 웨이브를 시작합니다.
    /// </summary>
    private async UniTaskVoid WaitAndStartNextWaveAsync()
    {
        try
        {
            // 휴식 시간 대기 (초 단위를 밀리초로 변환)
            await UniTask.Delay(TimeSpan.FromSeconds(m_waveConfig.RestDuration), cancellationToken: this.GetCancellationTokenOnDestroy());
            
            StartNextWave();
        }
        catch (OperationCanceledException)
        {
            // 객체 파괴 등으로 인한 취소 시 안전하게 종료
        }
    }

    private void SpawnEnemy()
    {
        if (m_enemyPool == null || m_spawnAreaCollider == null) return;

        // [개선]: 중첩 방지 로직이 적용된 위치 계산
        Vector3 spawnPos = Vector3.zero;
        bool foundPos = false;

        for (int i = 0; i < m_maxSpawnAttempts; i++)
        {
            Vector3 potentialPos = m_spawnLogic.CalculateRandomSpawnPositionInBounds(m_spawnAreaCollider.bounds);
            
            // 물리 엔진으로 해당 위치에 적이 있는지 체크
            Collider2D hit = Physics2D.OverlapCircle(potentialPos, m_spawnOverlapRadius, 1 << gameObject.layer); // 자신의 레이어(Enemy 추정) 체크
            if (hit == null)
            {
                spawnPos = potentialPos;
                foundPos = true;
                break;
            }
        }

        // 만약 빈 공간을 찾지 못했다면, 마지막 시도한 위치를 사용하거나 스폰을 건너뛸 수 있음 (여기서는 마지막 시도 위치 사용)
        if (!foundPos)
        {
            spawnPos = m_spawnLogic.CalculateRandomSpawnPositionInBounds(m_spawnAreaCollider.bounds);
        }

        GameObject enemyObj = m_enemyPool.Get();
        if (enemyObj == null) return;

        enemyObj.transform.position = spawnPos;
        enemyObj.transform.rotation = Quaternion.identity;

        // 적 컨트롤러에 풀 반환 액션 등록 (개체수 추적 로직 포함)
        if (enemyObj.TryGetComponent<EnemyController>(out var controller))
        {
            m_activeEnemyCount++;
            controller.SetPoolReleaseAction(obj => 
            {
                m_enemyPool.Release(obj);
                OnEnemyDestroyed();
            });
        }
    }
    #endregion
}
#endregion
