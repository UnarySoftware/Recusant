using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenu : UiState
{
    private UIDocument Document;
    private Button HostButton;
    private Button ClientButton;

#if UNITY_EDITOR

    private async void EditorAutoPress()
    {
        await Task.Run(() => Thread.Sleep(100));

        if (EditorLaunch.LaunchData.Type == EditorLaunchData.LaunchType.Host)
        {
            HostButton.Click();
        }
        else if (EditorLaunch.LaunchData.Type == EditorLaunchData.LaunchType.Client)
        {
            ClientButton.Click();
        }
    }

#endif

    public override void Initialize()
    {
        base.Initialize();

        Document = GetComponent<UIDocument>();

        HostButton = Document.rootVisualElement.Q<Button>("Host");
        HostButton.RegisterCallback<MouseUpEvent>((evt) =>
        {
            Networking.Instance.StartHost();
            Ui.Instance.GoForward(typeof(Loading));
        });

        ClientButton = Document.rootVisualElement.Q<Button>("Client");
        ClientButton.RegisterCallback<MouseUpEvent>((evt) =>
        {
            Networking.Instance.StartClient();
            Ui.Instance.GoForward(typeof(Loading));
        });

        // TODO: Move this in some other place
        Application.runInBackground = true;

#if UNITY_EDITOR
        EditorAutoPress();
#endif

    }

    public override void Deinitialize()
    {
        base.Deinitialize();
    }

    public override void Open()
    {
        base.Open();
    }

    public override void Close()
    {
        base.Close();
    }

    public override Type GetBackState()
    {
        return null;
    }
}
