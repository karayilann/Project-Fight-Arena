using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Network
{
    /// <summary>
    /// NetworkManager'ı izleyen ve otomatik cleanup yapan yardımcı sınıf
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkManagerHelper : MonoBehaviour
    {
        [Header("Network Ayarları")]
        [SerializeField] private bool autoShutdownOnDestroy = true;
        [SerializeField] private bool autoShutdownOnSceneChange = true;

        private NetworkManager _networkManager;

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton != _networkManager)
            {
                Debug.LogWarning("Birden fazla NetworkManager bulundu! Bu obje yok ediliyor.");
                Destroy(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            if (autoShutdownOnDestroy)
            {
                CleanupNetwork();
            }
        }

        private void OnApplicationQuit()
        {
            CleanupNetwork();
        }

        private void OnDisable()
        {
            if (autoShutdownOnSceneChange)
            {
                CleanupNetwork();
            }
        }

        /// <summary>
        /// Network bağlantısını temizler
        /// </summary>
        public void CleanupNetwork()
        {
            if (_networkManager != null && _networkManager.IsListening)
            {
                Debug.Log("NetworkManager temizleniyor...");
                
                try
                {
                    _networkManager.Shutdown();
                    Debug.Log("NetworkManager başarıyla kapatıldı.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"NetworkManager kapatılırken hata: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Network durumunu kontrol eder
        /// </summary>
        public void CheckNetworkStatus()
        {
            if (_networkManager == null)
            {
                Debug.LogWarning("NetworkManager bulunamadı!");
                return;
            }

            Debug.Log($"Network Durumu: IsListening={_networkManager.IsListening}, " +
                     $"IsServer={_networkManager.IsServer}, " +
                     $"IsClient={_networkManager.IsClient}, " +
                     $"IsHost={_networkManager.IsHost}");
        }
    }
}

