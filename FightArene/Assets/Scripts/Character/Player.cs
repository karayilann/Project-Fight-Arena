using System;
using Unity.Netcode;
using Utilities;

namespace Character
{
    public partial class Player : NetworkSingleton<Player>,IDamageable
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!IsOwner)
            {
                enabled = false;
                return;
            }
            PlayerInit();
        }

        private void PlayerInit()
        {
            SubscribeToInput();
            InitCombat();
            InitUI();
            InitCamera();
        }
        
                
        void Update()
        {
            ControllerUpdate();
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
                inputHandler.OnLookPerformed += HandleLookInput;
                inputHandler.OnJumpPerformed += HandleJump;
                inputHandler.OnExtraFirePerformed += HandleExtraFire;
                inputHandler.OnFirePerformed += HandleFire;
            }
        }

        private void UnsubscribeFromInput()
        {
            if (inputHandler != null)
            {
                inputHandler.OnMovePerformed -= HandleMoveInput;
                inputHandler.OnMoveCanceled -= HandleMoveCanceled;
                inputHandler.OnLookPerformed -= HandleLookInput;
                inputHandler.OnJumpPerformed -= HandleJump;
                inputHandler.OnExtraFirePerformed -= HandleExtraFire;
                inputHandler.OnFirePerformed -= HandleFire;
            }
        }
        
    }
}
