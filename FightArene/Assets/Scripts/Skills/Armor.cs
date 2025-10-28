using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

public class Armor : NetworkBehaviour, IDamageable
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
        Debug.Log($"Armor TakeDamage called! Damage: {damage}, Remaining Health: {health.Value}");
        
        if (health.Value <= 0f)
        {
            DespawnArmor();
        }
    }

    private void DespawnArmor()
    {
        if (IsServer)
        {
            Debug.Log("Armor destroyed!");
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!IsServer) return;
        
        if (other.gameObject.CompareTag("Projectile"))
        {
            Debug.Log("Armor: OnCollisionEnter triggered with Projectile");
        }
    }

}