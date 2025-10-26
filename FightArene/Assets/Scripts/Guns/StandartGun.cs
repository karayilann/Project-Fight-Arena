using System;
using Character;
using UnityEngine;
using Debug = Utilities.Debug;

public class StandartGun : AGun
{
    public AudioSource gunSource;
    public AudioClip fireSound;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 50f;
    [SerializeField] private Transform firePoint;

    private Player _player;

    private void Awake()
    {
        _player = Player.Instance;
    }

    public override void Fire()
    {
        SpawnProjectile();
    }

    private void SpawnProjectile()
    {
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

        Vector3 targetPosition;
        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        var raycast = Physics.Raycast(ray, out RaycastHit hit, range);
        if (hit.collider != null && hit.collider.gameObject != _player.gameObject)
        {
            targetPosition = hit.point;
        }
        else if (hit.collider != null && hit.collider.gameObject != _player.gameObject)
        {
            Debug.Log("Hit self, ignoring.");
            targetPosition = Vector3.zero;
        }
        else
        {
            targetPosition = ray.origin + ray.direction * range;
        }

        if(targetPosition == Vector3.zero)
        {
            Debug.Log("No valid target found, aborting projectile spawn.");
            return;
        }
        
        SpawnProjectileServerRpc(spawnPosition, targetPosition);
       
        gunSource.PlayOneShot(fireSound);
    }

    private void SpawnProjectileServerRpc(Vector3 spawnPos, Vector3 targetPos)
    {
        
        if (Player.Instance != null)
        {
            Player.Instance.SpawnProjectileServerRpc(spawnPos, targetPos);
        }
    }

    public override void ExtraFire()
    {
        Debug.Log("Standart Gun Extra Fire Activated");
    }
}