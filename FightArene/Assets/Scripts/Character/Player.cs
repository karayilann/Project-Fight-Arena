using Unity.Netcode;

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
            
            SubscribeToInput();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            UnsubscribeFromInput();
        }

        void Update()
        {
            ControllerUpdate();
        }
    }
}
