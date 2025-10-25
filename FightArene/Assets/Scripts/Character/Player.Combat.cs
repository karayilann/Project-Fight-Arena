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
        
        [Header("Projectile")]
        [SerializeField] private Projectile _projectilePrefab; // Projectile prefab referansı
        
        private List<AGun> _spawnedGuns = new List<AGun>();
        
        void InitCombat()
        {
            // Silah prefab kontrolü
            if (guns == null || guns.Count == 0 || guns[0] == null)
            {
                Debug.LogError("No gun prefab assigned in Player guns list!");
                return;
            }
            
            // Eğer gun prefab'ında NetworkObject varsa hata ver ve devam etme
            var prefabNetworkObj = guns[0].GetComponent<NetworkObject>();
            if (prefabNetworkObj != null)
            {
                Debug.LogError("ERROR: Gun prefab '" + guns[0].name + "' has NetworkObject component! " +
                    "Please open the prefab in Unity Editor and REMOVE the NetworkObject component. " +
                    "Only Projectile should have NetworkObject, NOT the gun itself!");
                return;
            }
            
            EquipGun();
        }

        private void EquipGun(AGun aGun = null)
        {
            var spawnedGun = Instantiate(guns[0], gunHolder);
            
            // Local pozisyon ve rotasyonu sıfırla
            spawnedGun.transform.localPosition = Vector3.zero;
            spawnedGun.transform.localRotation = Quaternion.identity;
            
            _currentGun = spawnedGun;
            _spawnedGuns.Add(spawnedGun);
            
            Debug.Log("Gun Equipped: " + _currentGun.name);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void SpawnProjectileServerRpc(Vector3 spawnPos, Vector3 targetPos, float damage, float range)
        {
            // Projectile prefab'ı burada al - her zaman aynı prefab kullanacağız
            // Silahtan prefab referansı almak yerine, Player'da tutacağız
            if (_projectilePrefab == null)
            {
                Debug.LogError("Projectile prefab is not assigned to Player!");
                return;
            }
            
            // Projectile'ı spawn et
            var bullet = Instantiate(_projectilePrefab, spawnPos, Quaternion.identity);
            var networkObject = bullet.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                networkObject.Spawn();
                // Initialize et
                bullet.Initialize(spawnPos, targetPos, damage, range);
            }
            else
            {
                Debug.LogError("Projectile prefab doesn't have NetworkObject component!");
                Destroy(bullet.gameObject);
            }
        }
        
        
        #region Combat Input Handlers
        
        private void HandleFire()
        {
            _currentGun?.Fire();
            
        }

        private void HandleExtraFire()
        {
            _currentGun?.ExtraFire();
            
        }

        
        #endregion
    }
}
