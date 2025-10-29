using Utilities;

namespace Character
{
    public partial class Player : NetworkSingleton<Player>,IDamageable
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // if (!IsOwner)
            // {
            //     enabled = false;
            //     return;
            // }
            // PlayerInit();
            
            try
            {
                Debug.Log($"Player spawned! IsOwner: {IsOwner}, ClientId: {OwnerClientId}, NetworkObjectId: {NetworkObjectId}");
        
                if (!IsLocalPlayer)
                {
                    Debug.Log("Bu benim player'ım değil, kontrol etmeyeceğim.");
                    // Input component'lerini devre dışı bırak
                    var inputHandler = GetComponent<PlayerInputHandler>();
                    if (inputHandler) inputHandler.enabled = false;
                    return;
                }
        
                Debug.Log("Bu benim player'ım! Kontrol ediyorum.");
                PlayerInit();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Player.OnNetworkSpawn HATA: {e.Message}\n{e.StackTrace}");
            }
        }
        
        
        private void PlayerInit()
        {
            SubscribeToInput();
            InitCombat();
            InitUI();
            InitCamera();
            InitInventory();
            InitSkills();
            InitAnimation();
        }
        
        void Update()
        {
            ControllerUpdate();
            UpdateAnimation();
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
                inputHandler.OnMagnetSelected += HandleMagnetSelected;
                inputHandler.OnArmorSelected += HandleArmorSelected;
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
                inputHandler.OnMagnetSelected -= HandleMagnetSelected;
                inputHandler.OnArmorSelected -= HandleArmorSelected;
            }
        }
        
    }
}
