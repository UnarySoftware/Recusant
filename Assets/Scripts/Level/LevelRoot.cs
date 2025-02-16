using Netick.Unity;
using System.Threading;

public class LevelRoot : NetworkBehaviour
{
    public string Name;

    public override void NetworkStart()
    {
        if(Networking.Instance.Sandbox.IsServer)
        {
            Core.Instance.SpawnNetwork();
        }

        Networking.Instance.LevelLoaded(Name, this);
    }
}
