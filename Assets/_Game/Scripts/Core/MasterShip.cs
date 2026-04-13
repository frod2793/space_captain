using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;
using Cysharp.Threading.Tasks;

[Serializable]
public class MasterShipDTO
{
    public int MaxHp = 1000;
    public int CurrentHp = 1000;
    public bool IsDestroyed = false;
}

public class MasterShipLogic
{
    private MasterShipDTO m_data;

    public MasterShipLogic(MasterShipDTO data)
    {
        m_data = data;
    }

    public float OnDamaged(int damage)
    {
        if (m_data == null || m_data.IsDestroyed)
        {
            return 0f;
        }

        if (damage > 0)
        {
            m_data.CurrentHp = Mathf.Max(0, m_data.CurrentHp - damage);
        }

        if (m_data.CurrentHp <= 0)
        {
            m_data.IsDestroyed = true;
        }

        return (float)m_data.CurrentHp / m_data.MaxHp;
    }

    public bool CheckIsDestroyed() => m_data != null && m_data.IsDestroyed;
}

public class MasterShip : MonoBehaviour
{
    [Header("모선 설정")]
    [SerializeField] private MasterShipDTO m_shipData;
    [SerializeField] private Barrier m_barrier;

    [Header("연출 설정")]
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    
    [Header("스킬(유도 미사일) 설정")]
    [SerializeField] private int m_missileDamage = 50;
    [SerializeField] private int m_missileCount = 12;
    [SerializeField] private float m_missileFireInterval = 0.1f;
    [SerializeField] private GameObject m_missilePrefab;

    private MasterShipLogic m_logic;
    private ObjectPoolManager m_poolManager;

    public event Action OnMasterShipDestroyed;
    public event Action<float> OnHpChanged;

    private void Awake()
    {
        Initialize();
        m_poolManager = FindAnyObjectByType<ObjectPoolManager>();
    }

    private void Initialize()
    {
        if (m_shipData == null)
        {
            m_shipData = new MasterShipDTO();
        }

        m_logic = new MasterShipLogic(m_shipData);
    }

    public void TakeDamage(int damageAmount)
    {
        if (m_logic == null || m_logic.CheckIsDestroyed())
        {
            return;
        }

        if (m_barrier != null)
        {
            damageAmount = m_barrier.ResolveDamage(damageAmount);
        }

        if (damageAmount <= 0)
        {
            return;
        }

        float hpRatio = m_logic.OnDamaged(damageAmount);

        OnHpChanged?.Invoke(hpRatio);
        PlayDamageEffect();

        if (m_logic.CheckIsDestroyed())
        {
            HandleDestruction();
        }
    }

    private void PlayDamageEffect()
    {
        if (m_spriteRenderer != null)
        {
            m_spriteRenderer.DOKill();
            m_spriteRenderer.color = Color.white;
            m_spriteRenderer.DOColor(Color.red, 0.05f).SetLoops(2, LoopType.Yoyo);
        }
    }

    private void HandleDestruction()
    {
        OnMasterShipDestroyed?.Invoke();
        gameObject.SetActive(false);
    }


    public void ExecuteSkill(int index)
    {
        switch (index)
        {
            case 0:
                ExecuteGuidedMissile(index);
                break;
            default:
                break;
        }
    }

    public async void ExecuteGuidedMissile(int index)
    {
        if (index != 0 || m_missilePrefab == null)
        {
            return;
        }

        var potentialTargets = new List<IAttackTarget>();
        potentialTargets.AddRange(FindObjectsByType<EnemyController>(FindObjectsSortMode.None));
        potentialTargets.AddRange(FindObjectsByType<BossController>(FindObjectsSortMode.None));

        List<IAttackTarget> activeTargets = new List<IAttackTarget>();
        for (int i = 0; i < potentialTargets.Count; i++)
        {
            if (potentialTargets[i] != null && potentialTargets[i].IsActiveTarget)
            {
                activeTargets.Add(potentialTargets[i]);
            }
        }

        for (int i = 0; i < m_missileCount; i++)
        {
            GameObject missileObj = null;
            if (m_poolManager != null)
            {
                missileObj = m_poolManager.GetFromPool(m_missilePrefab, transform.position, Quaternion.identity);
            }
            else
            {
                missileObj = Instantiate(m_missilePrefab, transform.position, Quaternion.identity);
            }

            if (missileObj != null)
            {
                HomingMissile missile = missileObj.GetComponent<HomingMissile>();
                if (missile != null)
                {
                    missile.InitializeMissile(new MissileParams { Target = GetTarget(activeTargets, i), Damage = m_missileDamage });
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(m_missileFireInterval), cancellationToken: this.GetCancellationTokenOnDestroy());
        }
    }

    private IAttackTarget GetTarget(List<IAttackTarget> targets, int index)
    {
        if (targets == null || targets.Count == 0)
        {
            return null;
        }
        return targets[index % targets.Count];
    }

    public class MissileParams
    {
        public IAttackTarget Target;
        public int Damage;
    }
}
