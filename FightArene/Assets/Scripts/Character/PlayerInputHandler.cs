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
        public event Action<Vector2> OnLookPerformed;
        public event Action OnJumpPerformed;
        public event Action OnSprintStarted;
        public event Action OnSprintCanceled;
        public event Action OnArmorSelected;
        public event Action OnMagnetSelected;

        #endregion

        #region Combat Input

        public event Action OnFirePerformed;
        public event Action OnExtraFirePerformed;

        #endregion

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool IsSprintPressed { get; private set; }
        
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
            
            _inputActions.PlayerMovement.Look.performed += OnLookInputPerformed;
            _inputActions.PlayerMovement.Look.canceled += OnLookInputCanceled;
            
            _inputActions.PlayerMovement.Jump.performed += OnJumpInputPerformed;

            _inputActions.PlayerMovement.Sprint.started += OnSprintInputStarted;
            _inputActions.PlayerMovement.Sprint.canceled += OnSprintInputCanceled;
            
            _inputActions.PlayerMovement.Armor.performed += OnArmorInputPerformed;
            _inputActions.PlayerMovement.Magnet.performed += OnMagnetInputPerformed;

            // Combat
            _inputActions.Combat.Fire.performed += FireOnperformed;
            _inputActions.Combat.ExtraFire.performed += ExtraFireOnperformed;
        }


        private void OnDisable()
        {
            _inputActions.PlayerMovement.Disable();
            _inputActions.Combat.Disable();

            _inputActions.PlayerMovement.Move.performed -= OnMoveInputPerformed;
            _inputActions.PlayerMovement.Move.canceled -= OnMoveInputCanceled;

            _inputActions.PlayerMovement.Look.performed -= OnLookInputPerformed;
            _inputActions.PlayerMovement.Look.canceled -= OnLookInputCanceled;
            
            _inputActions.PlayerMovement.Jump.performed -= OnJumpInputPerformed;

            _inputActions.PlayerMovement.Sprint.started -= OnSprintInputStarted;
            _inputActions.PlayerMovement.Sprint.canceled -= OnSprintInputCanceled;
            
            // Combat
            _inputActions.Combat.Fire.performed -= FireOnperformed;
            _inputActions.Combat.ExtraFire.performed -= ExtraFireOnperformed;
        }
        
        
        #region Input Callback Metodları

        private void OnMoveInputPerformed(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
            OnMovePerformed?.Invoke(MoveInput);
        }

        private void OnMoveInputCanceled(InputAction.CallbackContext context)
        {
            MoveInput = Vector2.zero;
            OnMoveCanceled?.Invoke();
        }

        private void OnLookInputPerformed(InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
            OnLookPerformed?.Invoke(LookInput);
        }
        
        private void OnLookInputCanceled(InputAction.CallbackContext context)
        {
            LookInput = Vector2.zero;
            OnLookPerformed?.Invoke(LookInput);
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
        
        private void OnMagnetInputPerformed(InputAction.CallbackContext obj)
        {
            OnMagnetSelected?.Invoke();
        }

        private void OnArmorInputPerformed(InputAction.CallbackContext obj)
        {
            OnArmorSelected?.Invoke();
        }
        
        // Combat input callbacks


        private void ExtraFireOnperformed(InputAction.CallbackContext obj)
        {
            OnExtraFirePerformed?.Invoke();
        }

        private void FireOnperformed(InputAction.CallbackContext obj)
        {
            OnFirePerformed?.Invoke();
        }

        #endregion
    }
}