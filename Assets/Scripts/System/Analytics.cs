using UnityEngine;

public class Analytics : MonoBehaviour, ISystem
{
    public static Analytics Instance = null;

    [InitDependency()]
    public void Initialize()
    {

    }

    public void Deinitialize()
    {
        
    }
}
