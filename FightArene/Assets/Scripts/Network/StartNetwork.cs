using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = Utilities.Debug;

namespace Network
{
    public class StartNetwork : MonoBehaviour
    {
        public GameObject networkCanvas;
        private void OnDestroy()
        {
            ShutdownNetwork();
        }

        private void OnApplicationQuit()
        {
            ShutdownNetwork();
        }

        public void StartHost()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                Debug.Log("Network zaten aktif, kapatılıyor...");
                ShutdownNetwork();
            }

            Debug.Log("Starting Host...");
            
            try
            {
                NetworkManager.Singleton.StartHost();
                if (NetworkManager.Singleton.IsListening)
                {
                    networkCanvas.SetActive(false);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Host başlatılamadı: {e.Message}");
            }
        }
        
        public void StartClient()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                Debug.Log("Network zaten aktif, kapatılıyor...");
                ShutdownNetwork();
            }

            Debug.Log("Starting Client...");
            
            try
            {
                NetworkManager.Singleton.StartClient();
                if (NetworkManager.Singleton.IsListening)
                {
                    networkCanvas.SetActive(false);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Client başlatılamadı: {e.Message}");
            }
        }

        public void StopNetwork()
        {
            Debug.Log("Stopping Network...");
            ShutdownNetwork();
        }

        private void ShutdownNetwork()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("Network kapatıldı.");
            }
        }
        
        public void PrintConnectedClients()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                Debug.Log("Network aktif değil!");
                return;
            }

            Debug.Log($"Connected Clients: {NetworkManager.Singleton.ConnectedClientsList.Count}");
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                Debug.Log($"ClientId: {client.ClientId}, PlayerObject: {client.PlayerObject}");
            }
        }
    }
}
