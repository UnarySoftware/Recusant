#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

class SharedCodeGeneration
{
    public Type[] Types { get; private set; } = null;

    public Dictionary<Type, List<FieldInfo>> GameplayVariables { get; private set; } = new();

    public void FindTypes()
    {
        Types = Bootstrap.GetTypeStatic().Assembly.GetTypes();
    }

    public void FindGameplayVariables()
    {
        GameplayVariables.Clear();

        foreach (var type in Types)
        {
            if (type.IsClass)
            {
                var Fields = type.GetFields();

                foreach (var Field in Fields)
                {
                    if (Field.IsStatic && Field.IsPublic)
                    {
                        if (Field.FieldType.IsGenericType &&
                            (Field.FieldType.GetGenericTypeDefinition() == typeof(GameplayVariable<>) ||
                            Field.FieldType.GetGenericTypeDefinition() == typeof(GameplayVariableRanged<,>)))
                        {
                            if (!GameplayVariables.ContainsKey(type))
                            {
                                GameplayVariables[type] = new();
                            }

                            GameplayVariables[type].Add(Field);
                        }
                    }
                }
            }
        }
    }
}

abstract class CodeGenerator
{
    protected SharedCodeGeneration Shared;
    protected string Writer;
    private readonly string _path;

    public CodeGenerator(SharedCodeGeneration shared, string path)
    {
        Shared = shared;
        _path = path;
    }

    public abstract void Write();

    public void Finish()
    {
        File.WriteAllText(_path, Writer);
    }
}

class GameCodeGeneration : CodeGenerator
{
    public GameCodeGeneration(SharedCodeGeneration shared, string path) : base(shared, path)
    {
    }

    public void WriteHeader()
    {
        Writer += "//========= THIS FILE IS REGENERATED ON EACH SCRIPT REBUILD, DO NOT EDIT IT BY HAND =========\n\n";
        Writer += "public static class CodeGenerated\n{";
    }

    public void WriteConsts()
    {
        int GameplayVariableCount = 0;

        foreach (var variables in Shared.GameplayVariables)
        {
            foreach (var variable in variables.Value)
            {
                AbstractVariable targetVariable = (AbstractVariable)variable.GetValue(null);

                if (targetVariable.GetGroup() == GameplayGroup.Server &&
                    targetVariable.GetFlags().HasFlag(GameplayFlag.Replicated))
                {
                    GameplayVariableCount++;
                }
            }
        }

        Writer += "\n\t// Counter of replicated networked gameplay variables for Netick's fixed allocation";
        Writer += "\n\tpublic const int GameplayVariableMaxCount = " + GameplayVariableCount + ";\n";
    }

    public void WriteLayerMasks()
    {
        Writer += "\n\t// Enum for code reference of game object layers\n";
        Writer += "\tpublic enum GameObjectLayerMask : int\n\t{\n";

        for (int i = 0; i < 32; i++)
        {
            string name = LayerMask.LayerToName(i);

            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            name = name.Replace(" ", "");

            Writer += "\t\t" + name + " = " + i + ",\n";
        }

        Writer += "\t}\n";
    }

    public void WriteEnding()
    {
        Writer += "}\n";
    }

    public override void Write()
    {
        WriteHeader();
        WriteConsts();
        WriteLayerMasks();
        WriteEnding();
    }
}

class EditorCodeGeneration : CodeGenerator
{
    public EditorCodeGeneration(SharedCodeGeneration shared, string path) : base(shared, path)
    {
    }

    public void WriteHeader()
    {
        Writer += "//========= THIS FILE IS REGENERATED ON EACH SCRIPT REBUILD, DO NOT EDIT IT BY HAND =========\n\n";
        Writer += "#if UNITY_EDITOR\n";
        Writer += "using UnityEditor;\n";
    }

    public void WriteGameplayVariableEditors()
    {
        Writer += "\n// Custom gameplay variable editors\n";

        foreach (var variables in Shared.GameplayVariables)
        {
            Writer += "\n[CustomEditor(typeof(" + variables.Key.Name + "))]";
            Writer += "\npublic class " + variables.Key.Name + "_GeneratedEditor : BaseGameplayVariablesEditor<" + variables.Key.Name + ">\n{";
            Writer += "\n\tprotected override void OnEnable()\n\t{\n\t\tFieldNames = new string[]\n\t\t{";

            foreach (var variable in variables.Value)
            {
                Writer += "\n\t\t\t\"" + variable.Name + "\",";
            }

            Writer += "\n\t\t};\n\t\tbase.OnEnable();\n\t}\n}\n";
        }
    }

    public void WriteEnding()
    {
        Writer += "\n#endif\n";
    }

    public override void Write()
    {
        WriteHeader();
        WriteGameplayVariableEditors();
        WriteEnding();
    }
}

class CodeGeneration : AssetPostprocessor
{
    private static SharedCodeGeneration Shared = null;
    private static GameCodeGeneration Game = null;
    private static EditorCodeGeneration Editor = null;

    protected static void OnPostprocessAllAssets(string[] importedAssets,
        string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if(Shared == null)
        {
            Shared = new SharedCodeGeneration();
            Shared.FindTypes();
            Shared.FindGameplayVariables();
        }

        if (Game == null)
        {
            Game = new GameCodeGeneration(Shared, Application.dataPath + "/Scripts/Game.Gen.cs");
            Game.Write();
            Game.Finish();
        }

        if (Editor == null)
        {
            Editor = new EditorCodeGeneration(Shared, Application.dataPath + "/Scripts/Editor/Editor.Gen.cs");
            Editor.Write();
            Editor.Finish();
        }
    }
}

#endif
