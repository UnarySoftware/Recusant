using UnityEngine;

public abstract class UiUnit : MonoBehaviour
{
    public abstract void Initialize();
    public abstract void Deinitialize();
    public abstract void Open();
    public abstract void Close();
}
