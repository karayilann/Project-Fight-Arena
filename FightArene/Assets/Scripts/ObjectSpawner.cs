using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using Debug = Utilities.Debug;

public class ObjectSpawner : NetworkBehaviour
{
    [Header("Spawn Configuration")] [SerializeField]
    private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    [Header("Spawn Settings")] [SerializeField]
    private float spawnInterval = 2f;

    [SerializeField] private int maxObjectsPerSpawn = 1;
    [SerializeField] private float spawnHeight = 10f;
    [SerializeField] private bool autoStart = true;

    [Header("Gizmos")] 
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showLabels = true;

    private bool _isSpawning = false;
    private CancellationTokenSource _cancellationTokenSource;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && autoStart)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Spawning sadece server'da başlatılabilir!");
            return;
        }

        if (_isSpawning)
        {
            Debug.LogWarning("Spawning zaten aktif!");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("Spawn point bulunamadı!");
            return;
        }

        _isSpawning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        SpawnRoutineAsync(_cancellationTokenSource.Token).Forget();
        
        Debug.Log($"Spawning başlatıldı - {spawnPoints.Count} spawn point aktif");
    }

    public void StopSpawning()
    {
        if (!IsServer) return;

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        _isSpawning = false;
        Debug.Log("Spawning durduruldu");
    }



    private async UniTaskVoid SpawnRoutineAsync(CancellationToken token)
    {
        try
        {
            while (_isSpawning && !token.IsCancellationRequested)
            {
                SpawnObjects();
                await UniTask.Delay((int)(spawnInterval * 1000), cancellationToken: token);
            }
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    private void SpawnObjects()
    {
        if (!IsServer) return;

        for (int i = 0; i < maxObjectsPerSpawn; i++)
        {
            SpawnPoint point = GetRandomActiveSpawnPoint();
            
            if (point != null && point.spawnableObjects.Count > 0)
            {
                SpawnableObject objToSpawn = point.continuousSpawn ? SelectObjectForContinuous(point) : SelectObjectByChance(point);
                
                if (objToSpawn != null)
                {
                    SpawnObjectAtPoint(point, objToSpawn);
                }
            }
        }
    }
    
    private SpawnableObject SelectObjectForContinuous(SpawnPoint point)
    {
        if (point.spawnableObjects.Count == 0)
            return null;

        float totalChance = 0f;
        foreach (var obj in point.spawnableObjects)
        {
            totalChance += obj.spawnChance;
        }

        if (totalChance <= 0f)
        {
            return point.spawnableObjects[0];
        }

        float randomValue = Random.Range(0f, totalChance);
        float currentSum = 0f;

        foreach (var obj in point.spawnableObjects)
        {
            currentSum += obj.spawnChance;
            if (randomValue <= currentSum)
            {
                return obj;
            }
        }

        return point.spawnableObjects[0];
    }

    private SpawnPoint GetRandomActiveSpawnPoint()
    {
        List<SpawnPoint> validPoints = new List<SpawnPoint>();

        foreach (var point in spawnPoints)
        {
            bool shouldAdd = point.continuousSpawn || Random.value <= point.pointActiveChance;
            
            if (shouldAdd && point.spawnableObjects.Count > 0)
            {
                validPoints.Add(point);
            }
        }

        if (validPoints.Count == 0)
        {
            var pointsWithObjects = spawnPoints.Where(p => p.spawnableObjects.Count > 0).ToList();
            return pointsWithObjects.Count > 0 ? pointsWithObjects[Random.Range(0, pointsWithObjects.Count)] : null;
        }

        return validPoints[Random.Range(0, validPoints.Count)];
    }

    private SpawnableObject SelectObjectByChance(SpawnPoint point)
    {
        float totalChance = 0f;
        foreach (var obj in point.spawnableObjects)
        {
            totalChance += obj.spawnChance;
        }

        if (totalChance <= 0f)
        {
            Debug.LogWarning($"Spawn point '{point}' için toplam chance 0!");
            return null;
        }

        float randomValue = Random.Range(0f, totalChance);
        float currentSum = 0f;

        foreach (var obj in point.spawnableObjects)
        {
            currentSum += obj.spawnChance;
            if (randomValue <= currentSum)
            {
                return obj;
            }
        }

        return point.spawnableObjects[0];
    }

    private void SpawnObjectAtPoint(SpawnPoint point, SpawnableObject spawnableObj)
    {
        Vector3 spawnPos = GetRandomPositionInCircle(point);
        Quaternion rotation = spawnableObj.randomRotation ? Random.rotation : Quaternion.identity;

        NetworkObject obj = NetworkObjectPool.Instance.Spawn(
            spawnableObj.objectType,
            spawnPos,
            rotation
        );

        if (obj == null)
        {
            Debug.LogWarning($"Obje spawn edilemedi: {spawnableObj.objectType} at {point}");
        }
    }

    private Vector3 GetRandomPositionInCircle(SpawnPoint point)
    {
        if (point.transform == null)
        {
            Debug.LogError("Spawn point transform null!");
            return transform.position;
        }

        Vector2 randomCircle = Random.insideUnitCircle * point.radius;

        Vector3 worldPos = point.transform.position;
        worldPos.x += randomCircle.x;
        worldPos.y += spawnHeight;
        worldPos.z += randomCircle.y;

        return worldPos;
    }

    #region Utils

    public void SpawnFromPointIndex(int pointIndex)
    {
        if (!IsServer)
        {
            Debug.LogWarning("SpawnFromPointIndex sadece server'da çağrılabilir!");
            return;
        }

        if (pointIndex < 0 || pointIndex >= spawnPoints.Count)
        {
            Debug.LogError($"Geçersiz spawn point index: {pointIndex}");
            return;
        }

        SpawnPoint point = spawnPoints[pointIndex];

        if (point.transform == null)
        {
            Debug.LogError($"Spawn point {pointIndex} transform null!");
            return;
        }

        if (point.spawnableObjects.Count == 0)
        {
            Debug.LogWarning($"Spawn point {pointIndex} için spawn edilecek obje yok!");
            return;
        }

        SpawnableObject objToSpawn = SelectObjectByChance(point);
        if (objToSpawn != null)
        {
            SpawnObjectAtPoint(point, objToSpawn);
        }
    }

    public void SpawnSpecificObject(int pointIndex, PoolObjectType objectType)
    {
        if (!IsServer) return;

        if (pointIndex < 0 || pointIndex >= spawnPoints.Count)
        {
            Debug.LogError($"Geçersiz spawn point index: {pointIndex}");
            return;
        }

        SpawnPoint point = spawnPoints[pointIndex];

        if (point.transform == null)
        {
            Debug.LogError($"Spawn point {pointIndex} transform null!");
            return;
        }

        SpawnableObject spawnableObj = point.spawnableObjects.Find(o => o.objectType == objectType);
        if (spawnableObj == null)
        {
            Debug.LogError($"'{objectType}' objesi spawn point {pointIndex}'de bulunamadı!");
            return;
        }

        SpawnObjectAtPoint(point, spawnableObj);
    }

    #endregion
    
    public override void OnNetworkDespawn()
    {
        StopSpawning();
        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }


    [System.Serializable]
    public class SpawnableObject
    {
        public PoolObjectType objectType;
        [Range(0f, 100f)] public float spawnChance = 50f;
        public bool randomRotation = false;
    }

    [System.Serializable]
    public class SpawnPoint
    {
        public Transform transform;
        public float radius = 5f;
        public List<SpawnableObject> spawnableObjects = new List<SpawnableObject>();
        [Range(0f, 1f)] public float pointActiveChance = 1f;
        
        [Header("Continuous Spawn")]
        [Tooltip("Aktif olduğunda bu spawn point için chance kontrollerini atlar ve garantili spawn yapar")]
        public bool continuousSpawn = false;
    }
}