using Steamworks;
using System.Collections.Generic;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    public class SteamUGCManager : System<SteamUGCManager>
    {
        // Hardcoded by Valve - https://partner.steamgames.com/doc/api/ISteamUGC#StartPlaytimeTracking
        // If we have more entries than this number - randomly pick MaxStartPlaybackCount entries from the list to provide playtime.
        private const int MaxStartPlaybackCount = 100;

        private readonly List<ModManifestFile> _steamEntries = new();

        private CallResult<StartPlaytimeTrackingResult_t> StartPlaytimeCallback;
        private CallResult<WorkshopEULAStatus_t> WorkshopEULACallback;

        public EventFunc<bool> EULAUpdate = new();

        private void OnStartPlaytime(StartPlaytimeTrackingResult_t data, bool failure)
        {

        }

        // TODO This is borked on the Steam / Steamworks.NET side for some reason
        // https://github.com/rlabrecque/Steamworks.NET/issues/538
        private void OnWorkshopEULA(WorkshopEULAStatus_t data, bool failure)
        {
            if (data.m_eResult != EResult.k_EResultOK || failure)
            {
                EULAUpdate.Publish(false);
                return;
            }

            EULAUpdate.Publish(data.m_bNeedsAction);
        }

        public override void Initialize()
        {
            if (!Steam.Initialized)
            {
                return;
            }

            Dictionary<string, ModManifestFile> manifests = ContentLoader.Instance.GetModManifestFiles();

            foreach (var manifest in manifests)
            {
                if (manifest.Value.PublishedFileId == default)
                {
                    continue;
                }

                _steamEntries.Add(manifest.Value);
            }

            if (_steamEntries.Count == 0)
            {
                return;
            }

            StartPlaytimeCallback = CallResult<StartPlaytimeTrackingResult_t>.Create(OnStartPlaytime);
            WorkshopEULACallback = CallResult<WorkshopEULAStatus_t>.Create(OnWorkshopEULA);

            WorkshopEULACallback.Set(SteamUGC.GetWorkshopEULAStatus());

            PublishedFileId_t[] fileIds;

            if (_steamEntries.Count <= MaxStartPlaybackCount)
            {
                fileIds = new PublishedFileId_t[_steamEntries.Count];

                for (int i = 0; i < _steamEntries.Count; i++)
                {
                    ModManifestFile entry = _steamEntries[i];
                    fileIds[i] = entry.PublishedFileId;
                }
            }
            else
            {
                fileIds = new PublishedFileId_t[MaxStartPlaybackCount];

                List<ModManifestFile> randomList = new(_steamEntries);

                int counter = 0;

                while (counter < MaxStartPlaybackCount)
                {
                    int index = Random.Range(0, randomList.Count);
                    fileIds[counter] = randomList[index].PublishedFileId;
                    randomList.RemoveAt(index);
                    counter++;
                }
            }

            StartPlaytimeCallback.Set(SteamUGC.StartPlaytimeTracking(fileIds, (uint)fileIds.Length));
        }

        // Result count gets clamped by k_nScreenshotMaxTaggedPublishedFiles
        // https://partner.steamgames.com/doc/api/ISteamScreenshots#constants
        public List<PublishedFileId_t> GetFileIdsForVisualTag()
        {
            List<PublishedFileId_t> result = new();

            if (!Steam.Initialized)
            {
                return result;
            }

            foreach (var entry in _steamEntries)
            {
                if (entry.PublishedFileId == default)
                {
                    continue;
                }

                result.Add(entry.PublishedFileId);
            }

            while (result.Count > Constants.k_nScreenshotMaxTaggedPublishedFiles)
            {
                int index = Random.Range(0, result.Count);
                result.RemoveAt(index);
            }

            return result;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            if (_steamEntries.Count == 0)
            {
                return;
            }

            StartPlaytimeCallback?.Dispose();
            StartPlaytimeCallback = null;
            WorkshopEULACallback?.Dispose();
            WorkshopEULACallback = null;
        }
    }
}
