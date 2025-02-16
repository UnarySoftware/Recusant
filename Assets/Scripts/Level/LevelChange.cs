using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelChange : MonoBehaviour
{
    [SerializeField]
    private SingleLayer _localLayer = CodeGenerated.GameObjectLayerMask.LocalPlayer;

    public string LevelName = string.Empty;

    private void OnTriggerEnter(Collider collider)
    {
        if(Networking.Instance == null || Networking.Instance.IsClient)
        {
            return;
        }

        if (collider.gameObject.layer == _localLayer)
        {
            Networking.Instance.Sandbox.LoadSceneAsync(LevelName, LoadSceneMode.Single);
        }
    }
}
