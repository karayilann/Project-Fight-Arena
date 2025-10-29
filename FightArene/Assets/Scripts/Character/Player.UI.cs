using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Character
{
    public partial class Player
    {
       
        [Header("UI")]
        [SerializeField] private Image _crosshair;
        [SerializeField] private TextMeshProUGUI _ammoText;
        [SerializeField] private TextMeshProUGUI _healthText;
        private bool isCrosshairEnabled;
        
        private void InitUI()
        {
            if (PlayerRequirements.Instance != null)
            {
                _crosshair = PlayerRequirements.Instance.crosshair;
                _ammoText = PlayerRequirements.Instance.ammoText;
                _healthText = PlayerRequirements.Instance.healthText;

                _crosshair.enabled = true;
                _ammoText.enabled = true;
                _healthText.enabled = true;
                _healthText.text = "Health: " + Mathf.RoundToInt(_health.Value);
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
