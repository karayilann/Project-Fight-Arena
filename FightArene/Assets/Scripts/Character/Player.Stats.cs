using Unity.Netcode;
using UnityEngine;

namespace Character
{
    public partial class Player
    {
        private readonly NetworkVariable<float> _health = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _isDead = new(false, NetworkVariableReadPermission.Everyone);

        public float Health
        {
            get => _health.Value;
            private set
            {
                if (IsOwner)
                {
                    _health.Value = Mathf.Clamp(value, 0f, 100f);
                }
            }
        }

        public void TakeDamage(float damage)
        {
            if (!IsOwner || _isDead.Value) return;

            Health -= damage;
            Debug.Log($"Player took {damage} damage. Current Health: {Health}");

            if (Health <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("Player has died.");
            DieServerRpc();
        }

        [ServerRpc]
        private void DieServerRpc()
        {
            _isDead.Value = true;
        }

    }
}
