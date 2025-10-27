using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

public class Armor : NetworkBehaviour
{
    public PoolObjectType type;
    NetworkVariable<float> health = new NetworkVariable<float>(40f);

    public float Health
    {
        get { return health.Value; }
        set { health.Value = value; }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;
        
        health.Value -= damage;
        Debug.Log($"Health: {health.Value}");
        if (health.Value <= 0f)
        {
            DespawnArmor();
        }
    }

    private void DespawnArmor()
    {
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Projectile"))
        {
            TakeDamage(10f);
        }
    }

}