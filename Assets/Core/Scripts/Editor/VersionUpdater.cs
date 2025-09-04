#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;
using UnityEngine;
using Utf8Json;

namespace Core.Editor
{
    public class VersionUpdater
    {
        public static void ProcessVersions(List<string> assets)
        {
            HashSet<string> modFolders = new();

            foreach (var file in assets)
            {
                string targetFile = file.Replace('\\', '/');

                if(targetFile.EndsWith("/ModManifest.json"))
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

                if (fileParts[0] != "Assets")
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
                string filePath = "Assets/" + mod + "/ModManifest.json";

                if (!File.Exists(filePath))
                {
                    continue;
                }

                ModManifestFile modManifest;

                try
                {
                    modManifest = JsonSerializer.Deserialize<ModManifestFile>(File.ReadAllText(filePath));
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    continue;
                }

                DateTime now = DateTime.Now;
                DateTime previous = DateTime.Parse(modManifest.BuildDate);
                TimeSpan delta = now - previous;

                if(delta.TotalSeconds <= 2.0)
                {
                    continue;
                }

                modManifest.BuildNumber++;
                modManifest.BuildDate = now.ToString("dd.MM.yyyy HH:mm:ss");

                File.WriteAllBytes(filePath, JsonSerializer.PrettyPrintByteArray(JsonSerializer.Serialize(modManifest)));
            }

            DateTime date = new(1970, 1, 1, 0, 0, 0, 0);
            date = date.AddSeconds(InternalEditorUtility.GetUnityVersionDate());

            string unityVersion = "Unity: " + Application.unityVersion + " Date: " + date.ToString("dd.MM.yyyy HH:mm:ss");

            File.WriteAllText("UnityVersion.txt", unityVersion);
        }
    }
}

#endif