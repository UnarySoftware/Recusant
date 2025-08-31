using Core;
using Netick;
using Netick.Unity;

namespace Recusant
{
    public class GameplayVariablesNetwork : SystemNetworkPrefab<GameplayVariablesNetwork, GameplayVariablesShared>
    {
        public override void Initialize()
        {

        }

        public override void Deinitialize()
        {

        }

        public const int NetworkedCountMax = 256; // TODO Maybe implement code-gen edit for this in future

        [Networked(size: NetworkedCountMax)]
        public readonly NetworkArray<GameplayVariableNetwork> ReplicatedVariables = new(NetworkedCountMax);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        [OnChanged(nameof(ReplicatedVariables))]
        private void OnReplicatedVariablesChanged(OnChangedData onChangedData)
        {
            int changedIndex = onChangedData.GetArrayChangedElementIndex();

            GameplayVariableNetwork targetNetworkVariable = ReplicatedVariables[changedIndex];

            AbstractVariable targetVariable = SharedData.SubscribedVariables[changedIndex];

            if (IsClient)
            {
                if (targetNetworkVariable == default)
                {
                    targetVariable.ResetToOriginal();
                }
                else
                {
                    targetVariable.FromNetwork(targetNetworkVariable);
                }
            }

            Logger.Instance.Log("Variable \"" + targetVariable.Name + "\" changed to " + GameplayShared.StringifyVariable(targetVariable.GetObject(), targetVariable.GetTypeEnum()), Logger.LogReciever.ServerOnly);
        }
    }
}
