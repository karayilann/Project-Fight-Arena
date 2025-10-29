using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRequirements : MonoSingleton<PlayerRequirements>
{
    [Header("UI Elements")]
    public Image crosshair;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI healthText;
    
    [Header("Skill UI")] 
    public Image armorImage;
    public Image armorTimer;
    public Image armorBackground;
    
    public Image magnetImage;
    public Image magnetTimer;
    public Image magnetBackground;
    
    
    
}
