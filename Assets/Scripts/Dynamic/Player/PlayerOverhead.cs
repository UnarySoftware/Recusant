using TMPro;
using UnityEngine;

public class PlayerOverhead : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _label;

    private CameraManager Manager = null;

    private void Start()
    {
        _label.transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);

        Manager = CameraManager.Instance;
    }

    public void SetName(string name)
    {
        _label.enabled = true;
        _label.text = name;
    }

    void Update()
    {
        if (Manager.CurrentCamera != null)
        {
            _label.transform.LookAt(Manager.CurrentCamera.transform.position);
        }
    }
}
