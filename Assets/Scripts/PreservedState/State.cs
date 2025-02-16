using Netick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Utf8Json;
using Utf8Json.Resolvers;

[Serializable]
[Networked]
public class ScriptableObjectRef<T> where T : BaseScriptableObject
{
    private Guid _id = Guid.Empty;
    private T _object = null;

    [Networked]
    public Guid Id
    {
        get { return _id; }
        set { _id = value; }
    }

    public T Value
    { 
        get
        {
            if(_object == null && Registry.Instance != null)
            {
                _object = Registry.Instance.GetObject<T>(_id);
            }

            return _object;
        }
        private set
        {

        }
    }

    public ScriptableObjectRef(Guid value)
    {
        _id = value;
    }
}

public class ScriptableObjectRefFormatter<T> : IJsonFormatter<ScriptableObjectRef<T>> where T : BaseScriptableObject
{
    public void Serialize(ref JsonWriter writer, ScriptableObjectRef<T> value, IJsonFormatterResolver formatterResolver)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var formatter = formatterResolver.GetFormatterWithVerify<Guid>();
        formatter.Serialize(ref writer, value.Id, formatterResolver);
    }

    public ScriptableObjectRef<T> Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
    {
        if (reader.ReadIsNull())
        {
            return null;
        }

        Guid value = formatterResolver.GetFormatterWithVerify<Guid>().Deserialize(ref reader, formatterResolver);
        return new ScriptableObjectRef<T>(value);
    }
}

public class ScriptableObjectResolver : IJsonFormatterResolver
{
    public static readonly IJsonFormatterResolver Instance = new ScriptableObjectResolver();

    ScriptableObjectResolver()
    {

    }

    public IJsonFormatter<T> GetFormatter<T>()
    {
        return FormatterCache<T>.formatter;
    }

    static class FormatterCache<T>
    {
        public static readonly IJsonFormatter<T> formatter;

        static FormatterCache()
        {
            formatter = (IJsonFormatter<T>)GetFormatter(typeof(T));
        }
    }

    public static object GetFormatter(Type t)
    {
        var ti = t.GetTypeInfo();

        if (ti.IsGenericType)
        {
            var genericType = ti.GetGenericTypeDefinition();

            if (genericType == typeof(ScriptableObjectRef<>))
            {
                return CreateInstance(typeof(ScriptableObjectRefFormatter<>), ti.GenericTypeArguments);
            }
        }

        return null;
    }

    static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
    {
        return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
    }
}

[Serializable]
public abstract class BaseState
{
    
}

public class State : CoreSystem<State>
{
    private static bool InitializedFormatters = false;

    public static bool IsSaving { get; private set; } = false;

    public static ServerState Server { get; private set; }
    private static string ServerPath = string.Empty;

    public static ClientState Client { get; private set; }
    private static string ClientPath = string.Empty;

    private static void InitializeFormatters()
    {
        if(InitializedFormatters)
        {
            return;
        }

        CompositeResolver.RegisterAndSetAsDefault(
        // use generated resolver first, and combine many other generated/custom resolvers
        ScriptableObjectResolver.Instance,
        // set StandardResolver or your use resolver chain
        StandardResolver.Default
        );

        InitializedFormatters = true;
    }

    public void LoadState(string statePath, bool serverState)
    {
        if (!File.Exists(statePath))
        {
            Logger.Instance.Error("Failed loading state from path " + statePath);
            return;
        }

        string stateText = File.ReadAllText(statePath);

        InitializeFormatters();

        try
        {
            if (serverState)
            {
                Server = JsonSerializer.Deserialize<ServerState>(stateText);
                ServerPath = statePath;
            }
            else
            {
                Client = JsonSerializer.Deserialize<ClientState>(stateText);
                ClientPath = statePath;
            }
        }
        catch (Exception e)
        {
            Logger.Instance.Error(e);
            return;
        }
    }

    public static void SaveDefault(string statePath, bool serverState)
    {
        BaseState targetState = serverState ? new ServerState() : new ClientState();

        Save(statePath, targetState, serverState);
    }

    public static void Save(bool serverState)
    {
        if (serverState)
        {
            Save(ServerPath, Server, serverState);
        }
        else
        {
            Save(ClientPath, Client, serverState);
        }
    }

    private static void Save(string statePath, BaseState targetState, bool serverState)
    {
        byte[] stateBytes;

        bool pretty = false;

#if UNITY_EDITOR
        pretty = true;
#endif

        InitializeFormatters();

        try
        {
            if (serverState)
            {
                stateBytes = JsonSerializer.Serialize((ServerState)targetState);

                if (pretty)
                {
                    stateBytes = JsonSerializer.PrettyPrintByteArray(stateBytes);
                }
            }
            else
            {
                stateBytes = JsonSerializer.Serialize((ClientState)targetState);

                if (pretty)
                {
                    stateBytes = JsonSerializer.PrettyPrintByteArray(stateBytes);
                }
            }
        }
        catch (Exception e)
        {
            if (Logger.Instance == null)
            {
                Debug.LogException(e);
            }
            else
            {
                Logger.Instance.Log(e);
            }
            return;
        }

        File.WriteAllBytes(statePath, stateBytes);
    }

    [InitDependency(typeof(Launcher), typeof(Networking))]
    public override void Initialize()
    {
        InitializeFormatters();

        Networking.Instance.LevelLoaded += LevelLoaded;
    }

    private void LevelLoaded(string name, LevelRoot root)
    {

    }

    public override void PostInitialize()
    {

    }

    public override void Deinitialize()
    {

    }
}
