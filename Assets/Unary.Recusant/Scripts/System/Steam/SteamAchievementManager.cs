using Unary.Core;

namespace Unary.Recusant
{
    public class SteamAchievementManager : System<SteamAchievementManager>
    {

#if UNITY_EDITOR

        // TODO Add this as a clickable command
        public void ResetStats()
        {
            if (!Steam.Initialized)
            {
                Logger.Instance.Error("Steam is not initialized in order to be used with this command");
                return;
            }

            if (ResetAllStats(true))
            {
                Logger.Instance.Log("Successfully reset all stats");
            }
            else
            {
                Logger.Instance.Error("Failed to reset all stats");
            }
        }

#endif

        public bool GiveAchievement(string achievementName)
        {
            if (!Steam.Initialized)
            {
                return false;
            }

            bool setResult = Steamworks.SteamUserStats.SetAchievement(achievementName);
            bool storeResult = Steamworks.SteamUserStats.StoreStats();

            return setResult && storeResult;
        }

        public bool ResetAllStats(bool achievementsToo)
        {
            if (!Steam.Initialized)
            {
                return false;
            }

            return Steamworks.SteamUserStats.ResetAllStats(achievementsToo);
        }

        public override void Initialize()
        {
            if (!Steam.Initialized)
            {
                return;
            }
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

            Steamworks.SteamUserStats.StoreStats();
        }
    }
}
