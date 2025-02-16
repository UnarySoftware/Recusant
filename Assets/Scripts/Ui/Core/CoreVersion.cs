using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class CoreVersion : UiUnit
{
    public static CoreVersion Instance = null;

    private Label VersionLabel = null;

    public override void Initialize()
    {
        Instance = this;

        var Document = GetComponent<UIDocument>();

        VersionLabel = Document.rootVisualElement.Q<Label>("VersionLabel");

        VersionLabel.text = Launcher.Instance.VersionString;
    }

    public override void Deinitialize()
    {

    }

    public override void Open()
    {

    }

    public override void Close()
    {

    }
}
