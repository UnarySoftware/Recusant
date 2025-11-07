#if UNITY_EDITOR

using Newtonsoft.Json;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Unary.Core;
using Unary.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Unary.Recusant.Editor
{
    public class SteamWorkshopUploader
    {
        private static SteamAPIWarningMessageHook_t _messageHook = null;

        [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
        private static void MessageHook(int severity, StringBuilder stringBuilder)
        {
            stringBuilder.Prepend("[Steamworks] ");

            if (severity == 0)
            {
                Debug.Log(stringBuilder.ToString());
            }
            else
            {
                Debug.LogWarning(stringBuilder.ToString());
            }
        }

        private static Dictionary<EResult, string> _createItemErrorLocales = new()
        {
            { EResult.k_EResultInsufficientPrivilege, "You are currently restricted from uploading content due to a hub ban, account lock, or community ban." },
            { EResult.k_EResultBanned, "You doesn't have permission to upload content to this hub because they have an active VAC or Game ban." },
            { EResult.k_EResultTimeout, "The operation took longer than expected. You have to retry the creation process." },
            { EResult.k_EResultNotLoggedOn, "You are not currently logged into Steam." },
            { EResult.k_EResultServiceUnavailable, "The workshop server hosting the content is having issues. Retry again later." },
            { EResult.k_EResultInvalidParam, "One of the submission fields contains something not being accepted by that field. " },
            { EResult.k_EResultAccessDenied, "There was a problem trying to save the title and description. Access was denied." },
            { EResult.k_EResultLimitExceeded, "You has exceeded your Steam Cloud quota. Remove some items and try again later." },
            { EResult.k_EResultFileNotFound, "The uploaded file could not be found." },
            { EResult.k_EResultDuplicateRequest, "The file was already successfully uploaded." },
            { EResult.k_EResultDuplicateName, "You already have a Steam Workshop item with this name." },
            { EResult.k_EResultServiceReadOnly, "Due to a recent password or email change, you are not allowed to upload new content.\n" +
                "Usually this restriction will expire in 5 days, but can last up to 30 days if the account has been inactive recently." }
        };

        private static Dictionary<EResult, string> _submitItemErrorLocales = new()
        {
            { EResult.k_EResultInvalidParam, "Either the provided app ID is invalid or doesn't match the consumer app ID of the item." },
            { EResult.k_EResultAccessDenied, "You dont own a license for the game." },
            { EResult.k_EResultFileNotFound, "Failed to get the workshop info for the item or failed to read the preview file." },
            { EResult.k_EResultLockingFailed, "Failed to aquire UGC Lock." },
            { EResult.k_EResultLimitExceeded, "The preview image is too large, it must be less than 1 Megabyte; or there is not enough space available on your Steam Cloud." }
        };

        private static bool _shouldQuit = false;
        private static Thread _updateThread;

        private static bool StartSteam()
        {
            if (!Steam.InitializeSteamWorks(false))
            {
                return false;
            }

            if (_messageHook == null)
            {
                _messageHook = new SteamAPIWarningMessageHook_t(MessageHook);
                SteamClient.SetWarningMessageHook(_messageHook);
            }

            if (_updateThread != null)
            {
                _shouldQuit = true;
                _updateThread.Join(1000);
            }

            _shouldQuit = false;

            _updateThread = new(UpdateSteam);
            _updateThread.Start();

            return true;
        }

        private static void OpenUploaded()
        {
            foreach (var file in _uploaded)
            {
                System.Diagnostics.Process.Start($"https://steamcommunity.com/sharedfiles/filedetails/?id={file.m_PublishedFileId}");
            }
            _uploaded.Clear();
        }

        private static void UpdateSteam()
        {
            while (!_shouldQuit)
            {
                SteamAPI.RunCallbacks();
                Thread.Sleep(16);

                if (_submitted)
                {
                    EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(_uploadingHandle, out ulong entryProcessed, out ulong entryTotal);

                    if (entryProcessed >= entryTotal)
                    {
                        ClearCurrentEntry();

                        _uploadingHandle = default;
                        _submitted = false;
                        _submittedMod = null;
                        _uploadingFile = default;

                        if (_progressId != -1)
                        {
                            Progress.Remove(_progressId);
                            _progressId = -1;
                        }

                        if (GotModEntries())
                        {
                            UploadMod(GetModEntry());
                        }
                        else
                        {
                            OpenUploaded();
                            StopSteam();
                        }
                    }
                    else
                    {
                        if (_progressId == -1)
                        {
                            _progressId = Progress.Start($"Uploading \"{_submittedMod}\" to Steam Workshop", options: Progress.Options.Indefinite);
                        }

                        Progress.Report(_progressId, (float)entryTotal / (float)entryProcessed);
                    }
                }
            }
        }

        private static bool StopSteam()
        {
            _uploadingHandle = default;
            _submitted = false;
            _submittedMod = null;
            _uploadingFile = default;

            if (_updateThread != null)
            {
                _shouldQuit = true;
                _updateThread.Join(1000);
            }

            if (_messageHook != null)
            {
                SteamClient.SetWarningMessageHook(null);
            }

            _shouldQuit = false;

            _updateThread = null;

            string modList = string.Empty;

            foreach (var mod in _selectedMods)
            {
                modList += $" \"{mod}\"";
            }

            DateTime endTime = DateTime.Now;
            TimeSpan elapsed = endTime - _startTime;

            Debug.Log("Finished uploading mods" + modList + " to Steam Workshop in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));

            OpenUploaded();

            return true;
        }

        private static ModManifestFile UpdateFileId(string modName, PublishedFileId_t newFileId)
        {
            if (modName == null)
            {
                return null;
            }

            string modManifestPath = $"Assets/{modName}/ModManifest.json";

            ModManifestFile modManifest;

            try
            {
                modManifest = JsonConvert.DeserializeObject<ModManifestFile>(File.ReadAllText(modManifestPath));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }

            modManifest.PublishedFileId = newFileId;

            File.WriteAllText(modManifestPath, JsonConvert.SerializeObject(modManifest, Formatting.Indented));

            return modManifest;
        }

        public static void UploadMod(string modName)
        {
            if (modName == null)
            {
                return;
            }

            string modManifestPath = $"Assets/{modName}/ModManifest.json";

            ModManifestFile modManifest;

            try
            {
                modManifest = JsonConvert.DeserializeObject<ModManifestFile>(File.ReadAllText(modManifestPath));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                StopSteam();
                return;
            }

            if (modManifest.PublishedFileId == default)
            {
                CreateItemCallback.Set(SteamUGC.CreateItem(new(Steam.AppId), EWorkshopFileType.k_EWorkshopFileTypeCommunity), OnCreatedItem);
            }
            else
            {
                UpdateMod(modManifest);
            }
        }

        private static void UpdateMod(ModManifestFile modManifest)
        {
            PublishedFileId_t fileId = modManifest.PublishedFileId;

            _uploaded.Add(fileId);

            if (!_contentManifests.TryGetValue(modManifest.ModId, out ContentManifest contentManifest))
            {
                Debug.LogError($"Failed to find ContentManifest for {modManifest.ModId}");
                StopSteam();
                return;
            }

            if (contentManifest == null)
            {
                StopSteam();
                return;
            }

            UGCUpdateHandle_t updateHandle = SteamUGC.StartItemUpdate(new(Steam.AppId), fileId);

            string absoluteDirectory = Directory.GetCurrentDirectory().Replace('\\', '/');

            if (!SteamUGC.SetItemContent(updateHandle, absoluteDirectory + "/Mods/" + modManifest.ModId))
            {
                Debug.LogError($"Failed to set content for a mod {modManifest.ModId}");
                StopSteam();
                return;
            }

            contentManifest.Precache();

            if (contentManifest.Preview.AssetPath == null)
            {
                Debug.LogError($"Failed to set preview for a mod {modManifest.ModId} since it had an empty {nameof(ContentManifest.Preview)} field in {nameof(ContentManifest)}");
                StopSteam();
                return;
            }

            string previewPath = absoluteDirectory + "/" + contentManifest.Preview.AssetPath;

            if (previewPath != null && File.Exists(previewPath) && !SteamUGC.SetItemPreview(updateHandle, previewPath))
            {
                Debug.LogError($"Failed to set preview for a mod {modManifest.ModId}");
                StopSteam();
                return;
            }

            if (!SteamUGC.SetItemTags(updateHandle, contentManifest.GetGameplayTags()))
            {
                Debug.LogError($"Failed to set title tags a mod {modManifest.ModId}");
                StopSteam();
                return;
            }

            _uploadingHandle = updateHandle;
            _uploadingFile = fileId;
            _submittedMod = contentManifest.FullName;
            SubmitItemUpdateCallback.Set(SteamUGC.SubmitItemUpdate(updateHandle, "Update"), OnSubmitItemUpdate);
        }

        private static void AcceptEULA()
        {
            MessageBox.Show("Steam Workshop EULA", "You must first accept terms and conditions of Steam Workshop EULA.\n" +
                    "A legal agreement letter will be opened in a browser for you to sign.\n" +
                    "After you will sign it - try uploading your mods again.");
            System.Diagnostics.Process.Start("https://steamcommunity.com/sharedfiles/workshoplegalagreement");
            StopSteam();
        }

        private static void OnSubmitItemUpdate(SubmitItemUpdateResult_t data, bool failure)
        {
            if (failure)
            {
                Debug.LogError($"Failed to submit a new Steam Workshop item update due to an unknown IO Steamworks error");
                StopSteam();
                return;
            }

            if (data.m_eResult != EResult.k_EResultOK)
            {
                if (!_submitItemErrorLocales.TryGetValue(data.m_eResult, out string error))
                {
                    error = "Generic error";
                }

                Debug.LogError($"Failed to submit a new Steam Workshop item update due to this error: {error}");
                StopSteam();
                return;
            }

            if (data.m_bUserNeedsToAcceptWorkshopLegalAgreement)
            {
                AcceptEULA();
                StopSteam();
                return;
            }

            _submitted = true;
        }

        private static void OnCreatedItem(CreateItemResult_t data, bool failure)
        {
            if (failure)
            {
                Debug.LogError($"Failed to create a new Steam Workshop item due to an unknown IO Steamworks error");
                StopSteam();
                return;
            }

            if (data.m_eResult != EResult.k_EResultOK)
            {
                if (!_createItemErrorLocales.TryGetValue(data.m_eResult, out string error))
                {
                    error = "Generic error";
                }

                Debug.LogError($"Failed to create a new Steam Workshop item due to this error: {error}");
                StopSteam();
                return;
            }

            _uploaded.Add(data.m_nPublishedFileId);

            ModManifestFile modManifest = UpdateFileId(GetModEntry(), data.m_nPublishedFileId);

            if (!_contentManifests.TryGetValue(modManifest.ModId, out ContentManifest contentManifest))
            {
                Debug.LogError($"Failed to find ContentManifest for {modManifest.ModId}");
                StopSteam();
                return;
            }

            if (contentManifest == null)
            {
                StopSteam();
                return;
            }

            if (data.m_bUserNeedsToAcceptWorkshopLegalAgreement)
            {
                AcceptEULA();
                StopSteam();
                return;
            }

            UGCUpdateHandle_t updateHandle = SteamUGC.StartItemUpdate(new(Steam.AppId), data.m_nPublishedFileId);

            if (!SteamUGC.SetItemTitle(updateHandle, contentManifest.FullName))
            {
                Debug.LogError($"Failed to set title for a mod {modManifest.ModId}");
                StopSteam();
                return;
            }

            if (!SteamUGC.SetItemDescription(updateHandle, contentManifest.FullDescription))
            {
                Debug.LogError($"Failed to set description for a mod {modManifest.ModId}");
                StopSteam();
                return;
            }

            if (!SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate))
            {
                Debug.LogError($"Failed to set visibility for a mod {modManifest.ModId}");
                StopSteam();
                return;
            }

            string absoluteDirectory = Directory.GetCurrentDirectory().Replace('\\', '/');

            if (!SteamUGC.SetItemContent(updateHandle, absoluteDirectory + "/Mods/" + modManifest.ModId))
            {
                Debug.LogError($"Failed to set content for a mod {modManifest.ModId}");
                StopSteam();
                return;
            }

            contentManifest.Precache();

            if (contentManifest.Preview.AssetPath == null)
            {
                Debug.LogError($"Failed to set preview for a mod {modManifest.ModId} since it had an empty {nameof(ContentManifest.Preview)} field in {nameof(ContentManifest)}");
                StopSteam();
                return;
            }

            string previewPath = absoluteDirectory + "/" + contentManifest.Preview.AssetPath;

            if (previewPath != null && File.Exists(previewPath) && !SteamUGC.SetItemPreview(updateHandle, previewPath))
            {
                Debug.LogError($"Failed to set preview for a mod {modManifest.ModId}");
                StopSteam();
                return;
            }

            if (!SteamUGC.SetItemTags(updateHandle, contentManifest.GetGameplayTags()))
            {
                Debug.LogError($"Failed to set title tags a mod {modManifest.ModId}");
                StopSteam();
                return;
            }

            _uploadingHandle = updateHandle;
            _uploadingFile = data.m_nPublishedFileId;
            _submittedMod = contentManifest.FullName;
            SubmitItemUpdateCallback.Set(SteamUGC.SubmitItemUpdate(updateHandle, "Update"), OnSubmitItemUpdate);
        }

        private static CallResult<WorkshopEULAStatus_t> WorkshopEULACallback = new();
        private static CallResult<CreateItemResult_t> CreateItemCallback = new();
        private static CallResult<SubmitItemUpdateResult_t> SubmitItemUpdateCallback = new();

        private static List<string> _selectedMods = new();
        private static Queue<string> _modQueue = new();

        private static bool GotModEntries()
        {
            return _modQueue.Count > 0;
        }

        private static string GetModEntry()
        {
            if (_modQueue.Count == 0)
            {
                return string.Empty;
            }

            return _modQueue.Peek();
        }

        private static void ClearCurrentEntry()
        {
            if (_modQueue.Count > 0)
            {
                _modQueue.Dequeue();
            }
        }

        private static UGCUpdateHandle_t _uploadingHandle;
        private static PublishedFileId_t _uploadingFile;
        private static bool _submitted = false;
        private static string _submittedMod = null;
        private static List<PublishedFileId_t> _uploaded = new();
        private static DateTime _startTime;
        private static int _progressId = -1;

        // TODO This is borked on the Steam / Steamworks.NET side for some reason
        // https://github.com/rlabrecque/Steamworks.NET/issues/538
        private static void OnEULAStatus(WorkshopEULAStatus_t data, bool failure)
        {
            /*
            if (!data.m_bAccepted)
            {
                AcceptEULA();
                StopSteam();
                return;
            }
            */

            UploadMod(GetModEntry());
        }

        private static Dictionary<string, ContentManifest> _contentManifests = new();

        [MenuItem("Assets/Recusant/Upload Mods To Steam")]
        public static void UploadModsToSteam()
        {
            _startTime = DateTime.Now;

            _modQueue.Clear();
            _uploadingHandle = default;
            _submitted = false;
            _submittedMod = null;
            _uploaded.Clear();
            _uploadingFile = default;
            _progressId = -1;

            _selectedMods.Clear();
            _contentManifests.Clear();

            string[] assets = AssetDatabase.GetAllAssetPaths();

            Type contentManifestType = typeof(ContentManifest);

            foreach (var asset in assets)
            {
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(asset);

                if (!contentManifestType.IsAssignableFrom(assetType))
                {
                    continue;
                }

                ContentManifest manifest = AssetDatabase.LoadAssetAtPath<ContentManifest>(asset);

                if (!manifest.Preview.AssetId.IsDefault())
                {
                    manifest.Preview.AssetPath = AssetDatabase.GUIDToAssetPath(manifest.Preview.AssetId.Value.ToUnity()).Replace('\\', '/');
                }

                _contentManifests[manifest.ModId] = manifest;
            }

            foreach (var targetObject in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(targetObject).Replace("\\", "/");

                if (!Directory.Exists(path))
                {
                    continue;
                }

                if (path.Count(c => c == '/') != 1)
                {
                    continue;
                }

                if (!File.Exists(path + "/ModManifest.json"))
                {
                    continue;
                }

                _selectedMods.Add(Path.GetFileName(path));
            }

            if (_selectedMods.Count == 0)
            {
                return;
            }

            EditorBuilding.SelectedModsForBuild = new(_selectedMods);
            EditorBuilding.BuildMods();

            if (!StartSteam())
            {
                Debug.LogError("Failed to start SteamWorks");
                return;
            }

            foreach (var mod in _selectedMods)
            {
                _modQueue.Enqueue(mod);
            }

            WorkshopEULACallback.Set(SteamUGC.GetWorkshopEULAStatus(), OnEULAStatus);
        }
    }
}

#endif
