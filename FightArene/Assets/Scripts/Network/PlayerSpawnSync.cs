using Unity.Netcode;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Network
{
    /// <summary>
    /// Player spawn pozisyonunu tüm clientlara senkronize eder
    /// </summary>
    public class PlayerSpawnSync : NetworkBehaviour
    {
        [ClientRpc]
        public void TeleportClientRpc(Vector3 position, Quaternion rotation)
        {
            Debug.Log($"PlayerSpawnSync: TeleportClientRpc called. Moving to {position}");
            
            var characterController = GetComponent<CharacterController>();
            var rb = GetComponent<Rigidbody>();
            
            // CharacterController varsa deaktif et
            if (characterController != null)
            {
                characterController.enabled = false;
            }
            
            // Rigidbody varsa sıfırla
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Pozisyonu ayarla
            transform.position = position;
            transform.rotation = rotation;
            
            // CharacterController'ı tekrar aktif et
            if (characterController != null)
            {
                characterController.enabled = true;
            }
            
            Debug.Log($"PlayerSpawnSync: Position synchronized to {position}");
        }
    }
}

