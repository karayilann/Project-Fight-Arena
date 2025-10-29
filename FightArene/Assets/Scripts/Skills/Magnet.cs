using Unity.Netcode;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Character;
using Debug = Utilities.Debug;

namespace Skills
{
    public class Magnet : NetworkBehaviour
    {
        public PoolObjectType type;
        
        [Header("Magnet Settings")]
        [SerializeField] private float lifetime = 10f;
        [SerializeField] private float pullRadius = 30f;
        [SerializeField] private float pullForce = 15f;
        [SerializeField] private float checkInterval = 0.1f;
        [SerializeField] private int maxColliders = 20;
        [SerializeField] private LayerMask collectableLayer;
        
        private float _spawnTime;
        private bool _isDestroying;
        private Player _ownerPlayer;
        private Collider[] _colliderBuffer;
        private NetworkVariable<ulong> _ownerPlayerNetworkId = new NetworkVariable<ulong>();

        public void SetOwnerPlayer(ulong playerNetworkId)
        {
            if (!IsServer) return;
            
            Debug.Log($"Magnet: SetOwnerPlayer called with ID: {playerNetworkId}");
            _ownerPlayerNetworkId.Value = playerNetworkId;
            
            // Eğer zaten spawn olduysa, owner'ı bul ve başlat
            if (IsSpawned && _ownerPlayer == null)
            {
                InitializeOwnerAndStart();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                _spawnTime = Time.time;
                _isDestroying = false;
                _colliderBuffer = new Collider[maxColliders];
                
                Debug.Log($"Magnet OnNetworkSpawn: _ownerPlayerNetworkId = {_ownerPlayerNetworkId.Value}");
                
                // Eğer owner ID set edilmişse, başlat
                if (_ownerPlayerNetworkId.Value != 0)
                {
                    InitializeOwnerAndStart();
                }
                else
                {
                    // Owner ID henüz set edilmemiş, bir frame bekle
                    WaitForOwnerAndStart().Forget();
                }
            }
        }

        private async UniTaskVoid WaitForOwnerAndStart()
        {
            if (!IsServer) return;
            
            // Owner ID'nin set edilmesini bekle (max 1 saniye)
            float waitTime = 0f;
            while (_ownerPlayerNetworkId.Value == 0 && waitTime < 1f)
            {
                await UniTask.Yield();
                waitTime += Time.deltaTime;
            }
            
            if (_ownerPlayerNetworkId.Value != 0)
            {
                InitializeOwnerAndStart();
            }
            else
            {
                Debug.LogError("Magnet: Owner player ID was not set within timeout!");
                DespawnMagnet();
            }
        }

        private void InitializeOwnerAndStart()
        {
            if (_ownerPlayer != null)
            {
                Debug.LogWarning("InitializeOwnerAndStart called but owner already set!");
                return;
            }
            
            Debug.Log($"[MAGNET] InitializeOwnerAndStart - Starting initialization...");
            
            if (_ownerPlayerNetworkId.Value != 0)
            {
                Debug.Log($"[MAGNET] Searching for player with NetworkObjectId: {_ownerPlayerNetworkId.Value}");
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_ownerPlayerNetworkId.Value, out var playerNetObj))
                {
                    _ownerPlayer = playerNetObj.GetComponent<Player>();
                    Debug.Log($"[MAGNET] Found player via NetworkObjectId: {(_ownerPlayer != null ? _ownerPlayer.gameObject.name : "NULL")}");
                }
                else
                {
                    Debug.LogError($"[MAGNET] Could not find NetworkObject with ID: {_ownerPlayerNetworkId.Value}");
                }
            }
            else
            {
                Debug.LogWarning("[MAGNET] _ownerPlayerNetworkId is 0, skipping NetworkObjectId search");
            }
            
            if (_ownerPlayer == null)
            {
                Debug.Log("[MAGNET] Trying to find player via GetComponentInParent...");
                _ownerPlayer = GetComponentInParent<Player>();
                Debug.Log($"[MAGNET] GetComponentInParent result: {(_ownerPlayer != null ? _ownerPlayer.gameObject.name : "NULL")}");
            }
            
            if (_ownerPlayer == null)
            {
                Debug.LogError("[MAGNET] FAILED - Owner player not found! NetworkId was: " + _ownerPlayerNetworkId.Value);
                DespawnMagnet();
                return;
            }
            
            Debug.Log($"[MAGNET] SUCCESS - Found owner player: {_ownerPlayer.gameObject.name} (NetworkId: {_ownerPlayer.NetworkObjectId})");
            Debug.Log($"[MAGNET] Owner collectableTypes count: {_ownerPlayer.collectableTypes.Count}");
            
            StartAutoDestroyTimer().Forget();
            StartPullingCollectables().Forget();

