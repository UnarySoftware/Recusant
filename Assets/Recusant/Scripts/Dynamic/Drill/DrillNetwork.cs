using Netick;
using Netick.Unity;

namespace Recusant
{
    public class DrillNetwork : NetworkBehaviour
    {
        public ScriptableObjectRef<DrillData> First;
        public ScriptableObjectRef<DrillData> Second;

        private void Awake()
        {
            First.Precache();
            Second.Precache();
        }

        public override void NetworkAwake()
        {
            DrillDataTestNetworked = new(First.Value);
        }

        [Networked]
        public ScriptableObjectNetworkRef<DrillData> DrillDataTestNetworked { get; set; }

        [OnChanged(nameof(DrillDataTestNetworked))]
        public void OnDataChanged(OnChangedData data)
        {
            string previousName = "Empty";
            if (data.GetPreviousValue<ScriptableObjectNetworkRef<DrillData>>().TryGetObject(out var previous))
            {
                previousName = previous.Name;
            }

            string currentName = "Empty";
            if (DrillDataTestNetworked.TryGetObject(out var current))
            {
                currentName = current.Name;
            }

            Core.Logger.Instance.Log("Was: " + previousName + " Became: " + currentName);
        }

        public override void NetworkFixedUpdate()
        {
            counter += Sandbox.FixedDeltaTime;

            if (counter > max)
            {
                counter = 0.0f;

                if (isFirst)
                {
                    isFirst = false;
                    DrillDataTestNetworked = new(Second.Value);
                }
                else
                {
                    isFirst = true;
                    DrillDataTestNetworked = new(First.Value);
                }
            }
        }

        float max = 1.0f;
        float counter = 0.0f;
        bool isFirst = true;
    }
}
