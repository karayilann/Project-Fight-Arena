using System;
using Unity.Netcode;
using Utilities;

namespace Character
{
    public partial class Player : NetworkBehaviour
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
                inputHandler.OnJumpPerformed -= HandleJump;
                inputHandler.OnExtraFirePerformed -= HandleExtraFire;
                inputHandler.OnFirePerformed -= HandleFire;
            }
        }
        
        void Update()
        {
            ControllerUpdate();
        }
        
    }
}
