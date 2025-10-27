using UnityEngine;
using UnityEngine.UI;

namespace Character
{
    public partial class Player
    {
       
        [Header("UI")]
        private Image _crosshair;

        private bool isCrosshairEnabled;
        
        private void InitUI()
        {
            // UI referanslarını cache'le
            if (PlayerRequirements.Instance != null)
            {
                _crosshair = PlayerRequirements.Instance.crosshair;
            }
            else
            {
                Debug.LogError("PlayerRequirements.Instance is NULL in InitUI!");
                return;
            }
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            if (_crosshair != null)
            {
                _crosshair.enabled = true;
                isCrosshairEnabled = true;
            }
        }

        public void EnableCursor()
        {
            if (isCrosshairEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (_crosshair != null) _crosshair.enabled = false;
                isCrosshairEnabled = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                if (_crosshair != null) _crosshair.enabled = true;
                isCrosshairEnabled = true;
            }
        }
        
    }
}
