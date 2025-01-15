using UnityEngine;

public class Steam : MonoBehaviour, ISystem
{
    public static Steam Instance = null;

    public bool Initialized { get; private set; } = false;

    [InitDependency(typeof(Analytics))]
    public void Initialize()
    {
        

        try
        {
            Steamworks.SteamClient.Init(1436420, true);
        }
        catch (System.Exception)
        {
            Application.Quit();
        }
    }

    private void Update()
    {
        Steamworks.SteamClient.RunCallbacks();
    }

    public void Deinitialize()
    {
        Steamworks.SteamClient.Shutdown();
    }
}
