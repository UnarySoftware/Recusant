using Netick.Unity;
using Unary.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Unary.Recusant
{
    [RequireComponent(typeof(NetworkObject))]
    public class LevelRoot : NetworkBehaviourExtended
    {
        public CompiledLevelData CompiledLevelData;
        public ProbeVolumeBakingSet ProbeVolumeBakingSet;

#if UNITY_EDITOR
        public void Destroy(GameObject target)
        {
            DestroyImmediate(target);
        }
#endif

        public void Awake()
        {
            //BakingSetHelper.SetupScene(SceneManager.GetActiveScene());



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

        bool firstFrame = true;
        bool updated = false;

        public void Update()
        {
            if (firstFrame)
            {
                firstFrame = false;
                return;
            }

            if (!updated)
            {
                //ProbeVolumeBakingSet.use
                //ProbeReferenceVolume.instance.SetActiveBakingSet(ProbeVolumeBakingSet);
                //ProbeReferenceVolume.instance.lightingScenario = "Default";
                updated = true;
            }
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
