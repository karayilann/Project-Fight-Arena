using Unity.Netcode;
using UnityEngine;

namespace Character
{
    public partial class Player
    {
        [Header("Hareket Ayarları")] [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float gravity = 20f;

        [Header("Rotasyon Ayarları")] [SerializeField]
        private float rotationSpeed = 10f;

        [SerializeField] private float lookSensitivity;
        [SerializeField] private float verticalLookLimit;
        [SerializeField] private float horizontalLookLimit;

        [Header("Zemin Kontrolü")] [SerializeField]
        private float groundCheckDistance = 0.2f;

        [SerializeField] private LayerMask groundMask;

        [Header("Referanslar")] [SerializeField]
        private PlayerInputHandler inputHandler;

        [SerializeField] private CharacterController characterController;

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _jumpRequested;

        private float _cameraPitch = 0f;
        private float _yawOffset = 0f;


        private void ControllerUpdate()
        {
            if (!IsOwner) return;

            CheckGroundStatus();
            HandleCameraLook();
            HandleMovement();
        }

        #region Zemin Kontrolü

        private void CheckGroundStatus()
        {
            _isGrounded = characterController.isGrounded;

            if (!_isGrounded)
            {
                Vector3 rayStart = transform.position + Vector3.up * 0.1f;
                _isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + 0.1f, groundMask);
            }

            if (_isGrounded && _velocity.y < 0)
                _velocity.y = -2f;
        }

        #endregion

        #region Kamera Rotasyonu

        private void HandleCameraLook()
        {
            float mouseX = _lookInput.x * lookSensitivity * Time.deltaTime;
            float mouseY = _lookInput.y * lookSensitivity * Time.deltaTime;

            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -verticalLookLimit, verticalLookLimit);

            cinemachineCameraTarget.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);

            _yawOffset += mouseX;

            if (Mathf.Abs(_yawOffset) > horizontalLookLimit)
            {
                float rotateAmount = _yawOffset > 0
                    ? _yawOffset - horizontalLookLimit
                    : _yawOffset + horizontalLookLimit;

                transform.Rotate(Vector3.up, rotateAmount);

                _yawOffset -= rotateAmount;
            }

            cinemachineCameraTarget.parent.localRotation = Quaternion.Euler(0, _yawOffset, 0);
        }


        // private void HandleCameraLook()
        // {
        //     float mouseX = _lookInput.x * lookSensitivity * Time.deltaTime;
        //     float mouseY = _lookInput.y * lookSensitivity * Time.deltaTime;
        //
        //     _cameraPitch -= mouseY;
        //     _cameraPitch = Mathf.Clamp(_cameraPitch, -verticalLookLimit, verticalLookLimit);
        //
        //     cinemachineCameraTarget.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        //
        //     // Karakteri yatayda mouseX kadar döndür (Sınırsız şekilde)
        //     transform.Rotate(Vector3.up, mouseX * rotationSpeed);
        //
        //     cinemachineCameraTarget.parent.localRotation = Quaternion.identity;
        // }

        #endregion

        #region Hareket Sistemi

        private void HandleMovement()
        {
            if (!_isGrounded)
                _velocity.y -= gravity * Time.deltaTime;

            if (_jumpRequested && _isGrounded)
            {
                _velocity.y = jumpForce;
                _jumpRequested = false;
            }

            Vector3 moveDir = Vector3.zero;

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 forward = cinemachineCameraTarget.forward;
                Vector3 right = cinemachineCameraTarget.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                moveDir = (forward * _moveInput.y + right * _moveInput.x).normalized;
            }

            float speed = inputHandler.IsSprintPressed ? sprintSpeed : moveSpeed;
            Vector3 finalMove = moveDir * speed + Vector3.up * _velocity.y;

            characterController.Move(finalMove * Time.deltaTime);
        }

        #endregion

        #region Input Olayları

        private void HandleMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        private void HandleMoveCanceled()
        {
            _moveInput = Vector2.zero;
        }

        private void HandleLookInput(Vector2 input)
        {
            _lookInput = input;
        }

        private void HandleJump()
        {
            if (_isGrounded)
                _jumpRequested = true;
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