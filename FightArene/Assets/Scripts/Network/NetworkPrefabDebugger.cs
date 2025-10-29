// csharp
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Network
{
    public class NetworkPrefabDebugger : MonoBehaviour
    {
        private static FieldInfo s_globalObjectIdHashField;

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                Debug.Log("=== NETWORK PREFAB HASH DEBUG ===");
                Debug.Log($"Total registered prefabs: {NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs.Count}");

                foreach (var entry in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
                {
                    if (entry.Prefab != null)
                    {
                        var netObj = entry.Prefab.GetComponent<NetworkObject>();
                        if (netObj != null)
                        {
                            uint hash = GetGlobalObjectIdHash(netObj);
                            Debug.Log($"Prefab: {entry.Prefab.name} | Hash: {hash}");
                        }
                        else
                        {
                            Debug.LogWarning($"Prefab: {entry.Prefab.name} | NO NetworkObject!");
                        }
                    }
                }
                Debug.Log("=== END DEBUG ===");
            }
        }

        [ContextMenu("Print All Prefab Hashes")]
        public void PrintAllHashes()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager bulunamadı!");
                return;
            }

            Debug.Log("=== REGISTERED PREFABS ===");
            foreach (var entry in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
            {
                if (entry.Prefab != null)
                {
                    var netObj = entry.Prefab.GetComponent<NetworkObject>();
                    uint hash = netObj != null ? GetGlobalObjectIdHash(netObj) : 0;
                    string hashStr = netObj != null ? hash.ToString() : "NO NETWORK OBJECT";
                    Debug.Log($"{entry.Prefab.name}: Hash = {hashStr}");
                }
            }
        }

        private static uint GetGlobalObjectIdHash(NetworkObject netObj)
        {
            if (netObj == null) return 0;

            // Cache FieldInfo
            if (s_globalObjectIdHashField == null)
            {
                s_globalObjectIdHashField = typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }

            if (s_globalObjectIdHashField != null)
            {
                try
                {
                    var val = s_globalObjectIdHashField.GetValue(netObj);
                    if (val is uint u) return u;
                    if (val is int i) return (uint)i;
                    if (val is long l) return (uint)l;
                }
                catch
                {
                    // Güvenli hata yutma; reflection koşullarında sorun çıkarsa 0 döndür
                }
            }

            return 0;
        }
    }
}
