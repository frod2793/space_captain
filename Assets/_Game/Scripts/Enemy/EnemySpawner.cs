using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

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

    [Header("보스 특별 설정")]
    [Tooltip("보스가 출현할 웨이브 번호입니다. (1부터 시작)")]
    public int BossArrivalWave = 3;
}

/// <summary>
/// [설명]: 웨이브 진행 상태 및 스폰 위치 계산을 담당하는 순수 C# 로직 클래스입니다.
/// </summary>
public class EnemySpawnLogic
{
    private WaveConfigDTO m_config;

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

/// <summary>
/// [설명]: 증가율 기반 웨이브 시스템과 오브젝트 풀링을 활용한 적 스포너 클래스입니다.
/// 보스 출현 시 일반 적 생성을 일시 중지하고, 처치 후 재개하는 로직을 포함합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private WaveConfigDTO m_waveConfig;

    [Header("스폰 최적화")]
    [Tooltip("적이 겹치지 않게 생성되도록 체크하는 반경입니다.")]
    [SerializeField] private float m_spawnOverlapRadius = 1.0f;
    [Tooltip("빈 공간을 찾기 위한 최대 시도 횟수입니다.")]
    [SerializeField] private int m_maxSpawnAttempts = 5;

    [Header("보스 설정")]
    [SerializeField] private GameObject m_bossPrefab;
    [SerializeField] private Transform m_bossSpawnPoint;

    private EnemySpawnLogic m_spawnLogic;
    
    private float m_currentTimer;
    private int m_currentWaveIndex = 0;
    private int m_remainingEnemiesInWave;
    private float m_currentSpawnInterval;
    private int m_activeEnemyCount;

    private Collider2D m_spawnAreaCollider;
    private bool m_isBossSpawned;

    private void Awake()
    {
        Init();
    }

    private void Update()
    {
        HandleWaveLogic();
    }

    /// <summary>
    /// [설명]: 시스템 구성 요소와 오브젝트 풀을 초기화합니다.
    /// </summary>
    private void Init()
    {
        if (m_waveConfig == null) m_waveConfig = new WaveConfigDTO();
        m_spawnLogic = new EnemySpawnLogic(m_waveConfig);
        m_spawnAreaCollider = GetComponent<Collider2D>();
        if (m_spawnAreaCollider != null) m_spawnAreaCollider.isTrigger = true;
        StartNextWave();
    }

    /// <summary>
    /// [설명]: 다음 웨이브 단계를 위한 파라미터를 설정하고 보스 스폰 여부를 체크합니다.
    /// </summary>
    private void StartNextWave()
    {
        m_remainingEnemiesInWave = m_spawnLogic.GetEnemyCountForWave(m_currentWaveIndex);
        m_currentSpawnInterval = m_spawnLogic.GetSpawnIntervalForWave(m_currentWaveIndex);
        m_currentTimer = m_currentSpawnInterval;

        CheckBossSpawn();
    }

    /// <summary>
    /// [설명]: 현재 웨이브 번호를 기반으로 보스 출현 조건을 확인합니다.
    /// </summary>
    private void CheckBossSpawn()
    {
        if (m_waveConfig != null && m_currentWaveIndex + 1 == m_waveConfig.BossArrivalWave && !m_isBossSpawned)
        {
            SpawnBoss();
        }
    }

    /// <summary>
    /// [설명]: 보스 캐릭터를 소환하고 처치 이벤트를 구독
    /// </summary>
    private void SpawnBoss()
    {
        if (m_bossPrefab == null) return;

        Vector3 spawnPos = m_bossSpawnPoint != null ? m_bossSpawnPoint.position : transform.position;
        GameObject bossObj = Instantiate(m_bossPrefab, spawnPos, Quaternion.identity);
        
        m_isBossSpawned = true;

        if (bossObj.TryGetComponent<BossController>(out var bossController))
        {
            bossController.OnDefeated += HandleBossDefeated;
        }

        Debug.LogWarning($"[웨이브 {m_currentWaveIndex + 1}]: 보스가 출현했습니다! 일반 적 스폰이 일시 중단됩니다.");
    }

