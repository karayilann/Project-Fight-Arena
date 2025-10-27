using Unity.Netcode;
using Utilities;

namespace Character
{
    public partial class Player
    {
        private NetworkVariable<bool> hasArmor = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> hasMagnet = new NetworkVariable<bool>(false);

        void InitSkills()
        {
            hasArmor.OnValueChanged += OnArmorChanged;
            hasMagnet.OnValueChanged += OnMagnetChanged;
        }

        private void OnArmorChanged(bool previous, bool current)
        {
            Debug.Log($"Armor skill: {current}");
        }

        private void OnMagnetChanged(bool previous, bool current)
        {
            Debug.Log($"Magnet skill: {current}");
        }

        private void CleanupSkills()
        {
            hasArmor.OnValueChanged -= OnArmorChanged;
            hasMagnet.OnValueChanged -= OnMagnetChanged;
        }
    }
}
