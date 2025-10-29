using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Network
{
    /// <summary>
    /// Network başlatma ve durdurma işlemlerini yönetir
    /// UI butonlarından çağrılır
    /// </summary>
    public class StartNetwork : MonoBehaviour
    {
        [Header("UI Referansları (Opsiyonel)")]
        [Tooltip("Oyun başladığında kapatılacak canvas")]
        public GameObject networkCanvas;

        private void asda()
        {
            ShutdownNetwork();
        }

        private void OnApplicationQuit()
        {
            ShutdownNetwork();
        }

        /// <summary>
        /// Host olarak oyunu başlatır (UI butonundan çağrılır)
        /// </summary>
        public void StartHost()
        {
            if (IsNetworkActive())
            {
                Debug.Log("StartNetwork: Network zaten aktif, yeniden başlatılıyor...");
                ShutdownNetwork();
            }

            Debug.Log("StartNetwork: Host başlatılıyor...");

            try
            {
                // KRITIK: Otomatik player spawn'ı KAPAT
                NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
                
                // Player Prefab'ı temizle
                if (NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null)
                {
                    Debug.LogWarning("StartNetwork: NetworkManager'da PlayerPrefab bulundu, temizleniyor...");
                    NetworkManager.Singleton.NetworkConfig.PlayerPrefab = null;
                }
                
                NetworkManager.Singleton.StartHost();
                Debug.Log("StartNetwork: Host başarıyla başlatıldı.");
                
                // Canvas'ı kapat (eğer atanmışsa)
                if (networkCanvas != null)
                {
                    networkCanvas.SetActive(false);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"StartNetwork: Host başlatılamadı: {e.Message}");
            }
        }

        /// <summary>
        /// Client olarak oyuna bağlanır (UI butonundan çağrılır)
        /// </summary>
        public void StartClient()
        {
            if (IsNetworkActive())
            {
                Debug.Log("StartNetwork: Network zaten aktif, yeniden başlatılıyor...");
                ShutdownNetwork();
            }

            Debug.Log("StartNetwork: Client başlatılıyor...");

            try
            {
                // Player Prefab'ı temizle
                if (NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null)
                {
                    Debug.LogWarning("StartNetwork: NetworkManager'da PlayerPrefab bulundu, temizleniyor...");
                    NetworkManager.Singleton.NetworkConfig.PlayerPrefab = null;
                }
                
                NetworkManager.Singleton.StartClient();
                Debug.Log("StartNetwork: Client başarıyla başlatıldı.");
                
                // Canvas'ı kapat (eğer atanmışsa)
                if (networkCanvas != null)
                {
                    networkCanvas.SetActive(false);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"StartNetwork: Client başlatılamadı: {e.Message}");
            }
        }

        /// <summary>
        /// Connection approval - otomatik player spawn'ı engeller
        /// </summary>
        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = false; // KRITIK: Otomatik spawn KAPALI
            response.PlayerPrefabHash = null;    // Hash gönderme
            
            Debug.Log($"StartNetwork: Client {request.ClientNetworkId} onaylandı. Otomatik spawn devre dışı.");
        }

        public void StopNetwork()
        {
            Debug.Log("StartNetwork: Network durduruluyor...");
            ShutdownNetwork();
        }

        private void ShutdownNetwork()
        {
            if (IsNetworkActive())
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("StartNetwork: Network kapatıldı.");
            }
        }

        private bool IsNetworkActive()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        }

        public void PrintConnectedClients()
        {
            if (!IsNetworkActive())
            {
                Debug.Log("StartNetwork: Network aktif değil!");
                return;
            }

            Debug.Log($"StartNetwork: Bağlı oyuncu sayısı: {NetworkManager.Singleton.ConnectedClientsList.Count}");
            
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                string playerInfo = client.PlayerObject != null ? client.PlayerObject.name : "Player object yok";
                Debug.Log($"  - ClientId: {client.ClientId}, Player: {playerInfo}");
            }
        }
    }
}