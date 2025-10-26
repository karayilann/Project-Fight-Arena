using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Character
{
    public class PlayerCollect : MonoBehaviour
    {
        private void OnCollisionEnter(Collision other)
        {
            if (!Player.Instance) return;

            if (other.gameObject.TryGetComponent<NetworkObject>(out var netObj) &&
                other.gameObject.TryGetComponent<Collectable>(out var collectable))
            {
                Player.Instance.RequestPickupServerRpc(netObj.NetworkObjectId);
            }
        }
    }
}
