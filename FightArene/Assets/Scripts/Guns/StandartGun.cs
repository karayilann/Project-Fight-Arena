using Character;
using UnityEngine;
using Debug = Utilities.Debug;

public class StandartGun : AGun
{
    public AudioSource gunSource;
    public AudioClip fireSound;
    public Projectile projectilePrefab;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 50f;
    [SerializeField] private Transform firePoint;
    
    public override void Fire()
    {
        SpawnProjectile();
    }
    
    private void SpawnProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab is not assigned to StandartGun!");
            return;
        }
        
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        
        Vector3 targetPosition;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            targetPosition = hit.point;
        }
        else
        {
            targetPosition = ray.origin + ray.direction * range;
        }
        
        SpawnProjectileServerRpc(spawnPosition, targetPosition);
        
        if (gunSource != null && fireSound != null)
        {
            gunSource.PlayOneShot(fireSound);
        }
        
        Debug.Log("Standart Gun Fired");
    }
    
    private void SpawnProjectileServerRpc(Vector3 spawnPos, Vector3 targetPos)
    {
        if (Player.Instance != null)
        {
            Player.Instance.SpawnProjectileServerRpc(spawnPos, targetPos, damage, range);
        }
    }

    public override void ExtraFire()
    {
        Debug.Log("Standart Gun Extra Fire Activated");
    }
}
