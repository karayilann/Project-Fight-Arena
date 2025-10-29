using Unity.Netcode;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Debug = Utilities.Debug;

public class Armor : NetworkBehaviour, IDamageable
{
    public PoolObjectType type;
    
    [Header("Armor Settings")]
    [SerializeField] private float maxHealth = 40f;
    [SerializeField] private float lifetime = 10f;
    
    private NetworkVariable<float> health = new NetworkVariable<float>(40f);
    private float _spawnTime;
    private bool _isDestroying;

    public float Health
    {
        get { return health.Value; }
        set { health.Value = value; }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            health.Value = maxHealth;
            _spawnTime = Time.time;
            _isDestroying = false;
            
            StartAutoDestroyTimer().Forget();

            Debug.Log($"Armor spawned! Lifetime: {lifetime}s, Health: {health.Value}");
        }
    }

    private async UniTaskVoid StartAutoDestroyTimer()
    {
        if (!IsServer) return;
        
        await UniTask.Delay(System.TimeSpan.FromSeconds(lifetime), cancellationToken: this.GetCancellationTokenOnDestroy());
        
        if (!_isDestroying && this != null && gameObject != null)
        {
            Debug.Log($"Armor lifetime expired ({lifetime}s). Auto-destroying...");
            DespawnArmor();
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;
        
        health.Value -= damage;
        Debug.Log($"Armor TakeDamage called! Damage: {damage}, Remaining Health: {health.Value}, Time Alive: {Time.time - _spawnTime:F1}s");
        
        if (health.Value <= 0f)
        {
            Debug.Log("Armor destroyed by damage!");
            DespawnArmor();
        }
    }

    private void DespawnArmor()
    {
        if (!IsServer || _isDestroying) return;
        
        _isDestroying = true;
        Debug.Log($"Armor despawning! Final Health: {health.Value}, Time Alive: {Time.time - _spawnTime:F1}s");
        
        var networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn();
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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _isDestroying = false;
    }
}