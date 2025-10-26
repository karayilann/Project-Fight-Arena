using UnityEngine;

public class Collectable : PooledNetworkObject
{
    public CollectableType type;
    public int amount = 1;
    public Rigidbody rb;
    public Collider col;

    public override void OnSpawnFromPool()
    {
        gameObject.SetActive(true);
        // if (rb != null)
        // {
        //     rb.linearVelocity = Vector3.zero;
        //     rb.angularVelocity = Vector3.zero;
        //     rb.isKinematic = false;
        // }

        //if (col != null) col.enabled = true;
    }

    public override void OnReturnToPool()
    {
        // if (rb != null)
        // {
        //     rb.linearVelocity = Vector3.zero;
        //     rb.angularVelocity = Vector3.zero;
        //     rb.isKinematic = true;
        // }

        //if (col != null) col.enabled = false;

        gameObject.SetActive(false);
    }

    public void PickedUp()
    {
        if (IsServer)
        {
            ReturnToPool();
        }
    }
}