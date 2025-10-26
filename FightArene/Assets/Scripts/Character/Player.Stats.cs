using Unity.Netcode;
using UnityEngine;

namespace Character
{
    public partial class Player
    {
        private NetworkVariable<float> _health = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> _isDead = new(false, NetworkVariableReadPermission.Everyone);
        
        public float Health
        {
            get => _health.Value;
            private set
            {
                if (IsServer)
                {
                    _health.Value = Mathf.Clamp(value, 0f, 100f);
                }
            }
        }

        public void TakeDamage(float damage)
        {
            TakeDamageServerRpc(damage);
        }

        [ServerRpc(RequireOwnership = false)]
        private void TakeDamageServerRpc(float damage)
        {
            if (_isDead.Value) return;

            Health -= damage;
            Debug.Log($"Player {OwnerClientId} took {damage} damage. Current Health: {Health}");

            if (Health <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (!IsServer) return;
            
            Debug.Log($"Player {OwnerClientId} has died.");
            _isDead.Value = true;
        }

    }
}
