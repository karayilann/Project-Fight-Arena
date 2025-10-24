using Unity.Netcode;
using UnityEngine;

namespace Character
{
    public partial class Player
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
        
        private void ControllerUpdate()
        {
            if (!IsOwner) return;
            
            CheckGroundStatus();
            HandleMovement();
            HandleRotation();
        }

        private void CheckGroundStatus()
        {
            _isGrounded = characterController.isGrounded;
            
            if (!_isGrounded)
            {
                Vector3 rayStart = transform.position + Vector3.up * 0.1f;
                _isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + 0.1f, groundMask);
            }

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
        }

        private void HandleMovement()
        {
            if (!_isGrounded)
            {
                _velocity.y -= gravity * Time.deltaTime;
            }

            if (_jumpRequested && _isGrounded)
            {
                _velocity.y = jumpForce;
                _jumpRequested = false;
            }

            Vector3 moveDirection = Vector3.zero;

            if (_moveInput.sqrMagnitude >= 0.01f)
            {
                Vector3 forward = cinemachineCameraTarget.forward;
                Vector3 right = cinemachineCameraTarget.right;

                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                moveDirection = (forward * _moveInput.y + right * _moveInput.x).normalized;
            }

            float currentSpeed = (inputHandler != null && inputHandler.IsSprintPressed) ? sprintSpeed : moveSpeed;
            Vector3 horizontalVelocity = moveDirection * currentSpeed;

            Vector3 finalMovement = horizontalVelocity + new Vector3(0, _velocity.y, 0);
            characterController.Move(finalMovement * Time.deltaTime);
        }

        private void HandleRotation()
        {
            if (_moveInput.sqrMagnitude >= 0.01f)
            {
                Vector3 forward = cinemachineCameraTarget.forward;
                Vector3 right = cinemachineCameraTarget.right;

                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 moveDirection = (forward * _moveInput.y + right * _moveInput.x).normalized;

                if (moveDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }
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