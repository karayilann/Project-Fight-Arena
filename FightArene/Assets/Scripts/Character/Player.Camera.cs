using UnityEngine;
using Managers;

namespace Character
{
    public partial class Player
    {
        [Header("Kamera Ayarları")]
        [SerializeField] private Transform cinemachineCameraTarget;
        
        private void InitCamera()
        {
            if (!IsOwner) return;

            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SetupCameraForPlayer(transform, cinemachineCameraTarget);
            }
            else
            {
                Debug.LogError("CameraManager bulunamadı!");
            }
        }
        
    }
}
