using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;
public class StartNetwork : MonoBehaviour
{
    public void StartHost()
    {
        Debug.Log("Starting Host...");
        NetworkManager.Singleton.StartHost();
    }
    
    public void StartClient()
    {
        Debug.Log("Starting Client...");
        NetworkManager.Singleton.StartClient();
    }
    
    public void PrintConnectedClients()
    {
        Debug.Log($"Connected Clients: {NetworkManager.Singleton.ConnectedClientsList.Count}");
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            Debug.Log($"ClientId: {client.ClientId}, PlayerObject: {client.PlayerObject}");
        }
    }
    
}
