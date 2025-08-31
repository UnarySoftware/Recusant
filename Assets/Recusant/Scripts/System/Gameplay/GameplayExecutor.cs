using Core;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using Netick;
using Utf8Json;

namespace Recusant
{
    public class GameplayExecutorShared : SystemShared
    {
        public struct GameplayCommandInfo
        {
            public GameplayCommandAttribute Command;
            public GameplayCommandRange[] Ranges;
            public MethodInfo Method;
            public Type SystemType;
        }

        public struct GameplayUnit
        {
            public bool IsVariable;
            public AbstractVariable Variable;
            public GameplayCommandInfo Command;
        }

        public readonly Dictionary<string, GameplayUnit> GameplayUnits = new();
    }

    public class GameplayExecutor : SystemNetworkRoot<GameplayExecutor, GameplayExecutorShared>
    {
        public Dictionary<string, GameplayExecutorShared.GameplayUnit> GameplayUnits { get { return SharedData.GameplayUnits; } }

        public void Execute(string line)
        {
            string[] parts;
            bool hasValue;

            if (line.Contains(' '))
            {
                parts = line.Split(new[] { ' ' }, 2);
                hasValue = true;
            }
            else
            {
                parts = new string[1] { line };
                hasValue = false;
            }

            string key = parts[0];

            GameplayExecutorShared.GameplayUnit unit;

            if (!SharedData.GameplayUnits.TryGetValue(key, out unit))
            {
                Core.Logger.Instance.Error("Invalid variable/command: " + key);
                return;
            }

            if (GameplayExecutorNetwork.Instance != null && GameplayExecutorNetwork.Instance.IsClient)
            {
                if (unit.IsVariable)
                {
                    if (unit.Variable.GetGroup() == GameplayGroup.Server)
                    {
                        if (unit.Variable.GetFlags().HasFlag(GameplayFlag.Replicated))
                        {
                            GameplayExecutorNetwork.Instance.SendCmd(line);
                            return;
                        }
                        else
                        {
                            Core.Logger.Instance.Error("Cant change \"" + key + "\" variable since it is in a Server group but cant be replicated");
                            return;
                        }
                    }
                }
                else
                {
                    if (unit.Command.Command.Group == GameplayGroup.Server)
                    {
                        if (unit.Command.Command.Flags.HasFlag(GameplayFlag.Replicated))
                        {
                            GameplayExecutorNetwork.Instance.SendCmd(line);
                            return;
                        }
                        else
                        {
                            Core.Logger.Instance.Error("Cant call \"" + key + "\" command since it is in a Server group but cant be replicated");
                            return;
                        }
                    }
                }
            }

            string value;
            object node = null;

            if (hasValue)
            {
                value = parts[1];

                if (!unit.IsVariable)
                {
                    value = "[ " + value + " ]";
                }

                try
                {
                    node = JsonSerializer.Deserialize<object>(value);
                }
                catch (Exception e)
                {
                    Core.Logger.Instance.Error("Failed to parse variable/command value: " + e.Message);
                    return;
                }
            }

            if (unit.IsVariable)
            {
                if (hasValue)
                {
                    object result = GameplayShared.GetValueFromNode(unit.Variable.GetTypeEnum(), unit.Variable.GetTypeSystem(), node);
                    unit.Variable.SetObject(result);
                }
                else
                {
                    Core.Logger.Instance.Error("Failed to parse variable/command value: " + key);
                    return;
                }
            }
            else
            {
                List<GameplayShared.GameplayCommandArgument> arguments = GameplayShared.GetArgumentTypes(unit.Command.Method);

                int count = 1;
                bool valid = true;

                foreach (var argument in arguments)
                {
                    if (argument.gameplayType == GameplayType.None)
                    {
                        Core.Logger.Instance.Error("Invalid argument #" + count + " in command \"" + key + "\"");
                        valid = false;
                    }
                    count++;
                }

                if (!valid)
                {
                    return;
                }

                object[] callingParams = null;

                if (hasValue)
                {
                    if (node is not List<object>)
                    {
                        Core.Logger.Instance.Error("Failed parsing arguments for command \"" + key + "\"");
                        return;
                    }

                    List<object> array = (List<object>)node;

                    if (array.Count != arguments.Count)
                    {
                        Core.Logger.Instance.Error("Argument count mismatch for command \"" + key + "\"");
                        return;
                    }

                    callingParams = new object[arguments.Count];

                    for (int i = 0; i < arguments.Count; i++)
                    {
                        GameplayCommandRange range = null;

                        if (unit.Command.Ranges[i] is not GameplayCommandIgnore)
                        {
                            range = unit.Command.Ranges[i];
                        }

                        object result;

                        if (range == null)
                        {
                            result = GameplayShared.GetValueFromNode(arguments[i].gameplayType, arguments[i].systemType, array[i]);
                        }
                        else
                        {
                            result = GameplayShared.GetValueFromNode(arguments[i].gameplayType, arguments[i].systemType, array[i], range.DefaultValue);
                            result = GameplayShared.ClampWithRanges(result, arguments[i].gameplayType, range.Min, range.Max);
                        }

                        callingParams[i] = result;
                    }
                }

                SystemBasic targetSystem = (SystemBasic)Systems.Instance.GetSystem(unit.Command.SystemType);

                if (targetSystem == null)
                {
                    Core.Logger.Instance.Error("Failed to find system of type \"" + unit.Command.SystemType.Name + "\" for command \"" + key + "\"");
                    return;
                }

                if (hasValue)
                {
                    unit.Command.Method.Invoke(targetSystem, callingParams);
                }
                else
                {
                    unit.Command.Method.Invoke(targetSystem, null);
                }
            }
        }

