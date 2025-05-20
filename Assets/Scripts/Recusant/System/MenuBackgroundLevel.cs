using Core;
using Netick.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recusant
{
    public class MenuBackgroundLevel : System<MenuBackgroundLevel>
    {
        public override void Initialize()
        {
            LoadSceneParameters parameters = new()
            {
                loadSceneMode = LoadSceneMode.Single,
                localPhysicsMode = LocalPhysicsMode.Physics3D
            };

            LevelManager.Instance.LoadLevel("Assets/Recusant/Levels/Background/Menu.unity", parameters);
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }
    }
}
