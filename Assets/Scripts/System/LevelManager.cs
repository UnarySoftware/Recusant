using UnityEngine;

public class LevelManager : MonoBehaviour, ISystem
{
    public static LevelManager Instance = null;

    [InitDependency(typeof(Analytics))]
    public void Initialize()
    {
        
    }

    public void Deinitialize()
    {

    }
}
