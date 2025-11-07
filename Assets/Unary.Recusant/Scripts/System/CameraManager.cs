using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    public class CameraManager : System<CameraManager>
    {
        [SerializeField]
        private Camera _currentCamera = null;

        public Camera CurrentCamera
        {
            get
            {
                return _currentCamera;
            }
            set
            {
                if (_currentCamera != null)
                {
                    _currentCamera.enabled = false;
                }

                _currentCamera = value;

                if (_currentCamera != null)
                {
                    _currentCamera.enabled = true;
                }
            }
        }

        public override void Initialize()
        {
            CurrentCamera = GetComponent<Camera>();
            CurrentCamera = null;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }
    }
}
