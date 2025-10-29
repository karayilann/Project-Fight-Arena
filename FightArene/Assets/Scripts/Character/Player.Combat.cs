using System.Collections.Generic;
using Skills;
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
        public void SpawnProjectileServerRpc(Vector3 spawnPos, Vector3 targetPos, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            NetworkObject projectileNetObj;
            if (clientId == 0)
            {
                projectileNetObj = NetworkObjectPool.Instance.Spawn(
                    PoolObjectType.Projectile,
                    spawnPos,
                    Quaternion.identity
                );
            }
            else
            {
                projectileNetObj = NetworkObjectPool.Instance.Spawn(
                    PoolObjectType.Projectile2,
                    spawnPos,
                    Quaternion.identity
                );
            }
            
            if (projectileNetObj == null)
            {
                Debug.LogError("Failed to spawn projectile from pool!");
                return;
            }
            
            if (projectileNetObj.TryGetComponent<Projectile>(out var projectile))
            {
                // Owner bilgisini bul
                GameObject owner = null;
                Player targetPlayer = null;
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
                {
                    if (networkClient.PlayerObject != null && networkClient.PlayerObject.TryGetComponent<Player>(out var foundPlayer))
                    {
                        owner = networkClient.PlayerObject.gameObject;
                        targetPlayer = foundPlayer;
                    }
                }

                projectile.Initialize(spawnPos, targetPos, owner);

                if (targetPlayer != null)
                {
                    if (targetPlayer.collectableCount.Value > 0)
                    {
                        targetPlayer.collectableCount.Value -= 1;
                    }
                    else
                    {
                        Debug.LogWarning($"Player {clientId} tried to throw but has no collectables.");
                    }
                }
                else
                {
                    // Fallback: eğer targetPlayer bulunamadıysa, yerel collectableCount'ı değiştirmeyiz
                    Debug.LogWarning("Could not find server-side Player instance to decrement collectableCount for clientId: " + clientId);
                }

                Debug.Log($"Projectile spawned from pool and initialized with owner: {(owner != null ? owner.name : "None")}");
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
            
            Debug.Log($"[SERVER] SpawnArmorServerRpc called by ClientId: {clientId}");
            
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
            {
                if (networkClient.PlayerObject != null && networkClient.PlayerObject.TryGetComponent<Player>(out var targetPlayer))
                {
                    Debug.Log($"[SERVER] Found target player for ClientId {clientId}: {targetPlayer.gameObject.name}, NetworkObjectId: {targetPlayer.NetworkObjectId}");
                    
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

                    Debug.Log($"[SERVER] Armor spawned at position: {targetPlayer.armorHolder.position} for ClientId: {clientId}");

                    if (armorNetObj.TrySetParent(targetPlayer.transform))
                    {
                        targetPlayer._currentArmor = armorNetObj;
                        targetPlayer.UpdateArmorPositionClientRpc(armorNetObj.NetworkObjectId);
                        
                        targetPlayer.hasArmor.Value = false;
                        
                        Debug.Log($"[SERVER] Armor spawned and parented successfully to ClientId {clientId}'s player!");
                    }
                    else
                    {
                        Debug.LogError("Failed to parent armor!");
                        NetworkObjectPool.Instance.Despawn(armorNetObj);
                    }
                }
                else
                {
                    Debug.LogError($"[SERVER] Could not find PlayerObject or Player component for ClientId: {clientId}");
                }
            }
            else
            {
                Debug.LogError($"[SERVER] ClientId {clientId} not found in ConnectedClients!");
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
            
            Debug.Log($"[SERVER] SpawnMagnetServerRpc called by ClientId: {clientId}");
            
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
            {
                if (networkClient.PlayerObject != null && networkClient.PlayerObject.TryGetComponent<Player>(out var targetPlayer))
                {
                    Debug.Log($"[SERVER] Found target player for ClientId {clientId}: {targetPlayer.gameObject.name}, NetworkObjectId: {targetPlayer.NetworkObjectId}");
                    
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

                    Debug.Log($"[SERVER] Magnet spawned at position: {targetPlayer.magnetHolder.position} for ClientId: {clientId}");

                    bool parentSet = magnetNetObj.TrySetParent(targetPlayer.transform);
                    
                    if (magnetNetObj.TryGetComponent<Magnet>(out var magnet))
                    {
                        magnet.SetOwnerPlayer(targetPlayer.NetworkObjectId);
                        Debug.Log($"[SERVER] Set magnet owner to player NetworkObjectId: {targetPlayer.NetworkObjectId}");
                    }

                    if (parentSet)
                    {
                        targetPlayer._currentMagnet = magnetNetObj;
                        targetPlayer.UpdateMagnetPositionClientRpc(magnetNetObj.NetworkObjectId);
                        Debug.Log($"[SERVER] Magnet spawned and parented successfully to ClientId {clientId}'s player!");
                    }
                    else
                    {
                        Debug.LogWarning("Failed to parent magnet, but magnet will still work with owner ID!");
                        targetPlayer._currentMagnet = magnetNetObj;
                    }
                    
                    targetPlayer.hasMagnet.Value = false;
                }
                else
                {
                    Debug.LogError($"[SERVER] Could not find PlayerObject or Player component for ClientId: {clientId}");
                }
            }
            else
            {
                Debug.LogError($"[SERVER] ClientId {clientId} not found in ConnectedClients!");
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
            if(collectableCount.Value <= 0)
            {
                Debug.LogWarning("No collectables available to throw!");
                return;
            }
            PlayThrowAnimation();
            Debug.Log("HandleFire called - Animation started.");
        }
        
        public void OnThrowReleasePoint()
        {
            if (_currentGun != null)
            {
                _currentGun.Fire();
                Debug.Log("Player: OnThrowReleasePoint called - Projectile spawned at frame 23!");
            }
            else
            {
                Debug.LogWarning("Player: OnThrowReleasePoint called but _currentGun is null!");
            }
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