using System.Collections.Generic;
using UnityEngine;
using Debug = Utilities.Debug;

namespace Character
{
    public partial class Player
    {
        [Header("Combat")]
        private AGun _currentGun = null;
        public Transform gunHolder;

        public List<AGun> guns;
        
        private List<AGun> _spawnedGuns = new List<AGun>();
        
        void InitCombat()
        {
            EquipGun();
        }

        private void EquipGun(AGun aGun = null)
        {
            // if(_currentGun != null)
            // {
            //     _currentGun.gameObject.SetActive(false);
            // }
            
            var spawnedGun = Instantiate(guns[0], gunHolder.position, gunHolder.rotation, gunHolder);
            _currentGun = spawnedGun;
            _spawnedGuns.Add(spawnedGun);
            
            Debug.Log("Gun Equipped: " + _currentGun.name);
        }

        #region Combat Input Handlers
        
        private void HandleFire()
        {
            _currentGun?.Fire();
            
        }

        private void HandleExtraFire()
        {
            _currentGun?.ExtraFire();
            
        }

        
        #endregion
    }
}
