using Core;

namespace Recusant
{
    public class SteamAchievementManager : System<SteamAchievementManager>
    {

#if UNITY_EDITOR

        [GameplayCommand(GameplayGroup.Client, GameplayFlag.None, "Resets all Steam stats (including achievements)")]
        public void ResetStats()
        {
            if (!Steam.Instance.Initialized)
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
            if (!Steam.Instance.Initialized)
            {
                return false;
            }

            bool setResult = Steamworks.SteamUserStats.SetAchievement(achievementName);
            bool storeResult = Steamworks.SteamUserStats.StoreStats();

            return setResult && storeResult;
        }

        public bool ResetAllStats(bool achievementsToo)
        {
            if (!Steam.Instance.Initialized)
            {
                return false;
            }

            return Steamworks.SteamUserStats.ResetAllStats(achievementsToo);
        }

        public override void Initialize()
        {
            if (!Steam.Instance.Initialized)
            {
                return;
            }
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            if (!Steam.Instance.Initialized)
            {
                return;
            }

            Steamworks.SteamUserStats.StoreStats();
        }
    }
}
