using UnityEngine;
using UnityEngine.UI;

namespace Character
{
    public partial class Player
    {
       
        [Header("UI")]
        private Image crosshair => PlayerRequirements.Instance.crosshair;

        private bool isCrosshairEnabled;
        private void InitUI()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            crosshair.enabled = true;
            isCrosshairEnabled = true;
        }

        public void EnableCursor()
        {
            if (isCrosshairEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                crosshair.enabled = false;
                isCrosshairEnabled = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                crosshair.enabled = true;
                isCrosshairEnabled = true;
            }
        }
        
    }
}
