using Unity.Cinemachine;
using UnityEngine;

namespace Managers
{   
    public class CameraManager : MonoSingleton<CameraManager>
    {
        [SerializeField] private CinemachineCamera virtualCamera;

        public void SetupCameraForPlayer(Transform player, Transform cameraTarget)
        {
            Utilities.Debug.Log("Kamera ayarlanıyor...");
            if (virtualCamera == null)
            {
                Debug.LogError("Virtual Camera referansı eksik!");
                return;
            }

            virtualCamera.Follow = player;
            virtualCamera.LookAt = cameraTarget;
            
            Debug.Log($"Kamera {player.name} için ayarlandı");
        }
    }
}