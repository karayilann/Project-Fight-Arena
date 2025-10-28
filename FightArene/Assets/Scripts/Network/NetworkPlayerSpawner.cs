using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Network
{
    public class NetworkPlayerSpawner : MonoBehaviour
    {
        public Transform[] spawnPoints;
        private readonly Dictionary<ulong, int> clientSpawnIndices = new Dictionary<ulong, int>();
        private int nextIndex;
        private bool isSubscribed = false;

        private void Start()
        {
            // Spawn noktalarını kontrol et
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("NetworkPlayerSpawner: spawnPoints boş! Inspector'dan atayın!");
            }
            else
            {
                Debug.Log($"NetworkPlayerSpawner: {spawnPoints.Length} spawn noktası bulundu.");
            }
        }

        private void Update()
        {
            // NetworkManager hazır olduğunda event'lere subscribe ol
            if (!isSubscribed && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                SubscribeToEvents();
                
                // Server ise, spawn override'ı ayarla
                if (NetworkManager.Singleton.IsServer)
                {
                    SetupSpawnOverride();
                }
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SetupSpawnOverride()
        {
            // NetworkManager'a custom spawn pozisyonu belirle
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                // PlayerPrefab'ın spawn pozisyonunu override et
                NetworkManager.Singleton.OnServerStarted += () =>
                {
                    Debug.Log("NetworkPlayerSpawner: Server started, spawn override active.");
                };
            }
        }

        private void SubscribeToEvents()
        {
            if (isSubscribed) return;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            // Server spawn handler'ı ekle
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
                
                // Mevcut bağlı oyuncuları kontrol et ve spawn pozisyonlarını ayarla
                Debug.Log($"NetworkPlayerSpawner: Checking already connected clients. Count: {NetworkManager.Singleton.ConnectedClientsList.Count}");
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (!clientSpawnIndices.ContainsKey(client.ClientId))
                    {
                        Debug.Log($"NetworkPlayerSpawner: Found already connected client {client.ClientId}, assigning spawn position.");
                        OnClientConnected(client.ClientId);
                    }
                }
            }
            
            isSubscribed = true;
            Debug.Log("NetworkPlayerSpawner: Event callbacks registered.");
        }

        private void UnsubscribeFromEvents()
        {
            if (!isSubscribed || NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            }
            
            isSubscribed = false;
        }

        private void OnServerStarted()
        {
            Debug.Log("NetworkPlayerSpawner: Server started, ready to assign spawn positions.");
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"NetworkPlayerSpawner: Client {clientId} connected. IsServer: {NetworkManager.Singleton.IsServer}");
            
            if (!NetworkManager.Singleton.IsServer) return;
            
            int index = AllocateIndex();
            clientSpawnIndices[clientId] = index;
            Debug.Log($"NetworkPlayerSpawner: Allocated spawn index {index} for client {clientId}");
            
            StartCoroutine(AssignWhenReady(clientId, index));
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (clientSpawnIndices.TryGetValue(clientId, out var idx))
            {
                clientSpawnIndices.Remove(clientId);
                Debug.Log($"NetworkPlayerSpawner: Client {clientId} disconnected, freed spawn index {idx}");
            }
        }

        private int AllocateIndex()
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return 0;
            int idx = nextIndex % spawnPoints.Length;
            nextIndex++;
            Debug.Log($"NetworkPlayerSpawner: Next spawn index will be {nextIndex}, assigned {idx}");
            return idx;
        }

        private IEnumerator AssignWhenReady(ulong clientId, int index)
        {
            Debug.Log($"NetworkPlayerSpawner: AssignWhenReady started for client {clientId}, index {index}");
            
            yield return new WaitForSecondsRealtime(0.2f); // Kısa bir gecikme ekle
            
            var tries = 0;
            while (tries < 50)
            {
                if (NetworkManager.Singleton != null && 
                    NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
                    client.PlayerObject != null)
                {
                    var playerObj = client.PlayerObject;
                    var pos = GetSpawnPosition(index);
                    var rot = GetSpawnRotation(index);
                    
                    Debug.Log($"NetworkPlayerSpawner: Moving client {clientId} from {playerObj.transform.position} to {pos}");
                    
                    // PlayerSpawnSync bileşenini al
                    var spawnSync = playerObj.GetComponent<PlayerSpawnSync>();
                    
                    if (spawnSync != null)
                    {
                        // ClientRpc ile tüm clientlara pozisyonu gönder
                        spawnSync.TeleportClientRpc(pos, rot);
                        Debug.Log($"NetworkPlayerSpawner: Called TeleportClientRpc for client {clientId}");
                    }
                    else
                    {
                        Debug.LogWarning($"NetworkPlayerSpawner: PlayerSpawnSync component not found on client {clientId}! Add it to Player prefab.");
                        
                        // Fallback: Manuel pozisyon ayarla
                        var characterController = playerObj.GetComponent<CharacterController>();
                        var rb = playerObj.GetComponent<Rigidbody>();
                        
                        if (characterController != null)
                        {
                            characterController.enabled = false;
                        }
                        
                        if (rb != null)
                        {
                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }
                        
                        playerObj.transform.position = pos;
                        playerObj.transform.rotation = rot;
                        
                        yield return null;
                        
                        playerObj.transform.position = pos;
                        playerObj.transform.rotation = rot;
                        
                        if (characterController != null)
                        {
                            characterController.enabled = true;
                        }
                    }
                    
                    Debug.Log($"NetworkPlayerSpawner: ✓ Client {clientId} successfully assigned to spawn index {index} at position {pos}");
                    yield break;
                }

                tries++;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            Debug.LogError($"NetworkPlayerSpawner: PlayerObject for client {clientId} not found within timeout (5 seconds).");
        }

        private Vector3 GetSpawnPosition(int index)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                index = Mathf.Clamp(index, 0, spawnPoints.Length - 1);
                var pos = spawnPoints[index].position;
                Debug.Log($"NetworkPlayerSpawner: GetSpawnPosition -> index {index} = position {pos}");
                return pos;
            }

            Debug.LogWarning("NetworkPlayerSpawner: No spawn points available, returning Vector3.zero");
            return Vector3.zero;
        }

        private Quaternion GetSpawnRotation(int index)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                index = Mathf.Clamp(index, 0, spawnPoints.Length - 1);
                return spawnPoints[index].rotation;
            }

            return Quaternion.identity;
        }
    }
}
