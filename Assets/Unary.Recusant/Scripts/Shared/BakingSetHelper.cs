using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Unary.Recusant
{
    public class BakingSetHelper
    {
        private static readonly List<GameObject> _objects = new();

        public static void SetupScene(Scene scene)
        {
            _objects.Clear();
            scene.GetRootGameObjects(_objects);

            foreach (GameObject gameObject in _objects)
            {
                if (gameObject.TryGetComponent(out LevelRoot levelRoot))
                {
                    //ProbeReferenceVolume.instance.SetActiveScene(scene);
                    ProbeReferenceVolume.instance.SetActiveBakingSet(levelRoot.ProbeVolumeBakingSet);

                    //ProbeReferenceVolume.instance.lightingScenario = "Default";
                    //var scene = SceneManager.GetActiveScene();
                    //ProbeReferenceVolume.instance.SetActiveScene(scene);

                    break;
                }
            }
        }

        public static void SetupScene(LevelRoot levelRoot)
        {
            ProbeReferenceVolume.instance.SetActiveBakingSet(levelRoot.ProbeVolumeBakingSet);
        }
    }
}
