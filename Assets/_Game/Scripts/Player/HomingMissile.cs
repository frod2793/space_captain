using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public class HomingMissile : MonoBehaviour, IPoolable
{
    [SerializeField] private float m_speed = 10f;
    [SerializeField] private float m_rotateSpeed = 200f;
    [SerializeField] private float m_maxLifeTime = 5f;
    [SerializeField] private float m_waveAmplitude = 2f;
    [SerializeField] private float m_waveFrequency = 5f;
    [SerializeField] private GameObject m_explosionEffect;
    [SerializeField] private float m_explosionLifetime = 2f;
    [SerializeField] private float m_visualRotationOffset = -90f;

    private IAttackTarget m_target;
    private int m_damage;
    private Vector2 m_scatterDirection;
    private Vector2 m_currentDirection;
    private bool m_isInitialized = false;
    private float m_aliveTime = 0f;
    private ObjectPoolManager m_poolManager;
    private CancellationTokenSource m_lifeTimeCts;

    private void Awake()
    {
        m_poolManager = FindAnyObjectByType<ObjectPoolManager>();
    }

    public void OnSpawn()
    {
        m_aliveTime = 0f;
        m_isInitialized = false;
        m_target = null;
        m_scatterDirection = Vector2.zero;
        m_currentDirection = Vector2.up;
        
        m_lifeTimeCts?.Cancel();
        m_lifeTimeCts?.Dispose();
        m_lifeTimeCts = new CancellationTokenSource();
    }

    public void OnDespawn()
    {
        m_lifeTimeCts?.Cancel();
        m_lifeTimeCts?.Dispose();
        m_lifeTimeCts = null;
    }

    public void InitializeMissile(MasterShip.MissileParams missileParams)
    {
        m_target = missileParams.Target;
        m_damage = missileParams.Damage;
        m_currentDirection = Vector2.up;
        
        if (m_target == null)
        {
            float angle = Random.Range(0f, 360f);
            m_scatterDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }

        m_isInitialized = true;
        StartLifeTimeTimer().Forget();
    }

    private async UniTaskVoid StartLifeTimeTimer()
    {
        try
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(m_maxLifeTime), cancellationToken: m_lifeTimeCts.Token);
            ReturnToPool();
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    private void Update()
    {
        if (!m_isInitialized)
        {
            return;
        }

        m_aliveTime += Time.deltaTime;

        Vector2 moveStep = Vector2.zero;

        if (m_target != null && m_target.IsActiveTarget)
        {
            Vector2 targetDir = (Vector2)m_target.TargetTransform.position - (Vector2)transform.position;
            targetDir.Normalize();

            float rotateAmount = Vector3.Cross(targetDir, m_currentDirection).z;
            float angleChange = -rotateAmount * m_rotateSpeed * Time.deltaTime;
            
            float currentAngle = Mathf.Atan2(m_currentDirection.y, m_currentDirection.x) * Mathf.Rad2Deg;
            float nextAngle = currentAngle + angleChange;
            m_currentDirection = new Vector2(Mathf.Cos(nextAngle * Mathf.Deg2Rad), Mathf.Sin(nextAngle * Mathf.Deg2Rad));

            Vector2 sideDir = new Vector2(-m_currentDirection.y, m_currentDirection.x);
            float waveOffset = Mathf.Cos(m_aliveTime * m_waveFrequency) * m_waveAmplitude;
            
            moveStep = (m_currentDirection * m_speed + sideDir * waveOffset) * Time.deltaTime;
        }
        else
        {
            if (m_scatterDirection == Vector2.zero)
            {
                float angle = Random.Range(0f, 360f);
                m_scatterDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            }
            m_currentDirection = Vector2.Lerp(m_currentDirection, m_scatterDirection, Time.deltaTime * m_rotateSpeed * 0.01f);
            moveStep = m_currentDirection * m_speed * Time.deltaTime;
        }

        transform.position += (Vector3)moveStep;

        float finalRotationAngle = Mathf.Atan2(m_currentDirection.y, m_currentDirection.x) * Mathf.Rad2Deg + m_visualRotationOffset;
        transform.rotation = Quaternion.Euler(0, 0, finalRotationAngle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") || collision.CompareTag("Boss"))
        {
            IAttackTarget target = collision.GetComponent<IAttackTarget>();
            if (target != null)
            {
                target.TakeDamage(m_damage);
                Explode();
            }
        }
    }

    private void Explode()
    {
        if (m_explosionEffect != null)
        {
            if (m_poolManager != null)
            {
                GameObject effect = m_poolManager.GetFromPool(m_explosionEffect, transform.position, Quaternion.identity);
                ReturnExplosionToPool(effect).Forget();
            }
            else
            {
                GameObject effect = Instantiate(m_explosionEffect, transform.position, Quaternion.identity);
                Destroy(effect, m_explosionLifetime);
            }
        }
        ReturnToPool();
    }

    private async UniTaskVoid ReturnExplosionToPool(GameObject effect)
    {
        try
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(m_explosionLifetime), cancellationToken: this.GetCancellationTokenOnDestroy());
            if (effect != null && m_poolManager != null)
            {
                m_poolManager.ReturnToPool(effect);
            }
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    private void ReturnToPool()
    {
        if (m_poolManager != null)
        {
            m_poolManager.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
