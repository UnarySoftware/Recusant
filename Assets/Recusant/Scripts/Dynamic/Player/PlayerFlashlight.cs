using Netick.Unity;
using UnityEngine;

namespace Recusant
{
    public class PlayerFlashlight : NetworkBehaviour
    {
        public static GameplayVariable<bool> EnableShadows = new(
            GameplayGroup.Server, GameplayFlag.Replicated, false, "Should our flashlight have detailed shadows");

        private Light _flashlight;
        private LightShadows _currentShadows;

        public override void NetworkStart()
        {
            _flashlight = GetComponentInChildren<Light>(true);

            if (_flashlight == null)
            {
                return;
            }

            _currentShadows = _flashlight.shadows;
        }

        public override void NetworkUpdate()
        {
            if(_flashlight == null)
            {
                return;
            }

            if(EnableShadows.Get())
            {
                if (_currentShadows == LightShadows.None)
                {
                    _currentShadows = LightShadows.Soft;
                    _flashlight.shadows = LightShadows.Soft;
                    _flashlight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
                }
            }
            else
            {
                if(_currentShadows == LightShadows.Soft)
                {
                    _currentShadows = LightShadows.None;
                    _flashlight.shadows = LightShadows.None;
                }
            }
        }
    }
}