            Debug.Log($"[MAGNET] Spawned! Lifetime: {lifetime}s, Radius: {pullRadius}, Force: {pullForce}, Layer: {collectableLayer.value}");
        }

        private async UniTaskVoid StartAutoDestroyTimer()
        {
            if (!IsServer) return;
            
            await UniTask.Delay(System.TimeSpan.FromSeconds(lifetime), cancellationToken: this.GetCancellationTokenOnDestroy());
            
            if (!_isDestroying && this != null && gameObject != null)
            {
                Debug.Log($"Magnet lifetime expired ({lifetime}s). Auto-destroying...");
                DespawnMagnet();
            }
        }

        private async UniTaskVoid StartPullingCollectables()
        {
            if (!IsServer) return;

            Debug.Log("[MAGNET] StartPullingCollectables - Loop started!");
            int loopCount = 0;

            while (!_isDestroying && this != null && gameObject != null)
            {
                try
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(checkInterval), cancellationToken: this.GetCancellationTokenOnDestroy());
                    
                    if (_isDestroying || _ownerPlayer == null) break;
                    
                    loopCount++;
                    if (loopCount % 10 == 0)
                    {
                        Debug.Log($"[MAGNET] Pull loop iteration: {loopCount}");
                    }
                    
                    PullNearbyCollectables();
                }
                catch (System.OperationCanceledException)
                {
                    Debug.Log("[MAGNET] Pull loop cancelled");
                    break;
                }
            }
            
            Debug.Log($"[MAGNET] Pull loop ended. Total iterations: {loopCount}");
        }

        private void PullNearbyCollectables()
        {
            if (!IsServer || _ownerPlayer == null) return;

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, pullRadius, _colliderBuffer, collectableLayer);
            
            if (hitCount > 0)
            {
                Debug.Log($"[MAGNET PULL] Found {hitCount} objects in radius at position {transform.position}");
            }
            
            int pulledCount = 0;
            int skippedByType = 0;
            int skippedNoRb = 0;
            
            for (int i = 0; i < hitCount; i++)
            {
                var hitCollider = _colliderBuffer[i];
                
                Debug.Log($"[MAGNET PULL] Checking object {i}: {hitCollider.gameObject.name} on layer {hitCollider.gameObject.layer}");
                
                if (hitCollider.TryGetComponent<Collectable>(out var collectable))
                {
                    Debug.Log($"[MAGNET PULL] Object has Collectable component, type: {collectable.type}");
                    Debug.Log($"[MAGNET PULL] Owner collectableTypes: [{string.Join(", ", _ownerPlayer.collectableTypes)}]");
                    
                    if (!_ownerPlayer.collectableTypes.Contains(collectable.type))
                    {
                        Debug.Log($"[MAGNET PULL] Skipping - type {collectable.type} not in owner's collectableTypes");
                        skippedByType++;
                        continue;
                    }
                    
                    Debug.Log($"[MAGNET PULL] Type check passed! Looking for Rigidbody and NetworkObject...");
                    
                    if (hitCollider.TryGetComponent<Rigidbody>(out var rb) && hitCollider.TryGetComponent<NetworkObject>(out var netObj))
                    {
                        Vector3 directionToPlayer = (_ownerPlayer.transform.position - hitCollider.transform.position).normalized;
                        float distance = Vector3.Distance(hitCollider.transform.position, _ownerPlayer.transform.position);
                        
                        float distanceMultiplier = Mathf.Clamp01(1f - (distance / pullRadius));
                        Vector3 pullVector = directionToPlayer * (pullForce * distanceMultiplier * Time.fixedDeltaTime);
                        
                        Debug.Log($"[MAGNET PULL] PULLING {hitCollider.gameObject.name}! Distance: {distance:F2}, Force: {pullVector.magnitude:F2}");
                        
                        rb.AddForce(pullVector, ForceMode.VelocityChange);
                        ApplyPullForceClientRpc(netObj.NetworkObjectId, pullVector);
                        
                        pulledCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"[MAGNET PULL] Object has Collectable but missing Rigidbody or NetworkObject! HasRB: {hitCollider.TryGetComponent<Rigidbody>(out _)}, HasNetObj: {hitCollider.TryGetComponent<NetworkObject>(out _)}");
                        skippedNoRb++;
                    }
                }
                else
                {
                    Debug.Log($"[MAGNET PULL] Object {hitCollider.gameObject.name} has no Collectable component");
                }
            }
            
            if (hitCount > 0)
            {
                Debug.Log($"[MAGNET PULL] Summary - Total found: {hitCount}, Pulled: {pulledCount}, Skipped by type: {skippedByType}, Skipped no RB/NetObj: {skippedNoRb}");
            }
        }

        [ClientRpc]
        private void ApplyPullForceClientRpc(ulong collectableNetworkId, Vector3 force)
        {
            if (IsServer) return; 
            
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(collectableNetworkId, out var netObj))
            {
                if (netObj.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.AddForce(force, ForceMode.VelocityChange);
                }
            }
        }

        private void DespawnMagnet()
        {
            if (!IsServer || _isDestroying) return;
            
            _isDestroying = true;
            Debug.Log($"Magnet despawning! Time Alive: {Time.time - _spawnTime:F1}s");
            
            var networkObject = GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isDestroying = false;
            _ownerPlayer = null;
            _colliderBuffer = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, pullRadius);
        }
    }
}