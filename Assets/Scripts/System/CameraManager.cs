using UnityEngine;

public class CameraManager : MonoBehaviour, ISystem
{
    public static CameraManager Instance = null;

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
            if(_currentCamera != null)
            {
                _currentCamera.enabled = false;
            }
            _currentCamera = value;
            _currentCamera.enabled = true;
        }
    }

    [InitDependency()]
    public void Initialize()
    {

    }

    public void Deinitialize()
    {

    }
}
