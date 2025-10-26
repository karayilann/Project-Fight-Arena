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
                SpawnableObject objToSpawn = SelectObjectByChance(point);
                if (objToSpawn != null)
                {
                    SpawnObjectAtPoint(point, objToSpawn);
                }
            }
        }
    }

    private SpawnPoint GetRandomActiveSpawnPoint()
    {
        List<SpawnPoint> validPoints = new List<SpawnPoint>();

        foreach (var point in spawnPoints)
        {
            if (Random.value <= point.pointActiveChance && point.spawnableObjects.Count > 0)
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

    #region Editor Gizmos

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            var point = spawnPoints[i];
            if (point.transform == null) continue;

            Vector3 worldPos = point.transform.position;

            Color pointColor = GetColorForIndex(i);
            Gizmos.color = pointColor;

            DrawCircle(worldPos, point.radius);

            Gizmos.color = pointColor * 0.5f;
            Gizmos.DrawLine(worldPos, worldPos + Vector3.up * spawnHeight);

            Gizmos.color = pointColor;
            DrawCircle(worldPos + Vector3.up * spawnHeight, point.radius * 0.3f);

#if UNITY_EDITOR
            if (showLabels)
            {
                Handles.Label(
                    worldPos + Vector3.up * (spawnHeight + 1f),
                    $"Point {i}\n{point.spawnableObjects.Count} obje"
                );
            }
#endif
        }
    }

    private void DrawCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    private Color GetColorForIndex(int index)
    {
        Color[] colors = new Color[]
        {
            Color.green,
            Color.cyan,
            Color.yellow,
            Color.magenta,
            new Color(1f, 0.5f, 0f),
            new Color(0.5f, 0f, 1f),
        };

        return colors[index % colors.Length];
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            var point = spawnPoints[i];
            if (point.transform == null) continue;

            Vector3 worldPos = point.transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(worldPos + Vector3.up * spawnHeight, 0.5f);

#if UNITY_EDITOR
            if (showLabels && point.spawnableObjects.Count > 0)
            {
                string objList = $"=== Point {i} ===\n";
                foreach (var obj in point.spawnableObjects)
                {
                    objList += $"• {obj.objectType} ({obj.spawnChance}%)\n";
                }

                Handles.Label(
                    worldPos + Vector3.up * (spawnHeight + 2f),
                    objList
                );
            }
#endif
        }
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
    }
}