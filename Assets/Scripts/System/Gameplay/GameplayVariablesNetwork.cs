using Netick;
using Netick.Unity;

public class GameplayVariablesNetwork : CoreSystemPrefab<GameplayVariablesNetwork, GameplayVariablesShared>
{
    public override void Initialize()
    {
        
    }

    public override void Deinitialize()
    {
        
    }

    [Networked(size: CodeGenerated.GameplayVariableMaxCount)]
    public readonly NetworkArray<GameplayVariableNetwork> ReplicatedVariables = new(CodeGenerated.GameplayVariableMaxCount);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
    [OnChanged(nameof(ReplicatedVariables))]
    private void OnReplicatedVariablesChanged(OnChangedData onChangedData)
    {
        int changedIndex = onChangedData.GetArrayChangedElementIndex();

        GameplayVariableNetwork targetNetworkVariable = ReplicatedVariables[changedIndex];

        AbstractVariable targetVariable = SharedData.SubscribedVariables[changedIndex];

        if (IsClient)
        {
            if(targetNetworkVariable == default)
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
