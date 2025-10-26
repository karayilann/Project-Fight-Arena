using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Character
{
    public partial class Player
    {
        [Header("Combat")]
        private AGun _currentGun = null;
        public Transform gunHolder;
        public List<AGun> guns;
        private List<AGun> _spawnedGuns = new List<AGun>();
        
        void InitCombat()
        {
            if (guns == null || guns.Count == 0 || guns[0] == null)
            {
                Debug.LogError("No gun prefab assigned in Player guns list!");
                return;
            }
            
            EquipGun();
        }

        private void EquipGun(AGun aGun = null)
        {
            var spawnedGun = Instantiate(guns[0], gunHolder);
            
            spawnedGun.transform.localPosition = Vector3.zero;
            spawnedGun.transform.localRotation = Quaternion.identity;
            
            _currentGun = spawnedGun;
            _spawnedGuns.Add(spawnedGun);
            
            Debug.Log("Gun Equipped: " + _currentGun.name);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void SpawnProjectileServerRpc(Vector3 spawnPos, Vector3 targetPos)
        {
            NetworkObject projectileNetObj = NetworkObjectPool.Instance.Spawn(
                PoolObjectType.Projectile,
                spawnPos,
                Quaternion.identity
            );
            
            if (projectileNetObj == null)
            {
                Debug.LogError("Failed to spawn projectile from pool!");
                return;
            }
            
            if (projectileNetObj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Initialize(spawnPos, targetPos);
                Debug.Log("Projectile spawned from pool and initialized!");
            }
            else
            {
                Debug.LogError("Spawned object doesn't have Projectile component!");
                NetworkObjectPool.Instance.Despawn(projectileNetObj);
            }
        }
        
        
        #region Combat Input Handlers
        
        private void HandleFire()
        {
            _currentGun?.Fire();
            Debug.Log("HandleFire called.");
        }

        private void HandleExtraFire()
        {
            _currentGun?.ExtraFire();
        }
        
        #endregion
    }
}