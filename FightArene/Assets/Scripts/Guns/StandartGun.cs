using UnityEngine;
using Debug = Utilities.Debug;

public class StandartGun : AGun
{
    public AudioSource gunSource;
    public AudioClip fireSound;
    
    public override void Fire()
    {
        gunSource.PlayOneShot(fireSound);  
        Debug.Log("Standart Gun Fired");
    }

    public override void ExtraFire()
    {
        Debug.Log("Standart Gun Extra Fire Activated");
    }
}
