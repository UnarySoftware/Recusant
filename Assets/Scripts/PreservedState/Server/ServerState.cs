using System;
using System.Collections.Generic;

[Serializable]
public class ServerGlobalState
{
    public float Agression = 1.0f;
    public int EnemiesKilled = 0;
}

[Serializable]
public class ServerBaseLevelState
{
    public Guid Id = Guid.Empty;
}

[Serializable]
public class ServerLevelState
{
    public string Name = string.Empty;
    public Dictionary<Guid, ServerBaseLevelState> States = new();
}

[Serializable]
public class ServerState : BaseState
{
    public ServerGlobalState GlobalState = new();
    public Dictionary<string, ServerLevelState> LevelState = new();
}
