using Core;

namespace Recusant
{
    public enum LevelEventType
    {
        Awake,
        AwakeNetwork,
        Destroy,
        DestoryNetwork,
        Start,
        StartNetwork
    }

    public sealed class LevelEvent : BaseEvent<LevelEvent>
    {
        public CompiledLevelData LevelData;
        public LevelRoot LevelRoot;
        public LevelEventType Type;

        public void Publish(CompiledLevelData levelData, LevelRoot levelRoot, LevelEventType type)
        {
            LevelData = levelData;
            LevelRoot = levelRoot;
            Type = type;
            Publish();
        }
    }
}
