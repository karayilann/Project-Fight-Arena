using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Network
{
    public class GameStartController : MonoBehaviour
    {
        public int minPlayers = 2;
        public GameObject networkCanvas;
        private bool gameStarted;
        private bool isSubscribed = false;

        private void Start()
        {
            if (networkCanvas == null)
            {
                Debug.LogError("GameStartController: networkCanvas atanmamış!");
            }
            
            // Başlangıçta oyunu durdur
            Time.timeScale = 0f;
            Debug.Log("GameStartController: Time.timeScale set to 0. Waiting for players...");
        }

        private void Update()
        {
            // NetworkManager hazır olduğunda event'lere subscribe ol
            if (!isSubscribed && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                SubscribeToEvents();
            }
            
            // Bağlı oyuncu sayısını kontrol et ve canvas'ı buna göre aç/kapat
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && networkCanvas != null)
            {
                int connectedCount = NetworkManager.Singleton.ConnectedClientsList.Count;
                
                if (connectedCount >= minPlayers && !gameStarted)
                {
                    gameStarted = true;
                    Time.timeScale = 1f;
                    networkCanvas.SetActive(false);
                    Debug.Log("GameStartController: Game started! Time.timeScale set to 1, canvas deactivated.");
                }
                else if (connectedCount < minPlayers && gameStarted)
                {
                    gameStarted = false;
                    Time.timeScale = 0f;
                    networkCanvas.SetActive(true);
                    Debug.Log("GameStartController: Not enough players. Time.timeScale set to 0, canvas activated.");
                }
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            
            // Script deaktif olduğunda timeScale'i normale döndür
            Time.timeScale = 1f;
        }

        private void SubscribeToEvents()
        {
            if (isSubscribed) return;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            isSubscribed = true;
            Debug.Log("GameStartController: Event callbacks registered.");
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
            Debug.Log($"GameStartController: Client {clientId} connected. Connected count: {NetworkManager.Singleton.ConnectedClientsList.Count}");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"GameStartController: Client {clientId} disconnected. Connected count: {NetworkManager.Singleton.ConnectedClientsList.Count}");
        }
    }
}
