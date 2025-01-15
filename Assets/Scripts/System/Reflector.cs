using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

public class Reflector : MonoBehaviour, ISystem
{
    public static Reflector Instance = null;

    public struct GameplayCommandInfo
    {
        public GameplayCommandAttribute Command;
        public GameplayCommandRange[] Arguments;
        public MethodInfo Method;
    }

    public struct GameplayUnit
    {
        public bool IsVariable;
        public AbstractVariable Variable;
        public GameplayCommandInfo Command;
    }

    private readonly Dictionary<string, GameplayUnit> _gameplayUnits = new();
    public Dictionary<string, GameplayUnit> GameplayUnits { get { return _gameplayUnits; } }

#pragma warning disable IDE0051

    [GameplayCommand(GameplayGroup.Client, GameplayFlag.None, "Test description")]
    private static void Test010Cmd()
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandIgnore()]
    private static void Test1Cmd()
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    private static void Test2Cmd()
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    [GameplayCommandIgnore()]
    private static void Test3Cmd()
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandIgnore()]
    private static void Test4Cmd(float a)
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    private static void Test5Cmd(float a)
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    private static void Test6Cmd(float a, float b, float c)
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandIgnore]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    private static void Test7Cmd(float a, float b, float c)
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    private static void Test8Cmd(float a, float b, float c)
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    private static void Test9Cmd(float a, bool b, float c)
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    private static void Test10Cmd(float a, Color b, float c)
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandShort(0, 1, 2)]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    private static void Test11Cmd(float a, short b, float c)
    {

    }

    [GameplayCommand(GameplayGroup.Server, GameplayFlag.None, "Test description")]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    [GameplayCommandShort(0, 1, 2)]
    [GameplayCommandFloat(0.0f, 1.0f, 2.0f)]
    private static void Test12Cmd(float a, Color b, float c)
    {

    }

#pragma warning restore IDE0051

    [InitDependency()]
    public void Initialize()
    {
        Type[] types = Bootstrap.GetTypeStatic().Assembly.GetTypes();

        foreach (Type type in types)
        {
            if (!type.IsClass)
            {
                continue;
            }

            var Fields = type.GetFields();

            foreach (var Field in Fields)
            {
                if (Field.IsStatic && Field.IsPublic)
                {
                    if (Field.FieldType.IsGenericType &&
                        (Field.FieldType.GetGenericTypeDefinition() == typeof(GameplayVariable<>) ||
                        Field.FieldType.GetGenericTypeDefinition() == typeof(GameplayVariableRanged<,>)))
                    {
                        AbstractVariable variableValue = (AbstractVariable)Field.GetValue(null);

                        if (variableValue.GetGroup() != GameplayGroup.None)
                        {
                            string gameplayName = variableValue.GetGroup() + "." + type.FullName + "." + Field.Name;
                            variableValue.Name = gameplayName;

                            _gameplayUnits[gameplayName] = new GameplayUnit()
                            {
                                IsVariable = true,
                                Variable = variableValue
                            };
                        }
                    }
                }
            }

            MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);

            foreach (MethodInfo method in methods)
            {
                if (!method.Name.EndsWith("Cmd"))
                {
                    continue;
                }

                var attributes = method.GetCustomAttributes();

                GameplayCommandInfo info = new();

                List<GameplayCommandRange> ranges = new();

                int attributeIndex = 0;

                foreach (Attribute attribute in attributes)
                {
                    if (attributeIndex == 0 && attribute is GameplayCommandAttribute commandAttribute)
                    {
                        if (commandAttribute.Group != GameplayGroup.None)
                        {
                            info.Command = commandAttribute;
                            info.Method = method;
                        }
                    }
                    else if (attributeIndex != 0 && attribute is GameplayCommandRange commandRange)
                    {
                        ranges.Add(commandRange);
                    }
                    attributeIndex++;
                }

                info.Arguments = ranges.ToArray();

                ParameterInfo[] parameters = method.GetParameters();

                if (info.Arguments.Length != parameters.Length)
                {
                    continue;
                }

                bool validRanges = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (info.Arguments[i].Type != GameplayType.None &&
                        !GameplayShared.ValidateRangeForType(parameters[i].ParameterType, info.Arguments[i].Type))
                    {
                        validRanges = false;
                        break;
                    }
                }

                if (validRanges && info.Command != null && info.Method != null)
                {
                    _gameplayUnits[info.Command.Group + "." + type.FullName + "." + method.Name[..^3]] = new GameplayUnit()
                    {
                        IsVariable = false,
                        Command = info
                    };
                }
            }
        }
    }

    public void Deinitialize()
    {

    }
}
