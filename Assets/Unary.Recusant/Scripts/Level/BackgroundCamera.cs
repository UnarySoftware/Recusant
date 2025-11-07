using UnityEngine;

namespace Unary.Recusant
{
    public class BackgroundCamera : MonoBehaviour
    {
        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.enabled = true;
        }
    }
}
