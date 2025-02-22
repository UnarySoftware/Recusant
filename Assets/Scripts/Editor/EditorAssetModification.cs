#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class EditorAssetModification : AssetModificationProcessor
{
    public static int DelayMs { get; private set; } = 1000;

    private const string AssetPath = "Assets/Recusant/ScriptableObjects/RegistryList.asset";
    private const string AssetsFolder = "Assets/Recusant/ScriptableObjects";

    public static async void ProcessRegistryList()
    {
        await Task.Run(() => Thread.Sleep(DelayMs));

        if(!AssetDatabase.AssetPathExists(AssetPath))
        {
            RegistryList asset = ScriptableObject.CreateInstance<RegistryList>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
        }

        RegistryList List = AssetDatabase.LoadAssetAtPath<RegistryList>(AssetPath);

        List<string> Files = Directory.GetFiles(AssetsFolder, "*.*", SearchOption.AllDirectories).ToList();

        Files.Sort();

        List<string> ProcessedList = new();

        foreach (string TargetFile in Files)
        {
            if(TargetFile.EndsWith(".meta"))
            {
                continue;
            }

            ProcessedList.Add(TargetFile.Replace("\\", "/"));
        }

        List<BaseScriptableObject> NewEntries = new();

        foreach(var File in ProcessedList)
        {
            ScriptableObject TargetObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(File);

            if(TargetObject is RegistryList)
            {
                continue;
            }

            if(TargetObject == null)
            {
                Debug.LogError(File + " is not of ScriptableObject type!");
                continue;
            }

            if(TargetObject is BaseScriptableObject TargetScriptable)
            {
                TargetScriptable.UniqueId = new System.Guid(AssetDatabase.GUIDFromAssetPath(File).ToString());
                EditorUtility.SetDirty(TargetScriptable);
                NewEntries.Add(TargetScriptable);
            }
        }

        List.Entries = NewEntries.ToArray();

        EditorUtility.SetDirty(List);
        AssetDatabase.SaveAssets();
    }

    private static void ProcessPath(string Path)
    {
        Path = Path.Replace("\\", "/");
        if(Path.Contains("Assets/Recusant/ScriptableObjects"))
        {
            ProcessRegistryList();
        }
    }

    public static void OnWillCreateAsset(string AssetName)
    {
        ProcessPath(AssetName);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060")]
    public static AssetDeleteResult OnWillDeleteAsset(string AssetName, RemoveAssetOptions Options)
    {
        ProcessPath(AssetName);
        return AssetDeleteResult.DidNotDelete;
    }

    public static AssetMoveResult OnWillMoveAsset(string SourcePath, string DestinationPath)
    {
        ProcessPath(SourcePath);
        ProcessPath(DestinationPath);
        return AssetMoveResult.DidNotMove;
    }
}

#endif
