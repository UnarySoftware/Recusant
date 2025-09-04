using Core;
using System;
using System.IO;
using UnityEngine;
using Utf8Json;
using Utf8Json.Resolvers;

namespace Recusant
{
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

            if (StatePath == string.Empty)
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
