using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Core
{
    public class Steam : CoreSystem<Steam>
    {
        public const uint AppId = 1436420;

        public static bool Initialized { get; private set; } = false;

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
                SteamAPI.Init(new(AppId));
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
            }
        }

        public override void Update()
        {
            if (Initialized)
            {
                SteamAPI.RunCallbacks();
            }
        }

        // TODO Finalize
        public IReadOnlyList<string> GetModsFolders()
        {
            List<string> result = new();

#if UNITY_EDITOR
            if (!Launcher.Data.Online)
            {
                return result;
            }
#endif

            uint subscribed = SteamUGC.GetNumSubscribedItems(false);

            PublishedFileId_t[] items = new PublishedFileId_t[subscribed];

            if (SteamUGC.GetSubscribedItems(items, subscribed, false) == 0)
            {
                return result;
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (!SteamUGC.GetItemInstallInfo(items[i], out ulong size, out string folder, 512, out uint timestamp))
                {
                    continue;
                }

                Logger.Instance.Log($"Found a mod at {folder}");
            }

            return result;
        }
    }
}
