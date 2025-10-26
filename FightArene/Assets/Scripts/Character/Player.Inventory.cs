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

            collectableCount.Value += 1;
            Debug.Log($"Picked {collectable.type} x{1}. Total: {collectableCount.Value}");

            netObj.Despawn(true);
        }

        private void OnDestroy()
        {
            collectableCount.OnValueChanged -= OnCollectableCountChanged;
        }
    }
}