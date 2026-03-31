using UnityEngine;
using UnityEngine.Pool;
using System;

#region 인터페이스
/// <summary>
/// [설명]: 오브젝트 풀링이 가능한 객체를 위한 인터페이스입니다.
/// </summary>
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}
#endregion

#region 풀러 구현
/// <summary>
/// [설명]: 적 오브젝트의 생성과 재사용을 관리하는 풀링 클래스입니다.
/// </summary>
public class EnemyPool
{
    private IObjectPool<GameObject> m_pool;
    private GameObject m_prefab;
    private Transform m_root;

    public EnemyPool(GameObject prefab, int defaultCapacity = 10, int maxCapacity = 50, Transform root = null)
    {
        m_prefab = prefab;
        m_root = root;
        m_pool = new ObjectPool<GameObject>(
            OnCreatePoolItem,
            OnGetFromPool,
            OnReleaseToPool,
            OnDestroyPoolItem,
            true,
            defaultCapacity,
            maxCapacity
        );
    }

    private GameObject OnCreatePoolItem()
    {
        var instance = UnityEngine.Object.Instantiate(m_prefab, m_root);
        return instance;
    }

    private void OnGetFromPool(GameObject obj)
    {
        obj.SetActive(true);
        if (obj.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnSpawn();
        }
    }

    private void OnReleaseToPool(GameObject obj)
    {
        if (obj.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnDespawn();
        }
        obj.SetActive(false);
    }

    private void OnDestroyPoolItem(GameObject obj)
    {
        UnityEngine.Object.Destroy(obj);
    }

    public GameObject Get() => m_pool.Get();
    public void Release(GameObject obj) => m_pool.Release(obj);
}
#endregion
