#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unary.Core.Editor
{
    public class ShaderVariantManager
    {
        private const string ReaderCollectionPath = "Assets/URP/ReaderCollection.asset";
        private const string EditorCollectionPath = "Assets/URP/EditorCollection.asset";

        private static string Beginning = string.Empty;

        [MenuItem("Core/Shaders Append")]
        public static void ShadersAppend()
        {
            List<ShaderVariantChanges> changes = BuildShaderVariants();

            if (changes.Count == 0)
            {
                Debug.Log("Shader collections are up to date");
            }
            else
            {
                string shaderResult = "Shader collections build results:\n";
                foreach (var mod in changes)
                {
                    shaderResult += $" Changed mod \"{mod.ModId}\":\n";

                    if (mod.OldShaderCount != mod.NewShaderCount)
                    {
                        shaderResult += $"  Shader count: {mod.OldShaderCount} => {mod.NewShaderCount}\n";
                    }

                    if (mod.OldVariantCount != mod.NewVariantCount)
                    {
                        shaderResult += $"  Variant count: {mod.OldVariantCount} => {mod.NewVariantCount}\n";
                    }
                }
                Debug.Log(shaderResult);
            }
        }

        [MenuItem("Core/Shaders Clear")]
        public static void ShadersClear()
        {
            string[] directories = Directory.GetDirectories("Assets");

            bool changed = false;

            string shaderResult = "Shader collections clear results:\n";

            foreach (var directory in directories)
            {
                string directoryPath = directory.Replace("\\", "/");

                if (!File.Exists(directoryPath + "/ModManifest.json"))
                {
                    continue;
                }

                string[] parts = directoryPath.Split('/');

                if (parts.Length < 2)
                {
                    continue;
                }

                string modId = parts[1];
                string collectionPath = "Assets/" + modId + "/Shaders/" + modId + ".shadervariants";
                string infoPath = "Assets/" + modId + "/Shaders/" + modId + ".asset";

                if (AssetDatabase.AssetPathExists(collectionPath))
                {
                    AssetDatabase.DeleteAsset(collectionPath);
                    changed = true;
                    shaderResult += $"\"{modId}\" cleared its ShaderVariantCollection\n";
                }

                if (AssetDatabase.AssetPathExists(infoPath))
                {
                    AssetDatabase.DeleteAsset(infoPath);
                    changed = true;
                    shaderResult += $"\"{modId}\" cleared its ReadableShaderVariantCollection\n";
                }
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(shaderResult);
            }
            else
            {
                Debug.Log("Nothing shader-related was cleaned up, everything is already gone");
            }
        }

        private static void Initialize()
        {
            if (!AssetDatabase.AssetPathExists(ReaderCollectionPath))
            {
                ReadableShaderVariantCollection svc = new();
                AssetDatabase.CreateAsset(svc, ReaderCollectionPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            string[] readerLines = File.ReadAllLines(ReaderCollectionPath);

            string result = string.Empty;

            foreach (var line in readerLines)
            {
                if (line.Contains("m_Shaders:"))
                {
                    break;
                }

                result += line + '\n';
            }

            Beginning = result;
        }

        private static string GetEnding(string path)
        {
            string[] readerLines = File.ReadAllLines(path);

            string result = string.Empty;

            bool gotEnding = false;

            foreach (var line in readerLines)
            {
                if (line.Contains("m_Shaders:"))
                {
                    gotEnding = true;
                }

                if (gotEnding)
                {
                    result += line + '\n';
                }
            }

            return result;
        }

        private static ReadableShaderVariantCollection ReadShaderVariantCollection(string path)
        {
            File.WriteAllText(ReaderCollectionPath, Beginning + GetEnding(path));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<ReadableShaderVariantCollection>(ReaderCollectionPath);
        }

        private static ReadableShaderVariantCollection BuildEditorShaderCollection()
        {
            if (AssetDatabase.AssetPathExists(EditorCollectionPath))
            {
                AssetDatabase.DeleteAsset(EditorCollectionPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Type shaderUtilType = typeof(ShaderUtil);
            if (shaderUtilType == null)
            {
                Debug.LogError("Could not find internal ShaderUtil class.");
                return null;
            }

            MethodInfo saveMethod = shaderUtilType.GetMethod(
                "SaveCurrentShaderVariantCollection",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (saveMethod == null)
            {
                Debug.LogError("Could not find internal method SaveCurrentShaderVariantCollection.");
                return null;
            }

            saveMethod.Invoke(null, new object[] { EditorCollectionPath });

            ReadableShaderVariantCollection collection = ReadShaderVariantCollection(EditorCollectionPath);

            AssetDatabase.DeleteAsset(EditorCollectionPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return collection;
        }

        public struct ShaderVariantChanges
        {
            public string ModId;

            public int OldShaderCount;
            public int OldVariantCount;

            public int NewShaderCount;
            public int NewVariantCount;
        }

        public static List<ShaderVariantChanges> BuildShaderVariants()
        {
            Initialize();

            ReadableShaderVariantCollection collection = BuildEditorShaderCollection();

            List<ShaderVariantCollection.ShaderVariant> variantsList = new();

            foreach (var entry in collection.m_Shaders)
            {
                foreach (var variant in entry.second.variants)
                {
                    variantsList.Add(new()
                    {
                        shader = entry.first,
                        keywords = variant.keywords.Split(' '),
                        passType = variant.passType,
                    });
                }
            }

            Dictionary<string, List<ShaderVariantCollection.ShaderVariant>> variantsSorted = new();

            foreach (var entry in variantsList)
            {
                string shaderPath = AssetDatabase.GetAssetPath(entry.shader).ToLower();
                string modId = "Unary.Core";

                shaderPath = shaderPath.Replace('\\', '/');

                if (shaderPath.Count(c => c == '/') > 2)
                {
                    string[] parts = shaderPath.Split('/');

                    if (parts[0] == "Assets" && File.Exists("Assets/" + parts[1] + "/ModManifest.json"))
                    {
                        modId = parts[1];
                    }
                }

                if (!variantsSorted.TryGetValue(modId, out var entryList))
                {
                    entryList = new();
                    variantsSorted[modId] = entryList;
                }

                entryList.Add(entry);
            }

            List<ShaderVariantChanges> result = new();

            foreach (var variantSet in variantsSorted)
            {
                string collectionPath = "Assets/" + variantSet.Key + "/Shaders/" + variantSet.Key + ".shadervariants";
                string infoPath = "Assets/" + variantSet.Key + "/Shaders/" + variantSet.Key + ".asset";

                if (!Directory.Exists(Path.GetDirectoryName(collectionPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(collectionPath));
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                if (!AssetDatabase.AssetPathExists(collectionPath))
                {
                    ShaderVariantCollection newCollection = new();
                    AssetDatabase.CreateAsset(newCollection, collectionPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                if (!AssetDatabase.AssetPathExists(infoPath))
                {
                    var newInfo = ScriptableObject.CreateInstance<ShaderVariantCollectionInfo>();
                    AssetDatabase.CreateAsset(newInfo, infoPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                int oldShaderCount = 0;
                int oldVariantCount = 0;

                int newShaderCount = 0;
                int newVariantCount = 0;

                ReadableShaderVariantCollection oldCounter = ReadShaderVariantCollection(collectionPath);

                foreach (var counter in oldCounter.m_Shaders)
                {
                    oldShaderCount++;

                    foreach (var entry in counter.second.variants)
                    {
                        oldVariantCount++;
                    }
                }

                ShaderVariantCollection targetCollection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(collectionPath);

                foreach (var variant in variantSet.Value)
                {
                    if (!targetCollection.Contains(variant))
                    {
                        targetCollection.Add(variant);
                    }
                }

                EditorUtility.SetDirty(targetCollection);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                ReadableShaderVariantCollection updatedCounter = ReadShaderVariantCollection(collectionPath);

                List<SerializableShaderVariant> readableEntries = new();

                foreach (var counter in updatedCounter.m_Shaders)
                {
                    newShaderCount++;

                    foreach (var entry in counter.second.variants)
                    {
                        newVariantCount++;

                        string assetPath = AssetDatabase.GetAssetPath(counter.first);

                        GUID guid = AssetDatabase.GUIDFromAssetPath(assetPath);

                        SerializableShaderVariant newReadableVariant = new()
                        {
                            shader = counter.first,
                            shaderGuid = guid.ToSystem(),
                            passType = entry.passType,
                            keywords = entry.keywords.Split(' ')
                        };

                        readableEntries.Add(newReadableVariant);
                    }
                }

                var info = AssetDatabase.LoadAssetAtPath<ShaderVariantCollectionInfo>(infoPath);
                info.Collection = targetCollection;
                info.Entries = readableEntries.ToArray();

                EditorUtility.SetDirty(info);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (oldShaderCount < newShaderCount ||
                    oldVariantCount < newVariantCount)
                {
                    result.Add(new()
                    {
                        ModId = variantSet.Key,
                        OldShaderCount = oldShaderCount,
                        OldVariantCount = oldVariantCount,
                        NewShaderCount = newShaderCount,
                        NewVariantCount = newVariantCount
                    });
                }
            }

            return result;
        }
    }
}

#endif
