using Unity.Cinemachine;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Managers
{   
    public class CameraManager : MonoSingleton<CameraManager>
    {
        [SerializeField] private CinemachineCamera virtualCamera;

        public void SetupCameraForPlayer(Transform player, Transform cameraTarget)
        {
            Debug.Log("Kamera ayarlanıyor...");
            if (virtualCamera == null)
            {
                Debug.LogError("Virtual Camera referansı eksik!");
                return;
            }

            virtualCamera.Follow = cameraTarget;
            virtualCamera.LookAt = cameraTarget;
            
            Debug.Log($"Kamera {player.name} için ayarlandı");
        }
    }
}