        public void RecieveResult(NetworkArrayStruct4<Core.Logger.LogType> types, NetworkArrayStruct4<NetworkString128> lines, NetworkArrayStruct4<NetworkString256> stackTraces)
        {
            for (int i = 0; i < types.Length; i++)
            {
                Core.Logger.LogType type = types[i];
                string line = lines[i];
                string stackTrace = stackTraces[i];

                if (type == Core.Logger.LogType.None || string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                switch (type)
                {
                    default:
                    case Core.Logger.LogType.None:
                        {
                            break;
                        }
                    case Core.Logger.LogType.Log:
                        {
                            Core.Logger.Instance.Log(line);
                            break;
                        }
                    case Core.Logger.LogType.Warning:
                        {
                            Core.Logger.Instance.SetCustomTrace(stackTrace);
                            Core.Logger.Instance.Warning(line);
                            break;
                        }
                    case Core.Logger.LogType.Error:
                        {
                            Core.Logger.Instance.SetCustomTrace(stackTrace);
                            Core.Logger.Instance.Error(line);
                            break;
                        }
                }
            }
        }

        public override void Initialize()
        {
            List<Type> types = new();

            foreach (var assembly in ContentLoader.Instance.GetLoadedAssemblies())
            {
                Type[] assemblyTypes = assembly.GetTypes();

                foreach (var assemblyType in assemblyTypes)
                {
                    types.Add(assemblyType);
                }
            }

            foreach (Type type in types)
            {
                if (!type.IsClass)
                {
                    continue;
                }

                var Fields = type.GetFields();

                foreach (var Field in Fields)
                {
                    if (!Field.IsStatic || !Field.IsPublic || !Field.FieldType.IsGenericType)
                    {
                        continue;
                    }

                    if (Field.FieldType.GetGenericTypeDefinition() != typeof(GameplayVariable<>) &&
                        Field.FieldType.GetGenericTypeDefinition() != typeof(GameplayVariableRanged<,>))
                    {
                        continue;
                    }

                    AbstractVariable variableValue = (AbstractVariable)Field.GetValue(null);

                    if (variableValue.GetGroup() == GameplayGroup.None)
                    {
                        continue;
                    }

                    // Due to a static nature of gameplay variables and preserved state
                    // between play sessions in the editor we need to reset them all here
                    variableValue.ResetToOriginal();

                    string gameplayName = variableValue.GetGroup() + "." + type.FullName + "." + Field.Name;
                    variableValue.Name = gameplayName;

                    SharedData.GameplayUnits[gameplayName] = new GameplayExecutorShared.GameplayUnit()
                    {
                        IsVariable = true,
                        Variable = variableValue
                    };
                }

                if (!typeof(SystemBasic).IsAssignableFrom(type))
                {
                    continue;
                }

                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                foreach (MethodInfo method in methods)
                {
                    if (method.ReturnType != typeof(void))
                    {
                        continue;
                    }

                    var attributes = method.GetCustomAttributes();

                    GameplayExecutorShared.GameplayCommandInfo info = new();

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
                                info.SystemType = type;
                            }
                        }
                        else if (attributeIndex != 0 && attribute is GameplayCommandRange commandRange)
                        {
                            ranges.Add(commandRange);
                        }
                        attributeIndex++;
                    }

                    info.Ranges = ranges.ToArray();

                    ParameterInfo[] parameters = method.GetParameters();

                    if (info.Ranges.Length != parameters.Length)
                    {
                        continue;
                    }

                    bool validRanges = true;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (info.Ranges[i].Type == GameplayType.None ||
                            GameplayShared.GetRangeForType(parameters[i].ParameterType) != info.Ranges[i].Type)
                        {
                            validRanges = false;
                            break;
                        }
                    }

                    if (validRanges && info.Command != null && info.Method != null && info.SystemType != null)
                    {
                        SharedData.GameplayUnits[info.Command.Group + "." + type.FullName + "." + method.Name] = new GameplayExecutorShared.GameplayUnit()
                        {
                            IsVariable = false,
                            Command = info
                        };
                    }
                }
            }
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }
    }
}
