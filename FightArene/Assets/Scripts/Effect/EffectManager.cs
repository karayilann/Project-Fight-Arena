using Unity.Netcode;
using UnityEngine;

public class EffectManager : MonoSingleton<EffectManager>
{
    public GameObject impactEffect;
    
    [ClientRpc]
    public void ShowImpactEffectClientRpc(Vector3 position, Vector3 normal)
    {
        if (impactEffect == null) return;
        GameObject effect = Instantiate(impactEffect, position, Quaternion.LookRotation(normal));
        Destroy(effect, 2f);
    }
    
}
