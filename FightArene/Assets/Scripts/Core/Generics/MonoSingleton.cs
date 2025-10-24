using System.Runtime.CompilerServices;
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);
                var monoSing = _instance as MonoSingleton<T>;
                if (monoSing) monoSing.OnInstanceLoaded();
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            var monoSing = _instance as MonoSingleton<T>;
            if (monoSing) monoSing.OnInstanceLoaded();
        }
    }

    protected virtual void OnInstanceLoaded()
    {
        // Override this method to perform actions after the instance is loaded
    }
}