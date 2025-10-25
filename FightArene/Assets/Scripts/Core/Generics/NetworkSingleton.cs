using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class NetworkSingleton<T> : NetworkBehaviour where T : NetworkBehaviour 
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);

            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null) _instance = this as T;
    }
}