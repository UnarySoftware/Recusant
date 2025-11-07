using UnityEngine;

namespace Unary.Recusant
{
    [RequireComponent(typeof(PlayerNetworkInfo))]
    public class PlayerOutliner : Outliner
    {
        [ColorUsage(true, true)]
        public Color DefaultGlow = new();

        [ColorUsage(true, true)]
        public Color LeaderGlow = new();

        public override void NetworkStart()
        {
            base.NetworkStart();

            if (IsInputSource)
            {
                Highlight.highlighted = false;
                return;
            }

            GetComponent<PlayerFlow>().OnLeaderChanged.Subscribe(OnLeaderChanged, this);
        }

        public override void NetworkDestroy()
        {
            GetComponent<PlayerFlow>().OnLeaderChanged.Unsubscribe(this);
        }

        private bool OnLeaderChanged(ref bool data)
        {
            if (data)
            {
                Highlight.glowHQColor = LeaderGlow;
            }
            else
            {
                Highlight.glowHQColor = DefaultGlow;
            }

            return true;
        }
    }
}
