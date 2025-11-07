using Steamworks;
using Unary.Core;

namespace Unary.Recusant
{
    public class SteamRichPresenceManager : System<SteamRichPresenceManager>
    {
        public override void Initialize()
        {
            if (!Steam.Initialized)
            {
                return;
            }

            LevelManager.Instance.OnAwake.Subscribe(OnLevelAwake, this);
        }

        private bool OnLevelAwake(ref LevelManager.LevelEventData data)
        {
            SteamFriends.SetRichPresence("text", data.LevelData.LevelName);
            SteamFriends.SetRichPresence("steam_display", "#Status");
            return true;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            if (!Steam.Initialized)
            {
                return;
            }

            LevelManager.Instance.OnAwake.Unsubscribe(this);
        }
    }
}
