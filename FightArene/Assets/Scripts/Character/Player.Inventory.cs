using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Character
{
    public partial class Player
    {
        NetworkVariable<int> collectableCount = new NetworkVariable<int>(0);

        void InitInventory()
        {
            collectableCount.OnValueChanged += OnCollectableCountChanged;
            OnCollectableCountChanged(0, collectableCount.Value);
        }

        private void OnCollectableCountChanged(int previous, int current)
        {
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.TryGetComponent<NetworkObject>(out var netObj) &&
                other.gameObject.TryGetComponent<Collectable>(out var collectable))
            {
                RequestPickupServerRpc(netObj.NetworkObjectId);
            }
        }
        
        
        [ServerRpc(RequireOwnership = false)]
        public void RequestPickupServerRpc(ulong collectableNetId, ServerRpcParams rpcParams = default)
        {
            if (NetworkManager.Singleton == null) return;

            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(collectableNetId, out var netObj)) return;
            var collectable = netObj.GetComponent<Collectable>();
            if (collectable == null) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
            {
                if (networkClient.PlayerObject != null && networkClient.PlayerObject.TryGetComponent<Player>(out var targetPlayer))
                {
                    if (collectable.type == PoolObjectType.Armor)
                    {
                        targetPlayer.hasArmor.Value = true;
                        Debug.Log("Client " + clientId + " picked Armor.");
                    }
                    else if (collectable.type == PoolObjectType.Magnet)
                    {
                        targetPlayer.hasMagnet.Value = true;
                        Debug.Log("Client " + clientId + " picked Magnet.");
                    }
                    else
                    {
                        targetPlayer.collectableCount.Value += 1;
                    }
                }
            }

            netObj.Despawn(true);
        }

        private void OnDestroy()
        {
            collectableCount.OnValueChanged -= OnCollectableCountChanged;
        }
    }
}