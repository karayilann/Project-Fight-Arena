using Unity.Netcode;

public abstract class PooledNetworkObject : NetworkBehaviour
{
    /// <summary>
    /// Obje pool'dan alındığında çağrılır
    /// </summary>
    public virtual void OnSpawnFromPool()
    {
    }

    /// <summary>
    /// Obje pool'a geri döndürüldüğünde çağrılır
    /// </summary>
    public virtual void OnReturnToPool()
    {
    }

    /// <summary>
    /// Objeyi pool'a geri döndür
    /// </summary>
    public void ReturnToPool()
    {
        if (IsServer)
        {
            NetworkObjectPool.Instance.Despawn(GetComponent<NetworkObject>());
        }
    }
}