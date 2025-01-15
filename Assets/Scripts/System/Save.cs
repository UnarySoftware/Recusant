using UnityEngine;

public class Save : MonoBehaviour, ISystem
{
    public static Save Instance = null;

    [InitDependency(typeof(Analytics), typeof(Steam))]
    public void Initialize()
    {
        
    }

    public void Deinitialize()
    {
        
    }
}
