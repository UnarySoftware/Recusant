#if UNITY_EDITOR

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;
using UnityEngine;

namespace Unary.Core.Editor
{
    public class VersionUpdater
    {
        public static void ProcessVersions(List<string> assets)
        {
            HashSet<string> modFolders = new();

            foreach (var file in assets)
            {
                string targetFile = file.Replace('\\', '/').ToLower();

                if (targetFile.EndsWith("/modmanifest.json"))
                {
                    continue;
                }

                if (!targetFile.Contains('/'))
                {
                    continue;
                }

                string[] fileParts = targetFile.Split('/');

                if (fileParts.Length < 2)
                {
                    continue;
                }

                if (fileParts[0].ToLower() != "assets")
                {
                    continue;
                }

                modFolders.Add(fileParts[1]);
            }

            if (modFolders.Count == 0)
            {
                return;
            }

            foreach (var mod in modFolders)
            {
                string filePath = "assets/" + mod + "/modmanifest.json";

                if (!File.Exists(filePath))
                {
                    continue;
                }

                ModManifestFile modManifest;

                try
                {
                    modManifest = JsonConvert.DeserializeObject<ModManifestFile>(File.ReadAllText(filePath));
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    continue;
                }

                DateTime now = DateTime.Now;
                DateTime previous = DateTime.Parse(modManifest.BuildDate);
                TimeSpan delta = now - previous;

                if (delta.TotalSeconds <= 2.0)
                {
                    continue;
                }

                modManifest.BuildNumber++;
                modManifest.BuildDate = now.ToString("dd.MM.yyyy HH:mm:ss");

                File.WriteAllText(filePath, JsonConvert.SerializeObject(modManifest, Formatting.Indented));
            }

            DateTime date = new(1970, 1, 1, 0, 0, 0, 0);
            date = date.AddSeconds(InternalEditorUtility.GetUnityVersionDate());

            string unityVersion = "Unity: " + Application.unityVersion + " Date: " + date.ToString("dd.MM.yyyy HH:mm:ss");

            File.WriteAllText("UnityVersion.txt", unityVersion);
        }
    }
}

#endif