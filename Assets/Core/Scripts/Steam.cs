using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Core
{
    public class Steam : CoreSystem<Steam>
    {
        public const uint AppId = 1436420;

        public bool Initialized { get; private set; } = false;

        private SteamAPIWarningMessageHook_t _messageHook = null;

        [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
        private static void MessageHook(int nSeverity, StringBuilder pchDebugText)
        {
            // TODO Move this to an extension, since prepending is sometimes necessary
            pchDebugText.Insert(0, "[Steamworks] ");

            if (nSeverity == 0)
            {
                Logger.Instance.Log(pchDebugText.ToString());
            }
            else
            {
                Logger.Instance.Warning(pchDebugText.ToString());
            }
        }

        public override bool Initialize()
        {
            if (Initialized)
            {
                return true;
            }

#if UNITY_EDITOR
            if (!Launcher.Data.Online)
            {
                Logger.Instance.Log("Starting Steam in offline mode");
                return true;
            }
#endif

            Logger.Instance.Log("Starting Steam in online mode");

            if (!Packsize.Test())
            {
                InitializationError.Show(InitializationError.ErrorType.Steamworks_Packsize);
                return false;
            }

            if (!DllCheck.Test())
            {
                InitializationError.Show(InitializationError.ErrorType.Steamworks_DllCheck);
                return false;
            }

            try
            {
                if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                InitializationError.Show(InitializationError.ErrorType.Steamworks_DllNotFound, e.Message, e.StackTrace);
                return false;
            }

            try
            {
                string appId = AppId.ToString();
                Environment.SetEnvironmentVariable("SteamAppId", appId);
                Environment.SetEnvironmentVariable("SteamOverlayGameId", appId);
                Environment.SetEnvironmentVariable("SteamGameId", appId);

                SteamAPI.Init();
                Initialized = true;
            }
            catch (Exception e)
            {
                InitializationError.Show(InitializationError.ErrorType.Steamworks_InitFailed, e.Message, e.StackTrace);
                return false;
            }

            if (_messageHook == null)
            {
                _messageHook = new SteamAPIWarningMessageHook_t(MessageHook);
                SteamClient.SetWarningMessageHook(_messageHook);
            }

            return true;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            if (_messageHook != null)
            {
                SteamClient.SetWarningMessageHook(null);
            }

            if (Initialized)
            {
                SteamAPI.Shutdown();
                Initialized = false;
            }
        }

        public override void Update()
        {
            if (Initialized)
            {
                SteamAPI.RunCallbacks();
            }
        }

        public string GetModsFolders()
        {
            if (!Initialized)
            {
                return string.Empty;
            }

            // In order to not strain Steamworks unnecessarily here, we determine root path for all mods
            // by requesting a single workshop mod and using its parent folder for further parsing

            uint subscribed = SteamUGC.GetNumSubscribedItems();

            if (subscribed == 0)
            {
                return string.Empty;
            }

            PublishedFileId_t[] items = new PublishedFileId_t[1];

            if (SteamUGC.GetSubscribedItems(items, 1) == 0)
            {
                return string.Empty;
            }

            if (!SteamUGC.GetItemInstallInfo(items[0], out ulong _, out string folder, 1024, out uint _))
            {
                return string.Empty;
            }

            return Path.GetDirectoryName(folder);
        }
    }
}
