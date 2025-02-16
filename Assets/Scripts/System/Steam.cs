using UnityEngine;

public class Steam : CoreSystem<Steam>
{
    public bool Initialized { get; private set; } = false;

    [InitDependency(typeof(Logger), typeof(Launcher))]
    public override void Initialize()
    {

#if UNITY_EDITOR
        if (!Launcher.LaunchData.Online)
        {
            Logger.Instance.Log("Starting Steam in offline mode");
            return;
        }
#endif

        Logger.Instance.Log("Starting Steam in online mode");

        try
        {
            Steamworks.SteamClient.Init(1436420, true);
            Initialized = true;
        }
        catch (System.Exception)
        {
            Application.Quit();
        }
    }

    public override void PostInitialize()
    {

    }

    public override void Deinitialize()
    {
        if (Initialized)
        {
            Steamworks.SteamClient.Shutdown();
        }
    }

    private void Update()
    {
        if (Initialized)
        {
            Steamworks.SteamClient.RunCallbacks();
        }
    }
}
