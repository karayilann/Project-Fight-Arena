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

            if (cinemachineCameraTarget == null)
            {
                GameObject cameraTargetObj = new GameObject("CameraTarget");
                cameraTargetObj.transform.SetParent(transform);
                cameraTargetObj.transform.localPosition = new Vector3(0, 1.5f, 0);
                cinemachineCameraTarget = cameraTargetObj.transform;
            }

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
