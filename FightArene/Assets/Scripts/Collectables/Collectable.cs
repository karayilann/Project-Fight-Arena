using UnityEngine;
using Unity.Netcode;
using Debug = Utilities.Debug;

public class Collectable : PooledNetworkObject
{
    public CollectableType type;

    public override void OnSpawnFromPool()
    {
        gameObject.SetActive(true);
    }

    public override void OnReturnToPool()
    {
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            Debug.Log("Collectable hit the ground.");
            if (IsServer)
            {
                ReturnToPool();
            }
            else
            {
                NotifyGroundHitServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyGroundHitServerRpc(ServerRpcParams rpcParams = default)
    {
        if (IsServer)
        {
            ReturnToPool();
        }
    }
}