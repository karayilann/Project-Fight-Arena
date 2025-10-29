using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Network
{
    /// <summary>
    /// Oyuncuları belirli spawn noktalarında oluşturur ve farklı prefab'lar kullanır
    /// İlk oyuncu (host) playerPrefab, diğerleri clientPrefab alır
    /// ÖNEMLI: Bu prefab'lar NetworkManager'ın Network Prefabs listesine MANUEL olarak eklenmelidir!
    /// </summary>
    public class NetworkPlayerSpawner : NetworkBehaviour
    {
        [Header("Prefab Ayarları")]
        [Tooltip("İlk bağlanan oyuncu için prefab (host) - NetworkManager'a da ekleyin!")]
        public GameObject playerPrefab;
        
        [Tooltip("Diğer oyuncular için prefab (client) - NetworkManager'a da ekleyin!")]
        public GameObject clientPrefab;
        
        [Header("Spawn Noktaları")]
        public Transform[] spawnPoints;
        
        [Header("Oyun Başlatma")]
        public int minPlayers = 2;
        public GameObject networkCanvas;
        
        private readonly Dictionary<ulong, int> clientSpawnIndices = new Dictionary<ulong, int>();
        private readonly HashSet<ulong> spawnedClients = new HashSet<ulong>(); // Çift spawn kontrolü
        private int nextSpawnIndex;
        private bool gameStarted;

        private void Start()
        {
            ValidateReferences();
            
            // Sadece server Time.timeScale'i kontrol eder
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                Time.timeScale = 0f;
                Debug.Log("NetworkPlayerSpawner: Network henüz başlamadı, oyun durduruldu.");
            }
            else if (NetworkManager.Singleton.IsServer)
            {
                Time.timeScale = 0f;
                Debug.Log("NetworkPlayerSpawner: Server - Oyun durduruldu, oyuncular bekleniyor...");
            }
            else
            {
                Debug.Log("NetworkPlayerSpawner: Client - Oyun durumu server tarafından kontrol ediliyor.");
            }
        }

        private void ValidateReferences()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("NetworkPlayerSpawner: Spawn noktaları atanmamış!");
            }
            if (playerPrefab == null)
            {
                Debug.LogError("NetworkPlayerSpawner: Player Prefab atanmamış!");
            }
            else
            {
                var netObj = playerPrefab.GetComponent<NetworkObject>();
                if (netObj == null)
                {
                    Debug.LogError($"NetworkPlayerSpawner: {playerPrefab.name} prefab'ında NetworkObject component yok!");
                }
            }
            
            if (clientPrefab == null)
            {
                Debug.LogError("NetworkPlayerSpawner: Client Prefab atanmamış!");
            }
            else
            {
                var netObj = clientPrefab.GetComponent<NetworkObject>();
                if (netObj == null)
                {
                    Debug.LogError($"NetworkPlayerSpawner: {clientPrefab.name} prefab'ında NetworkObject component yok!");
                }
            }
            
            if (networkCanvas == null)
            {
                Debug.LogError("NetworkPlayerSpawner: Network Canvas atanmamış!");
            }
            
            // NetworkManager kontrolü
            if (NetworkManager.Singleton != null)
            {
                bool playerRegistered = false;
                bool clientRegistered = false;
                
                foreach (var prefab in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
                {
                    if (prefab.Prefab == playerPrefab) playerRegistered = true;
                    if (prefab.Prefab == clientPrefab) clientRegistered = true;
                }
                
                if (!playerRegistered)
                {
                    Debug.LogError($"NetworkPlayerSpawner: {playerPrefab?.name} NetworkManager'ın Network Prefabs listesine EKLENMEMİŞ! Inspector'dan ekleyin!");
                }
                if (!clientRegistered)
                {
                    Debug.LogError($"NetworkPlayerSpawner: {clientPrefab?.name} NetworkManager'ın Network Prefabs listesine EKLENMEMİŞ! Inspector'dan ekleyin!");
                }
                
                if (playerRegistered && clientRegistered)
                {
                    Debug.Log("NetworkPlayerSpawner: ✓ Tüm prefab'lar NetworkManager'a kayıtlı.");
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            
            Debug.Log("NetworkPlayerSpawner: Server aktif, event listener'lar eklendi.");
            
            // *** DÜZELTİLDİ: Manuel host spawn çağrısı KALDIRILDI ***
            // OnClientConnectedCallback zaten host için otomatik tetiklenir
            // Bu satırları yoruma alın veya silin:
            /*
            if (NetworkManager.IsHost)
            {
                Debug.Log("NetworkPlayerSpawner: Host spawn ediliyor...");
                OnClientConnected(NetworkManager.ServerClientId);
            }
            */
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void OnDisable()
        {
            // Sadece server cleanup yapar
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                Time.timeScale = 1f;
                Debug.Log("NetworkPlayerSpawner: Server deaktif, Time.timeScale normale döndürüldü.");
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            // *** DÜZELTİLDİ: Çift spawn kontrolü eklendi ***
            if (spawnedClients.Contains(clientId))
            {
                Debug.LogWarning($"NetworkPlayerSpawner: Client {clientId} zaten spawn edilmiş, atlanıyor.");
                return;
            }
            
            Debug.Log($"NetworkPlayerSpawner: Client {clientId} bağlandı. Toplam: {NetworkManager.ConnectedClientsList.Count}");
            
            // Spawn pozisyonu ayır
            int spawnIndex = AllocateSpawnIndex();
            clientSpawnIndices[clientId] = spawnIndex;
            
            // *** DÜZELTİLDİ: Host kontrolü için ServerClientId yerine 0 karşılaştırması ***
            // Host her zaman ClientId = 0'dır
            GameObject prefabToSpawn = (clientId == 0) ? playerPrefab : clientPrefab;
            
            Debug.Log($"NetworkPlayerSpawner: Client {clientId} için {prefabToSpawn.name} spawn edilecek.");
            
            // Spawned listesine ekle
            spawnedClients.Add(clientId);
            
            // Oyuncuyu spawn et
            StartCoroutine(SpawnPlayerWithDelay(clientId, spawnIndex, prefabToSpawn));
            
            // Oyun başlatma kontrolü
            CheckGameStart();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"NetworkPlayerSpawner: Client {clientId} ayrıldı.");
            
            if (clientSpawnIndices.ContainsKey(clientId))
            {
                clientSpawnIndices.Remove(clientId);
            }
            
            // Spawned listesinden çıkar
            spawnedClients.Remove(clientId);
            
            CheckGameStart();
        }

        private int AllocateSpawnIndex()
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return 0;
            
            int index = nextSpawnIndex % spawnPoints.Length;
            nextSpawnIndex++;
            
            Debug.Log($"NetworkPlayerSpawner: Spawn index {index} ayrıldı.");
            return index;
        }

        private IEnumerator SpawnPlayerWithDelay(ulong clientId, int spawnIndex, GameObject prefab)
        {
            // Hiç bekleme yapma - hemen spawn et
            if (!NetworkManager.ConnectedClients.ContainsKey(clientId))
            {
                Debug.LogWarning($"NetworkPlayerSpawner: Client {clientId} artık bağlı değil.");
                spawnedClients.Remove(clientId); // Listeden çıkar
                yield break;
            }
            
            Debug.Log($"NetworkPlayerSpawner: Client {clientId} için spawn başlatılıyor...");
            
            // Prefab'ın NetworkObject component'ini kontrol et
            NetworkObject prefabNetworkObject = prefab.GetComponent<NetworkObject>();
            if (prefabNetworkObject == null)
            {
                Debug.LogError($"NetworkPlayerSpawner: {prefab.name} prefab'ında NetworkObject component yok!");
                spawnedClients.Remove(clientId); // Listeden çıkar
                yield break;
            }
            
            // Spawn pozisyonu ve rotasyonu
            Vector3 spawnPos = GetSpawnPosition(spawnIndex);
            Quaternion spawnRot = GetSpawnRotation(spawnIndex);
            
            // Prefab'ı spawn et
            GameObject playerInstance = Instantiate(prefab, spawnPos, spawnRot);
            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
            
            if (networkObject == null)
            {
                Debug.LogError($"NetworkPlayerSpawner: Instance'da NetworkObject bulunamadı!");
                Destroy(playerInstance);
                spawnedClients.Remove(clientId); // Listeden çıkar
                yield break;
            }
            
            // Network'e spawn et ve ownership'i ata
            try
            {
                Debug.Log($"NetworkPlayerSpawner: SpawnAsPlayerObject çağrılıyor - ClientId: {clientId}");
                networkObject.SpawnAsPlayerObject(clientId, true);
                Debug.Log($"NetworkPlayerSpawner: ✓ Client {clientId} için {prefab.name} spawn edildi. Pozisyon: {spawnPos}");
                Debug.Log($"NetworkPlayerSpawner: Ownership - IsOwner: {networkObject.IsOwner}, OwnerClientId: {networkObject.OwnerClientId}");
                
                // Spawn sonrası kontrol
                new WaitForSecondsRealtime(0.1f);
                if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
                {
                    Debug.Log($"NetworkPlayerSpawner: ✓✓ Client {clientId} PlayerObject doğrulandı: {client.PlayerObject.name}");
                }
                else
                {
                    Debug.LogError($"NetworkPlayerSpawner: Client {clientId} PlayerObject atanamadı!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NetworkPlayerSpawner: Spawn hatası: {e.Message}\n{e.StackTrace}");
                Destroy(playerInstance);
                spawnedClients.Remove(clientId); // Hata durumunda listeden çıkar
            }
        }

        private Vector3 GetSpawnPosition(int index)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("NetworkPlayerSpawner: Spawn noktası yok, Vector3.zero kullanılıyor.");
                return Vector3.zero;
            }
            
            index = Mathf.Clamp(index, 0, spawnPoints.Length - 1);
            return spawnPoints[index].position;
        }

        private Quaternion GetSpawnRotation(int index)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return Quaternion.identity;
            }
            
            index = Mathf.Clamp(index, 0, spawnPoints.Length - 1);
            return spawnPoints[index].rotation;
        }

        private void CheckGameStart()
        {
            if (networkCanvas == null) return;
            
            int connectedCount = NetworkManager.ConnectedClientsList.Count;
            bool shouldStart = connectedCount >= minPlayers;
            
            if (shouldStart && !gameStarted)
            {
                gameStarted = true;
                SetGameStateClientRpc(true);
                Debug.Log("NetworkPlayerSpawner: Oyun başladı!");
            }
            else if (!shouldStart && gameStarted)
            {
                gameStarted = false;
                SetGameStateClientRpc(false);
                Debug.Log("NetworkPlayerSpawner: Yetersiz oyuncu, oyun duraklatıldı.");
            }
        }

        [ClientRpc]
        private void SetGameStateClientRpc(bool isPlaying)
        {
            if (networkCanvas != null)
            {
                networkCanvas.SetActive(!isPlaying);
            }
            
            Time.timeScale = isPlaying ? 1f : 0f;
            Debug.Log($"NetworkPlayerSpawner: Oyun durumu güncellendi. Oynuyor: {isPlaying}");
        }
    }
}