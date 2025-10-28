using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Character
{
    public partial class Player
    {
        [Header("Animation")]
        [SerializeField] private Animator animator;
        
        // Animator parameter names
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int ThrowHash = Animator.StringToHash("Throw");
        
        // Network variables for animation sync
        private NetworkVariable<float> _networkSpeed = new NetworkVariable<float>(0f, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Owner);
        
        private NetworkVariable<bool> _networkIsGrounded = new NetworkVariable<bool>(true, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Owner);

        void InitAnimation()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    Debug.LogError("Player: Animator component not found!");
                    return;
                }
            }
            
            // AnimatorController kontrolü
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError("Player: Animator has no AnimatorController assigned! Please assign an AnimatorController in the Inspector.");
                animator = null; // Animator'ı null yap ki UpdateAnimation çalışmasın
                return;
            }
            
            // Network variable değişikliklerini dinle
            if (!IsOwner)
            {
                _networkSpeed.OnValueChanged += OnSpeedChanged;
                _networkIsGrounded.OnValueChanged += OnGroundedChanged;
            }
            
            Debug.Log("Player: Animation initialized successfully!");
        }

        void UpdateAnimation()
        {
            // Animator veya Controller yoksa çalışma
            if (!IsOwner || animator == null || animator.runtimeAnimatorController == null) return;
            
            // Hareket hızını hesapla
            float currentSpeed = 0f;
            
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                currentSpeed = inputHandler.IsSprintPressed ? 1f : 0.5f; // Sprint: 1.0, Walk: 0.5
            }
            
            // Network variable'ı güncelle (sadece owner)
            _networkSpeed.Value = currentSpeed;
            _networkIsGrounded.Value = _isGrounded;
            
            // Local animator'ı güncelle
            animator.SetFloat(SpeedHash, currentSpeed);
            animator.SetBool(IsGroundedHash, _isGrounded);
            
            // Jump animasyonu
            if (_velocity.y > 0.5f)
            {
                animator.SetBool(IsJumpingHash, true);
            }
            else if (_isGrounded)
            {
                animator.SetBool(IsJumpingHash, false);
            }
        }
        
        private void OnSpeedChanged(float previousValue, float newValue)
        {
            if (animator != null && !IsOwner)
            {
                animator.SetFloat(SpeedHash, newValue);
            }
        }
        
        private void OnGroundedChanged(bool previousValue, bool newValue)
        {
            if (animator != null && !IsOwner)
            {
                animator.SetBool(IsGroundedHash, newValue);
                
                // Jump animasyonu kontrolü
                if (!newValue) // Havada
                {
                    animator.SetBool(IsJumpingHash, true);
                }
                else // Yerde
                {
                    animator.SetBool(IsJumpingHash, false);
                }
            }
        }

        // Throw animasyonu (HandleFire çağrıldığında kullanılacak)
        public void PlayThrowAnimation()
        {
            if (animator == null) return;
            
            // Server'a throw animasyonu isteği gönder
            if (IsOwner)
            {
                PlayThrowAnimationServerRpc();
            }
        }
        
        [ServerRpc]
        private void PlayThrowAnimationServerRpc(ServerRpcParams rpcParams = default)
        {
            // Tüm clientlara throw animasyonunu oynat
            PlayThrowAnimationClientRpc();
        }
        
        [ClientRpc]
        private void PlayThrowAnimationClientRpc()
        {
            if (animator != null)
            {
                animator.SetTrigger(ThrowHash);
                Debug.Log("Player: Throw animation triggered!");
            }
        }
        
        private void OnDisable()
        {
            // Network variable listener'ları temizle
            if (!IsOwner)
            {
                _networkSpeed.OnValueChanged -= OnSpeedChanged;
                _networkIsGrounded.OnValueChanged -= OnGroundedChanged;
            }
        }
    }
}
