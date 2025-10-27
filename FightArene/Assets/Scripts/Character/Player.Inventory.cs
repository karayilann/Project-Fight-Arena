using Unity.Netcode;
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

        [ServerRpc(RequireOwnership = true)]
        public void RequestPickupServerRpc(ulong collectableNetId, ServerRpcParams rpcParams = default)
        {
            if (NetworkManager.Singleton == null) return;

            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(collectableNetId, out var netObj)) return;
            var collectable = netObj.GetComponent<Collectable>();
            if (collectable == null) return;

            if (collectable.type == PoolObjectType.Armor)
            {
                hasArmor.Value = true;
            }
            else if (collectable.type == PoolObjectType.Magnet)
            {
                hasMagnet.Value = true;
            }
            else
            {
                collectableCount.Value += 1;
                Debug.Log($"Picked {collectable.type} x{1}. Total: {collectableCount.Value}");
            }

            netObj.Despawn(true);
        }

        private void OnDestroy()
        {
            collectableCount.OnValueChanged -= OnCollectableCountChanged;
        }
    }
}