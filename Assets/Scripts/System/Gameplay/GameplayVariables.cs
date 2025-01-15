using Netick;
using Netick.Unity;
using System.Collections.Generic;
using System.Linq;

public class GameplayVariables : NetworkBehaviour, ISystem
{
    public static GameplayVariables Instance = null;

    private List<AbstractVariable> _replicatedVariables = new();

#pragma warning disable IDE0051
    [OnChanged(nameof(ReplicatedVariables))]
    private void OnReplicatedVariablesChanged(OnChangedData onChangedData)
    {
        if(_skippingChange)
        {
            return;
        }

        _replicatedVariables[onChangedData.GetArrayChangedElementIndex()]
            .FromNetwork(ReplicatedVariables[onChangedData.GetArrayChangedElementIndex()]);
    }
#pragma warning restore IDE0051
    
    [Networked(size: CodeGenerated.GameplayVariableMaxCount)]
    public readonly NetworkArray<GameplayVariableNetwork> ReplicatedVariables = new(CodeGenerated.GameplayVariableMaxCount);

    private bool _skippingChange = false;

    private void OnVariableChanged(int id, int replicatedId)
    {
        if (Networking.Instance == null || Networking.Instance.IsClient)
        {
            return;
        }

        var targetVariable = _replicatedVariables[replicatedId];
        var networked = targetVariable.ToNetwork();
        ReplicatedVariables[replicatedId] = networked;
        _skippingChange = true;
    }

    [InitDependency(typeof(Networking))]
    public void Initialize()
    {
        // Sort by name in order to get a determenistic ordering for the network indexes
        var SortedKeys = Reflector.Instance.GameplayUnits.Keys.OrderBy(k => k);

        int idCounter = 0;
        int idReplicated = 0;

        foreach (var key in SortedKeys)
        {
            var Unit = Reflector.Instance.GameplayUnits[key];

            if(Unit.IsVariable)
            {
                Unit.Variable.Id = idCounter;
                idCounter++;

                if (Unit.Variable.GetFlags().HasFlag(GameplayFlag.Replicated))
                {
                    _replicatedVariables.Add(Unit.Variable);
                    _replicatedVariables[idReplicated].ReplicatedId = idReplicated;
                    _replicatedVariables[idReplicated].OnChanged += OnVariableChanged;
                    idReplicated++;
                }
            }
        }

    }

    public void Deinitialize()
    {
        for (int i = 0; i < _replicatedVariables.Count; i++)
        {
            if (_replicatedVariables[i].GetFlags().HasFlag(GameplayFlag.Replicated))
            {
                _replicatedVariables[i].OnChanged -= OnVariableChanged;
            }
        }
    }
}
