using UnityEngine;

public abstract class AGun : MonoBehaviour, IFire, IExtraFire
{
    public abstract void Fire();
    public abstract void ExtraFire();
}