    /// <summary>
    /// [설명]: 보스 처치 신호를 수신하여 일반 적 스폰 로직을 재성성화합니다.
    /// </summary>
    private void HandleBossDefeated()
    {
        m_isBossSpawned = false;
        Debug.LogWarning("[보스 처치]: 보스가 파괴되었습니다. 중단되었던 웨이브 스폰을 재개합니다!");
    }

    /// <summary>
    /// [설명]: 매 프레임 적 소환 타이머 및 개체수를 관리합니다.
    /// </summary>
    private void HandleWaveLogic()
    {
        if (m_spawnAreaCollider == null) return;
        if (m_isBossSpawned) return;

        if (m_remainingEnemiesInWave > 0)
        {
            m_currentTimer -= Time.deltaTime;
            if (m_currentTimer <= 0f)
            {
                SpawnEnemy();
                m_currentTimer = m_currentSpawnInterval;
                m_remainingEnemiesInWave--;

                if (m_remainingEnemiesInWave <= 0)
                {
#if UNITY_EDITOR
                    Debug.Log($"[Wave {m_currentWaveIndex + 1}] 모든 일반 적 생성 완료. 남은 적 처치 대기 중...");
#endif
                }
            }
        }
    }

    /// <summary>
    /// [설명]: 적 개체 파괴 시 잔여 개체수를 점검하여 웨이브를 종료시킵니다.
    /// </summary>
    private void OnEnemyDestroyed()
    {
        m_activeEnemyCount--;

        if (m_remainingEnemiesInWave <= 0 && m_activeEnemyCount <= 0)
        {
            m_currentWaveIndex++;
            WaitAndStartNextWaveAsync().Forget();
        }
    }

    /// <summary>
    /// [설명]: 웨이브 사이의 휴식 시간을 대기한 후 다음 웨이브를 개시합니다.
    /// </summary>
    private async UniTaskVoid WaitAndStartNextWaveAsync()
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(m_waveConfig.RestDuration), cancellationToken: this.GetCancellationTokenOnDestroy());
            StartNextWave();
        }
        catch (OperationCanceledException)
        {
            // 객체 파괴 등으로 인한 취소 시 대응
        }
    }

    /// <summary>
    /// [설명]: 중첩 방지 로직을 거쳐 유효한 위치에 적을 소환합니다.
    /// </summary>
    private void SpawnEnemy()
    {
        if (m_spawnAreaCollider == null || m_waveConfig.EnemyPrefab == null) return;

        var pool = UnityEngine.Object.FindAnyObjectByType<ObjectPoolManager>();
        
        Vector3 spawnPos = Vector3.zero;
        bool foundPos = false;

        for (int i = 0; i < m_maxSpawnAttempts; i++)
        {
            Vector3 potentialPos = m_spawnLogic.CalculateRandomSpawnPositionInBounds(m_spawnAreaCollider.bounds);
            Collider2D hit = Physics2D.OverlapCircle(potentialPos, m_spawnOverlapRadius, 1 << gameObject.layer);
            
            if (hit == null)
            {
                spawnPos = potentialPos;
                foundPos = true;
                break;
            }
        }

        if (!foundPos) spawnPos = m_spawnLogic.CalculateRandomSpawnPositionInBounds(m_spawnAreaCollider.bounds);

        GameObject enemyObj = null;
        if (pool != null)
        {
            enemyObj = pool.GetFromPool(m_waveConfig.EnemyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            enemyObj = Instantiate(m_waveConfig.EnemyPrefab, spawnPos, Quaternion.identity);
        }

        if (enemyObj == null) return;

        if (enemyObj.TryGetComponent<EnemyController>(out var controller))
        {
            m_activeEnemyCount++;
            controller.SetPoolReleaseAction(obj => OnEnemyDestroyed());
        }
    }
}
