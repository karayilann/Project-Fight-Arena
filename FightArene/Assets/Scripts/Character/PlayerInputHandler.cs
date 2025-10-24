using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = Utilities.Debug;

namespace Character
{
    public class PlayerInputHandler : MonoBehaviour
    {
        private InputSystem_Actions _inputActions;

        #region Move Input

        public event Action<Vector2> OnMovePerformed;
        public event Action OnMoveCanceled;
        public event Action OnJumpPerformed;
        public event Action OnSprintStarted;
        public event Action OnSprintCanceled;

        #endregion

        #region Combat Input

        public event Action OnFirePerformed;
        public event Action OnExtraFirePerformed;

        #endregion

        public Vector2 MoveInput { get; private set; }
        public bool IsSprintPressed { get; private set; }

        public bool isFirePressed => _inputActions.Combat.Fire.IsPressed();

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            _inputActions.PlayerMovement.Enable();
            _inputActions.Combat.Enable();

            _inputActions.PlayerMovement.Move.performed += OnMoveInputPerformed;
            _inputActions.PlayerMovement.Move.canceled += OnMoveInputCanceled;

            _inputActions.PlayerMovement.Jump.performed += OnJumpInputPerformed;

            _inputActions.PlayerMovement.Sprint.started += OnSprintInputStarted;
            _inputActions.PlayerMovement.Sprint.canceled += OnSprintInputCanceled;

            _inputActions.Combat.Fire.performed += FireOnperformed;
            _inputActions.Combat.ExtraFire.performed += ExtraFireOnperformed;
        }

        private void OnDisable()
        {
            _inputActions.PlayerMovement.Disable();
            _inputActions.Combat.Disable();

            _inputActions.PlayerMovement.Move.performed -= OnMoveInputPerformed;
            _inputActions.PlayerMovement.Move.canceled -= OnMoveInputCanceled;

            _inputActions.PlayerMovement.Jump.performed -= OnJumpInputPerformed;

            _inputActions.PlayerMovement.Sprint.started -= OnSprintInputStarted;
            _inputActions.PlayerMovement.Sprint.canceled -= OnSprintInputCanceled;

            _inputActions.Combat.Fire.performed -= FireOnperformed;
            _inputActions.Combat.ExtraFire.performed -= ExtraFireOnperformed;
        }

        #region Input Callback Metodları

        private void OnMoveInputPerformed(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
            Debug.Log("OnMoveInputPerformed");
            OnMovePerformed?.Invoke(MoveInput);
        }

        private void OnMoveInputCanceled(InputAction.CallbackContext context)
        {
            MoveInput = Vector2.zero;
            OnMoveCanceled?.Invoke();
        }

        private void OnJumpInputPerformed(InputAction.CallbackContext context)
        {
            OnJumpPerformed?.Invoke();
        }

        private void OnSprintInputStarted(InputAction.CallbackContext context)
        {
            IsSprintPressed = true;
            OnSprintStarted?.Invoke();
        }

        private void OnSprintInputCanceled(InputAction.CallbackContext context)
        {
            IsSprintPressed = false;
            OnSprintCanceled?.Invoke();
        }

        // Combat input callbacks


        private void ExtraFireOnperformed(InputAction.CallbackContext obj)
        {
            Debug.Log("ExtraFireOnperformed");
            OnExtraFirePerformed?.Invoke();
        }

        private void FireOnperformed(InputAction.CallbackContext obj)
        {
            Debug.Log("FireOnperformed");
            OnFirePerformed?.Invoke();
        }

        #endregion
    }
}