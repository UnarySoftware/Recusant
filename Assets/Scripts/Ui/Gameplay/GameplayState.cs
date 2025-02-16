using System;

public class GameplayState : UiState
{
    public override void Initialize()
    {
        base.Initialize();
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
        return typeof(MainMenuState);
    }
}
