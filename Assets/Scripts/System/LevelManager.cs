using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : CoreSystem<LevelManager>
{
    [InitDependency(typeof(Logger))]
    public override void Initialize()
    {
        SceneManager.LoadScene("Menu");
    }

    public override void PostInitialize()
    {

    }

    public override void Deinitialize()
    {

    }
}
