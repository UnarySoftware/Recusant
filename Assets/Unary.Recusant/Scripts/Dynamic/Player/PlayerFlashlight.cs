using Netick;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    public class PlayerFlashlight : NetworkBehaviourExtended
    {
        [Networked]
        public NetworkBool NetworkedEnabled { get; set; } = false;

        [OnChanged(nameof(NetworkedEnabled))]
        public void EnabledChanged(OnChangedData _)
        {
            if (IsInputSource)
            {
                return;
            }

            SetFlashlight(NetworkedEnabled.ToBool());
        }

        private bool LocalEnabled = false;

        private void SetFlashlight(bool value)
        {
            _flashlight.enabled = value;
        }

        private Light _flashlight;

        public override void NetworkStart()
        {
            _flashlight = GetComponentInChildren<Light>(true);

            if (_flashlight == null)
            {
                return;
            }

            if (IsInputSource)
            {
                _flashlight.shadows = LightShadows.Soft;
                _flashlight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
            }
            else
            {
                _flashlight.shadows = LightShadows.Hard;
                _flashlight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
            }

            SetFlashlight(false);
        }

        public override void NetworkFixedUpdate()
        {
            if (FetchInputServer(out PlayerNetworkInput serverInput))
            {
                NetworkedEnabled = serverInput.Flashlight;
            }
        }

        public override void NetworkUpdate()
        {
            if (_flashlight == null)
            {
                return;
            }

            if (IsInputSource)
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    LocalEnabled = !LocalEnabled;
                    SetFlashlight(LocalEnabled);
                }

                if (GetInput(out PlayerNetworkInput input))
                {
                    input.Flashlight |= LocalEnabled;
                    SetInput(input);
                }
            }
        }
    }
}
