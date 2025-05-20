using Core;

namespace Recusant
{
    public sealed class LevelTransitionEvent : BaseEvent<LevelTransitionEvent>
    {
        public int CurrentCount;
        public int TargetCount;

        public void Publish(int currentCount, int targetCount)
        {
            CurrentCount = currentCount;
            TargetCount = targetCount;
            Publish();
        }
    }
}
