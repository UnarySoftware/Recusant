using Unary.Core;
using Netick;
using Netick.Transports.Steamworks;
using Steamworks;
using UnityEngine;

namespace Unary.Recusant
{
    public class PlayerIdentity : NetworkBehaviourExtended
    {
        [Networked]
        public int Slot { get; set; } = 0; // 0 < Slot < NetworkManager.MaxPlayerCount

        [Networked]
        public ulong SteamId { get; set; } = 0;

        [Networked]
        public NetworkString32 OfflineName { get; set; }

        public string OnlineName { get; private set; } = "Unnamed";

        public Texture2D Avatar { get; private set; }

        public override void NetworkAwake()
        {
            if (IsClient)
            {
                return;
            }

            if (Steam.Initialized)
            {
                if (InputSourcePlayerId == 0)
                {
                    SteamId = Steam.Instance.SteamId.m_SteamID;
                }
                else
                {
                    SteamId = SteamworksTransport.GetPlayerSteamID(Sandbox.GetPlayerById(InputSourcePlayerId));
                }

                OnlineName = SteamFriends.GetFriendPersonaName(new(SteamId));

                Steam.Instance.OnIdentityUpdate.Subscribe(OnIdentityUpdate, this);
            }
            else
            {
                if (InputSourcePlayerId == 0)
                {
                    OfflineName = NetworkManager.Instance.GetDeviceName(false);
                }

                SteamId = 0;
            }
        }

        public override void NetworkStart()
        {
            if (Steam.Initialized && SteamId != Steam.Instance.SteamId.m_SteamID)
            {
                SteamFriends.SetPlayedWith(new(SteamId));
            }
        }

        public override void NetworkDestroy()
        {
            if (Steam.Initialized)
            {
                Steam.Instance.OnIdentityUpdate.Unsubscribe(this);
            }
        }

        public bool OnIdentityUpdate(ref Steam.PersonaStateChangeData data)
        {
            if (InputSourcePlayerId != data.PlayerId)
            {
                return true;
            }

            if (data.OnlineName != null)
            {
                OnlineName = data.OnlineName;
            }

            if (data.Avatar != null)
            {
                Avatar = data.Avatar;
            }

            return true;
        }
    }
}
