using UnityEngine;

public class TestTrigger : MonoBehaviour
{
    [SerializeField]
    private SingleLayer _localLayer = CodeGenerated.GameObjectLayerMask.LocalPlayer;

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.layer == _localLayer)
        {
            collider.gameObject.GetComponent<PlayerCharacterController>().Teleport(Vector3.zero, Quaternion.identity);
        }
    }
}
