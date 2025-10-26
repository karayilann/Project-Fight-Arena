using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

public class NetworkObjectPool : NetworkSingleton<NetworkObjectPool>
{

    [Header("Pool Configurations")]
    [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();
    
    private Dictionary<PoolObjectType, GameObject> prefabDictionary = new Dictionary<PoolObjectType, GameObject>();
    private Dictionary<PoolObjectType, Queue<NetworkObject>> pools = new Dictionary<PoolObjectType, Queue<NetworkObject>>();
    private Dictionary<PoolObjectType, PoolConfig> configDictionary = new Dictionary<PoolObjectType, PoolConfig>();
    private Dictionary<NetworkObject, PoolObjectType> activeObjects = new Dictionary<NetworkObject, PoolObjectType>();
    
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            InitializePools();
        }
    }

    private void InitializePools()
    {
        foreach (var config in poolConfigs)
        {
            if (config.prefab == null)
            {
                Debug.LogError($"Prefab null: {config.type}");
                continue;
            }

            prefabDictionary[config.type] = config.prefab;
            configDictionary[config.type] = config;
            
            CreatePool(config.type, config.initialSize);
        }
        
        Debug.Log($"✓ {poolConfigs.Count} pool başarıyla oluşturuldu!");
    }
    
    private void CreatePool(PoolObjectType type, int initialSize)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Pool sadece server tarafında oluşturulabilir!");
            return;
        }

        if (pools.ContainsKey(type))
        {
            Debug.LogWarning($"Pool zaten mevcut: {type}");
            return;
        }

        if (!prefabDictionary.ContainsKey(type))
        {
            Debug.LogError($"Prefab bulunamadı: {type}");
            return;
        }

        GameObject prefab = prefabDictionary[type];
        NetworkObject networkObj = prefab.GetComponent<NetworkObject>();
        
        if (networkObj == null)
        {
            Debug.LogError($"Prefab'de NetworkObject component'i bulunamadı: {type}");
            return;
        }

        Queue<NetworkObject> pool = new Queue<NetworkObject>();
        pools[type] = pool;

        for (int i = 0; i < initialSize; i++)
        {
            NetworkObject obj = CreateNewObject(type);
            pool.Enqueue(obj);
        }

        Debug.Log($"✓ Pool: {type} - Başlangıç: {initialSize}");
    }


    public NetworkObject Spawn(PoolObjectType type, Vector3 position, Quaternion rotation)
    {
        if (!IsServer)
        {
            Debug.LogError("Spawn sadece server tarafından çağrılabilir!");
            return null;
        }

        if (!pools.ContainsKey(type))
        {
            Debug.LogError($"Pool bulunamadı: {type}. Inspector'dan pool config ekleyin!");
            return null;
        }

        NetworkObject obj;
        
        if (pools[type].Count > 0)
        {
            obj = pools[type].Dequeue();
        }
        else
        {
            if (configDictionary[type].autoExpand)
            {
                Debug.LogWarning($"Pool boş, yeni obje oluşturuluyor: {type}");
                obj = CreateNewObject(type);
            }
            else
            {
                Debug.LogError($"Pool boş ve genişleme devre dışı: {type}");
                return null;
            }
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.gameObject.SetActive(true);
        
        obj.Spawn(true);
        
        activeObjects[obj] = type;

        if (obj.TryGetComponent<PooledNetworkObject>(out var pooledObj))
        {
            pooledObj.OnSpawnFromPool();
        }

        return obj;
    }


    public NetworkObject Spawn(PoolObjectType type, Vector3 position)
    {
        return Spawn(type, position, Quaternion.identity);
    }
    
    public void Despawn(NetworkObject obj)
    {
        if (!IsServer)
        {
            Debug.LogError("Despawn sadece server tarafından çağrılabilir!");
            return;
        }

        if (!activeObjects.ContainsKey(obj))
        {
            Debug.LogWarning("Obje pool'a ait değil!");
            return;
        }

        PoolObjectType type = activeObjects[obj];
        activeObjects.Remove(obj);

        if (obj.TryGetComponent<PooledNetworkObject>(out var pooledObj))
        {
            pooledObj.OnReturnToPool();
        }

        obj.Despawn(false);
        obj.gameObject.SetActive(false);

        pools[type].Enqueue(obj);
    }


    private NetworkObject CreateNewObject(PoolObjectType type)
    {
        GameObject prefab = prefabDictionary[type];
        GameObject go = Instantiate(prefab, transform);
        go.name = $"{type}_{go.GetInstanceID()}";
        go.SetActive(false);
        NetworkObject netObj = go.GetComponent<NetworkObject>();
        return netObj;
    }


    public int GetPoolCount(PoolObjectType type)
    {
        if (pools.ContainsKey(type))
        {
            return pools[type].Count;
        }
        return 0;
    }


    public int GetActiveCount(PoolObjectType type)
    {
        int count = 0;
        foreach (var kvp in activeObjects)
        {
            if (kvp.Value == type) count++;
        }
        return count;
    }


    public void ClearPool(PoolObjectType type)
    {
        if (!IsServer) return;

        if (pools.ContainsKey(type))
        {
            while (pools[type].Count > 0)
            {
                NetworkObject obj = pools[type].Dequeue();
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
            }
            pools.Remove(type);
        }
    }
    
    public void ClearAllPools()
    {
        if (!IsServer) return;

        foreach (var type in new List<PoolObjectType>(pools.Keys))
        {
            ClearPool(type);
        }
    }

        
    [System.Serializable]
    public class PoolConfig
    {
        public PoolObjectType type;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 50;
        public bool autoExpand = true;
    }
}
