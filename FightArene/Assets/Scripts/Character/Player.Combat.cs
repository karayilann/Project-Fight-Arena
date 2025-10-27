using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Character
{
    public partial class Player
    {
        [Header("Combat")]
        public Transform gunHolder;
        public Transform magnetHolder;
        public Transform armorHolder;
        
        public List<AGun> guns;
        
        private AGun _currentGun = null;
        private List<AGun> _spawnedGuns = new List<AGun>();
        private NetworkObject _currentArmor = null;
        private NetworkObject _currentMagnet = null;
        
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
        
        
        [ServerRpc(RequireOwnership = false)]
        private void SpawnArmorServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
            {
                if (networkClient.PlayerObject != null && networkClient.PlayerObject.TryGetComponent<Player>(out var targetPlayer))
                {
                    if (targetPlayer._currentArmor != null)
                    {
                        NetworkObjectPool.Instance.Despawn(targetPlayer._currentArmor);
                        targetPlayer._currentArmor = null;
                    }

                    NetworkObject armorNetObj = NetworkObjectPool.Instance.Spawn(
                        PoolObjectType.Armor,
                        targetPlayer.armorHolder.position,
                        targetPlayer.armorHolder.rotation
                    );

                    if (armorNetObj == null)
                    {
                        Debug.LogError("Failed to spawn armor from pool!");
                        return;
                    }

                    if (armorNetObj.TrySetParent(targetPlayer.transform))
                    {
                        targetPlayer._currentArmor = armorNetObj;
                        targetPlayer.UpdateArmorPositionClientRpc(armorNetObj.NetworkObjectId);
                        
                        targetPlayer.hasArmor.Value = false;
                        
                        Debug.Log("Armor spawned and parented successfully!");
                    }
                    else
                    {
                        Debug.LogError("Failed to parent armor!");
                        NetworkObjectPool.Instance.Despawn(armorNetObj);
                    }
                }
            }
        }

        [ClientRpc]
        private void UpdateArmorPositionClientRpc(ulong armorNetworkId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(armorNetworkId, out var armorNetObj))
            {
                armorNetObj.transform.localPosition = armorHolder.localPosition;
                armorNetObj.transform.localRotation = armorHolder.localRotation;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnMagnetServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
            {
                if (networkClient.PlayerObject != null && networkClient.PlayerObject.TryGetComponent<Player>(out var targetPlayer))
                {
                    if (targetPlayer._currentMagnet != null)
                    {
                        NetworkObjectPool.Instance.Despawn(targetPlayer._currentMagnet);
                        targetPlayer._currentMagnet = null;
                    }

                    NetworkObject magnetNetObj = NetworkObjectPool.Instance.Spawn(
                        PoolObjectType.Magnet,
                        targetPlayer.magnetHolder.position,
                        targetPlayer.magnetHolder.rotation
                    );

                    if (magnetNetObj == null)
                    {
                        Debug.LogError("Failed to spawn magnet from pool!");
                        return;
                    }

                    if (magnetNetObj.TrySetParent(targetPlayer.transform))
                    {
                        targetPlayer._currentMagnet = magnetNetObj;
                        targetPlayer.UpdateMagnetPositionClientRpc(magnetNetObj.NetworkObjectId);
                        
                        targetPlayer.hasMagnet.Value = false;
                        
                        Debug.Log("Magnet spawned and parented successfully!");
                    }
                    else
                    {
                        Debug.LogError("Failed to parent magnet!");
                        NetworkObjectPool.Instance.Despawn(magnetNetObj);
                    }
                }
            }
        }

        [ClientRpc]
        private void UpdateMagnetPositionClientRpc(ulong magnetNetworkId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(magnetNetworkId, out var magnetNetObj))
            {
                magnetNetObj.transform.localPosition = magnetHolder.localPosition;
                magnetNetObj.transform.localRotation = magnetHolder.localRotation;
            }
        }
        
        
        #region Combat Input Handlers
        
        private void HandleFire()
        {
            _currentGun?.Fire();
            Debug.Log("HandleFire called.");
        }

        
        // Add Using Feedback UI for skills
        private void HandleExtraFire()
        {
            if (_selectedSkillIndex == 0 && isAvailableArmor)
            {
                Debug.Log("Using Armor Skill");
                SpawnArmorServerRpc();
                ResetSkillsAsync(0, _coolDown).Forget();
            }
            else if (_selectedSkillIndex == 1 && isAvailableMagnet)
            {
                Debug.Log("Using Magnet Skill");
                SpawnMagnetServerRpc();
                ResetSkillsAsync(1, _coolDown).Forget();
            }
        }

        #endregion
    }
}