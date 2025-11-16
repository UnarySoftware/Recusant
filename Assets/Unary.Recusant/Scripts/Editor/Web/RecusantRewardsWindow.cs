using Newtonsoft.Json;
using System;
using System.IO;
using Unary.Recusant;
using UnityEditor;
using UnityEngine;

public class RecusantRewardsWindow : EditorWindow
{
    public const string TargetFile = WebDataFetcher.RepoFolder + "/recusant_rewards.json";

    public RecusantRewardsDataEntry[] _entries = new RecusantRewardsDataEntry[0];
    public bool gotEntries = false;

    [MenuItem("Recusant Web/Rewards")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(RecusantRewardsWindow));
    }

    private void OnEnable()
    {
        gotEntries = false;

        if (Directory.Exists(WebDataFetcher.RepoFolder) && File.Exists(TargetFile))
        {
            Type type = typeof(RecusantRewardsDataEntry[]);

            try
            {
                _entries = (RecusantRewardsDataEntry[])JsonConvert.DeserializeObject(File.ReadAllText(TargetFile), type);
                gotEntries = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private void OnDisable()
    {
        Save(false);
    }

    private void Save(bool indented)
    {
        Formatting formatting = Formatting.None;

        if (indented)
        {
            formatting = Formatting.Indented;
        }

        if (Directory.Exists(WebDataFetcher.RepoFolder))
        {
            File.WriteAllText(TargetFile, JsonConvert.SerializeObject(_entries, formatting));
        }
    }

    void OnGUI()
    {
        if (!gotEntries)
        {
            GUILayout.Label($"Failed to fetch entries. Either we failed to deserialize or we are missing the {WebDataFetcher.RepoName} repository.");
            return;
        }

        SerializedObject serializedObject = new(this);
        SerializedProperty myDataProperty = serializedObject.FindProperty(nameof(_entries));

        EditorGUILayout.PropertyField(myDataProperty, true);

        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Save Performant"))
        {
            Save(false);
        }

        if (GUILayout.Button("Save Indented (for debug only)"))
        {
            Save(true);
        }
    }
}
