using Netick.Unity;
using UnityEngine;

namespace Recusant
{
    [RequireComponent(typeof(NetworkObject))]
    public class LevelRoot : NetworkBehaviour
    {
        public CompiledLevelData Data;

#if UNITY_EDITOR
        public void Destroy(GameObject target)
        {
            DestroyImmediate(target);
        }
#endif

        public void Awake()
        {
            LevelManager.Instance.LevelLoaded(this);
        }

        public void Start()
        {
            LevelEvent.Instance.Publish(Data, this, LevelEventType.Start);
        }

        public override void NetworkAwake()
        {
            if (NetworkManager.Instance.Sandbox.IsServer)
            {
                Core.Systems.Instance.InitializeNetwork(Sandbox);
            }

            LevelEvent.Instance.Publish(Data, this, LevelEventType.AwakeNetwork);
        }

        public override void NetworkStart()
        {
            LevelEvent.Instance.Publish(Data, this, LevelEventType.StartNetwork);
        }

        public void OnDestroy()
        {
            LevelEvent.Instance.Publish(Data, this, LevelEventType.Destroy);
        }

        public override void NetworkDestroy()
        {
            LevelEvent.Instance.Publish(Data, this, LevelEventType.DestoryNetwork);
        }
    }
}
