using Netick.Unity;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [RequireComponent(typeof(NetworkObject))]
    public class LevelRoot : NetworkBehaviourExtended
    {
        public CompiledLevelData CompiledLevelData;

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
            LevelManager.Instance.OnStart.Publish(new()
            {
                LevelDefinition = LevelManager.Instance.LevelDefinition,
                LevelRoot = this
            });
        }

        public override void NetworkAwake()
        {
            if (NetworkManager.Instance.Sandbox.IsServer)
            {
                Systems.Instance.InitializeNetwork(Sandbox);
            }

            LevelManager.Instance.OnAwakeNetwork.Publish(new()
            {
                LevelDefinition = LevelManager.Instance.LevelDefinition,
                LevelRoot = this
            });
        }

        public override void NetworkStart()
        {
            LevelManager.Instance.OnStartNetwork.Publish(new()
            {
                LevelDefinition = LevelManager.Instance.LevelDefinition,
                LevelRoot = this
            });
        }

        public void OnDestroy()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnDestroy.Publish(new()
                {
                    LevelDefinition = LevelManager.Instance.LevelDefinition,
                    LevelRoot = this
                });
            }
        }

        public override void NetworkDestroy()
        {
            LevelManager.Instance.OnDestroyNetwork.Publish(new()
            {
                LevelDefinition = LevelManager.Instance.LevelDefinition,
                LevelRoot = this
            });
        }
    }
}
