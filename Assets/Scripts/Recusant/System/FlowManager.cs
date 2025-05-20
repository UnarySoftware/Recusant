using Core;
using System.Collections.Generic;

namespace Recusant
{
    public class FlowManagerShared : SystemShared
    {
        public Dictionary<PlayerNetworkData, PlayerFlow> Players = new();
        public PlayerNetworkData Leader = null;
    }

    public class FlowManager : SystemNetworkRoot<FlowManager, FlowManagerShared>
    {
        public override void Initialize()
        {
            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            LevelEvent.Instance.Subscribe(OnDataReset, this);
            PlayerChangedEvent.Instance.Subscribe(OnPlayerChange, this);
        }

        private bool OnDataReset(LevelEvent data)
        {
            if (data.Type != LevelEventType.Awake && data.Type != LevelEventType.DestoryNetwork)
            {
                return true;
            }

            SharedData.Players.Clear();
            SharedData.Leader = null;

            return true;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            LevelEvent.Instance.Unsubscribe(this);
            PlayerChangedEvent.Instance.Unsubscribe(this);
        }

        private bool OnPlayerChange(PlayerChangedEvent data)
        {
            if (data.Added)
            {
                SharedData.Players[data.PlayerNetworkData] = data.PlayerNetworkData.GetComponent<PlayerFlow>();
            }
            else
            {
                SharedData.Players.Remove(data.PlayerNetworkData);

                if (data.PlayerNetworkData == SharedData.Leader)
                {
                    SharedData.Leader = null;
                }
            }

            return true;
        }
    }
}
