using Netick;
using System;
using UnityEngine;

namespace Recusant
{
    [RequireComponent(typeof(PlayerNetworkData))]
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

            PlayerLeaderChangedEvent.Instance.Subscribe(OnLeaderChanged, this);
        }

        public override void NetworkDestroy()
        {
            PlayerLeaderChangedEvent.Instance.Unsubscribe(this);
            base.NetworkDestroy();
        }

        private bool OnLeaderChanged(PlayerLeaderChangedEvent data)
        {
            if (data.Root.gameObject != gameObject)
            {
                return true;
            }

            if (data.Value)
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
