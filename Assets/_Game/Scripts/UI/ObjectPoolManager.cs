using UnityEngine;
using System.Collections.Generic;

public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}

public class ObjectPoolManager : MonoBehaviour
{
    private Dictionary<string, Queue<GameObject>> m_pools = new Dictionary<string, Queue<GameObject>>();

    public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name;
        if (!m_pools.ContainsKey(key))
        {
            m_pools[key] = new Queue<GameObject>();
        }

        if (m_pools[key].Count > 0)
        {
            GameObject obj = m_pools[key].Dequeue();
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);

                if (obj.TryGetComponent<IPoolable>(out var poolable))
                {
                    poolable.OnSpawn();
                }
                return obj;
            }
        }

        GameObject newObj = Instantiate(prefab, position, rotation);
        newObj.name = key;
        if (newObj.TryGetComponent<IPoolable>(out var newPoolable))
        {
            newPoolable.OnSpawn();
        }
        return newObj;
    }

    public void ReturnToPool(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (obj.TryGetComponent<IPoolable>(out var poolable))
        {
            poolable.OnDespawn();
        }

        string key = obj.name;
        if (!m_pools.ContainsKey(key))
        {
            m_pools[key] = new Queue<GameObject>();
        }

        obj.SetActive(false);
        m_pools[key].Enqueue(obj);
    }
}
