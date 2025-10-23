using Unity.Netcode;
using UnityEngine;

namespace Player
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Hareket Ayarları")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float gravity = 20f;
        
        [Header("Zemin Kontrolü")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;
        
        [Header("Referanslar")]
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private CharacterController characterController;
        
        private Vector2 _moveInput;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _jumpRequested;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!IsOwner)
            {
                enabled = false;
                return;
            }
            
            SubscribeToInput();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            UnsubscribeFromInput();
        }

        private void SubscribeToInput()
        {
            if (inputHandler != null)
            {
                inputHandler.OnMovePerformed += HandleMoveInput;
                inputHandler.OnMoveCanceled += HandleMoveCanceled;
                inputHandler.OnJumpPerformed += HandleJump;
            }
        }

        private void UnsubscribeFromInput()
        {
            if (inputHandler != null)
            {
                inputHandler.OnMovePerformed -= HandleMoveInput;
                inputHandler.OnMoveCanceled -= HandleMoveCanceled;
                inputHandler.OnJumpPerformed -= HandleJump;
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            CheckGroundStatus();
            HandleMovement();
            HandleRotation();
        }

        private void CheckGroundStatus()
        {
            // CharacterController'ın built-in ground check'i
            _isGrounded = characterController.isGrounded;
            
            // Ekstra raycast kontrolü (daha güvenilir)
            if (!_isGrounded)
            {
                Vector3 rayStart = transform.position + Vector3.up * 0.1f;
                _isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + 0.1f, groundMask);
            }

            // Yerdeyken velocity'yi sıfırla
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Yere yapışmasını sağla
            }
        }

        private void HandleMovement()
        {
            // Gravity uygula
            if (!_isGrounded)
            {
                _velocity.y -= gravity * Time.deltaTime;
            }

            // Jump işle
            if (_jumpRequested && _isGrounded)
            {
                _velocity.y = jumpForce;
                _jumpRequested = false;
            }

            // Yatay hareket hesapla
            Vector3 moveDirection = CalculateMoveDirection();
            float currentSpeed = (inputHandler != null && inputHandler.IsSprintPressed) ? sprintSpeed : moveSpeed;
            Vector3 horizontalVelocity = moveDirection * currentSpeed;

            // Toplam hareketi uygula
            Vector3 finalMovement = horizontalVelocity + new Vector3(0, _velocity.y, 0);
            characterController.Move(finalMovement * Time.deltaTime);
        }

        private Vector3 CalculateMoveDirection()
        {
            // Input yoksa hareket yok
            if (_moveInput.magnitude < 0.01f)
            {
                return Vector3.zero;
            }

            // Kamera bazlı hareket yönü
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (Camera.main != null)
            {
                forward = Camera.main.transform.forward;
                right = Camera.main.transform.right;
                
                // Y eksenini sıfırla (sadece yatay hareket)
                forward.y = 0f;
                right.y = 0f;
                
                forward.Normalize();
                right.Normalize();
            }

            // Hareket yönünü hesapla
            Vector3 direction = (forward * _moveInput.y + right * _moveInput.x).normalized;
            return direction;
        }

        private void HandleRotation()
        {
            Vector3 moveDirection = CalculateMoveDirection();
            
            // Hareket varsa karakteri döndür
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        #region Input Handlers

        private void HandleMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        private void HandleMoveCanceled()
        {
            _moveInput = Vector2.zero;
        }

        private void HandleJump()
        {
            if (_isGrounded)
            {
                _jumpRequested = true;
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(rayStart, rayStart + Vector3.down * (groundCheckDistance + 0.1f));
            Gizmos.DrawWireSphere(rayStart + Vector3.down * (groundCheckDistance + 0.1f), 0.1f);
        }

        #endregion
    }
}