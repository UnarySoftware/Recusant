using Core;
using Netick;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Utf8Json;
using Utf8Json.Resolvers;

namespace Recusant
{
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
                if (_object == null && ScriptableObjectRegistry.Instance != null)
                {
                    _object = ScriptableObjectRegistry.Instance.GetObject<T>(_id);
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

    public class SaveManager : System<SaveManager>
    {
        private static bool InitializedFormatters = false;

        public SaveState State { get; private set; } = null;
        public static string StatePath { get; set; } = string.Empty;

        private static void InitializeFormatters()
        {
            if (InitializedFormatters)
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

        private void LoadState()
        {
            if (!File.Exists(StatePath))
            {
                Core.Logger.Instance.Error("Failed loading state from path " + StatePath);
                return;
            }

            string stateText = File.ReadAllText(StatePath);

            InitializeFormatters();

            try
            {
                State = JsonSerializer.Deserialize<SaveState>(stateText);
            }
            catch (Exception e)
            {
                Core.Logger.Instance.Error(e);
                return;
            }
        }

        private static void Save(SaveState state)
        {
            byte[] stateBytes;

            bool pretty = false;

#if UNITY_EDITOR
            pretty = true;
#endif

            InitializeFormatters();

            try
            {
                stateBytes = JsonSerializer.Serialize(state);

                if (pretty)
                {
                    stateBytes = JsonSerializer.PrettyPrintByteArray(stateBytes);
                }
            }
            catch (Exception e)
            {
                if (Core.Logger.Instance == null)
                {
                    Debug.LogException(e);
                }
                else
                {
                    Core.Logger.Instance.Log(e);
                }
                return;
            }

            File.WriteAllBytes(StatePath, stateBytes);
        }

        public static void SaveDefault()
        {
            StatePath = "Saves/Characters/Default.json";
            Save(new());
        }

        public override void Initialize()
        {
            InitializeFormatters();

            if (!Directory.Exists("Saves/Characters"))
            {
                Directory.CreateDirectory("Saves/Characters");
                SaveDefault();
            }

            if(StatePath == string.Empty)
            {
                SaveDefault();
            }

            LoadState();

            LevelEvent.Instance.Subscribe(OnLevelLoaded, this);
        }

        private bool OnLevelLoaded(LevelEvent data)
        {
            if (data.Type != LevelEventType.Awake)
            {
                return true;
            }

            return true;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            LevelEvent.Instance.Unsubscribe(this);
        }
    }
}
