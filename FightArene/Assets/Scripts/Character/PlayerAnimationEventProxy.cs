using UnityEngine;
using UnityEngine.Serialization;

namespace Character
{
    public class PlayerAnimationEventProxy : MonoBehaviour
    {
        public Player player;

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponentInParent<Player>();
                
            }
            
            Debug.Log("PlayerAnimationEventProxy: Successfully found Player component!");
        }

        public void OnThrowReleasePoint()
        {
            if (player != null)
            {
                player.OnThrowReleasePoint();
                Debug.Log("PlayerAnimationEventProxy: OnThrowReleasePoint called and forwarded to Player!");
            }
            else
            {
                Debug.LogError("PlayerAnimationEventProxy: Cannot forward OnThrowReleasePoint - Player is null!");
            }
        }
    }
}

