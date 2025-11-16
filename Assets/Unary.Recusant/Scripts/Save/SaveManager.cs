using Newtonsoft.Json;
using System.IO;
using Unary.Core;

namespace Unary.Recusant
{
    public class SaveManager : System<SaveManager>
    {
        public SaveState State { get; private set; } = null;
        public static string StatePath { get; set; } = string.Empty;

        private void LoadState()
        {
            if (!File.Exists(StatePath))
            {
                Core.Logger.Instance.Error($"Failed loading state from path \"{StatePath}\"");
                return;
            }

            string stateText = File.ReadAllText(StatePath);

            State = JsonConvert.DeserializeObject<SaveState>(stateText);

            if (State == null)
            {
                Core.Logger.Instance.Error($"Failed to deserialize state from path \"{StatePath}\"");
            }
        }

        private static void Save(SaveState state)
        {
            string stateText = null;

            bool pretty = false;

#if UNITY_EDITOR
            pretty = true;
#endif

            if (pretty)
            {
                stateText = JsonConvert.SerializeObject(state, Formatting.Indented);
            }
            else
            {
                stateText = JsonConvert.SerializeObject(state);
            }

            if (stateText != null)
            {
                File.WriteAllText(StatePath, stateText);
            }
        }

        public static void SaveDefault()
        {
            StatePath = "Saves/Characters/Default.json";
            Save(new());
        }

        public override void Initialize()
        {
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

            LevelManager.Instance.OnAwake.Subscribe(OnLevelLoaded, this);
        }

        private bool OnLevelLoaded(ref LevelManager.LevelEventData data)
        {
            // Do stuff here

            return true;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            LevelManager.Instance.OnAwake.Unsubscribe(this);
        }
    }
}
