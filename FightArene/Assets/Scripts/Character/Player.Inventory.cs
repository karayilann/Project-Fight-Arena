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
            Debug.Log("Collectable Count Updated: " + current);
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!IsOwner) return;

            if (other.gameObject.TryGetComponent<NetworkObject>(out var netObj) &&
                other.gameObject.TryGetComponent<Collectable>(out var collectable))
            {
                RequestPickupServerRpc(netObj.NetworkObjectId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestPickupServerRpc(ulong collectableNetId, ServerRpcParams rpcParams = default)
        {
            if (NetworkManager.Singleton == null) return;

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(collectableNetId, out var netObj))
            {
                var collectable = netObj.GetComponent<Collectable>();
                if (collectable == null) return;

                collectableCount.Value += 1;
                Debug.Log($"Picked {collectable.type} x{1}. Total: {collectableCount.Value}");

                netObj.Despawn(true);
            }
        }

        private void OnDestroy()
        {
            collectableCount.OnValueChanged -= OnCollectableCountChanged;
        }
    }
}