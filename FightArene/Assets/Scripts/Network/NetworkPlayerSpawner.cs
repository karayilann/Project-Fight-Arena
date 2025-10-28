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
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (isSubscribed) return;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            isSubscribed = true;
            Debug.Log("NetworkPlayerSpawner: Event callbacks registered.");
        }

        private void UnsubscribeFromEvents()
        {
            if (!isSubscribed || NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            isSubscribed = false;
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"NetworkPlayerSpawner: Client {clientId} connected. IsServer: {NetworkManager.Singleton.IsServer}");
            
            if (!NetworkManager.Singleton.IsServer) return;
            
            int index = AllocateIndex();
            clientSpawnIndices[clientId] = index;
            Debug.Log($"Allocated spawn index {index} for client {clientId}");
            
            StartCoroutine(AssignWhenReady(clientId, index));
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (clientSpawnIndices.TryGetValue(clientId, out var idx))
            {
                clientSpawnIndices.Remove(clientId);
                if (idx < nextIndex) nextIndex = idx;
            }
        }

        private int AllocateIndex()
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return 0;
            int idx = nextIndex % spawnPoints.Length;
            nextIndex++;
            return idx;
        }

        private IEnumerator AssignWhenReady(ulong clientId, int index)
        {
            Debug.Log($"AssignWhenReady started for client {clientId}, index {index}");
            
            var tries = 0;
            while (tries < 50)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
                    client.PlayerObject != null)
                {
                    var playerObj = client.PlayerObject;
                    var pos = GetSpawnPosition(index);
                    var rot = GetSpawnRotation(index);
                    
                    Debug.Log($"Moving client {clientId} from {playerObj.transform.position} to {pos}");
                    
                    // Server tarafında pozisyonu ayarla
                    playerObj.transform.position = pos;
                    playerObj.transform.rotation = rot;
                    
                    // CharacterController varsa deaktif et, pozisyon ayarla, tekrar aktif et
                    var characterController = playerObj.GetComponent<CharacterController>();
                    if (characterController != null)
                    {
                        characterController.enabled = false;
                        playerObj.transform.position = pos;
                        playerObj.transform.rotation = rot;
                        characterController.enabled = true;
                        Debug.Log($"CharacterController found and repositioned for client {clientId}");
                    }
                    
                    Debug.Log($"Client {clientId} assigned to spawn index {index} at {pos}");
                    yield break;
                }

                tries++;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            Debug.LogError($"PlayerObject for client {clientId} not found within timeout (5 seconds).");
        }

        private Vector3 GetSpawnPosition(int index)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                index = Mathf.Clamp(index, 0, spawnPoints.Length - 1);
                var pos = spawnPoints[index].position;
                Debug.Log($"GetSpawnPosition: index {index} -> position {pos}");
                return pos;
            }

            Debug.LogWarning("GetSpawnPosition: No spawn points available, returning Vector3.zero");
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
