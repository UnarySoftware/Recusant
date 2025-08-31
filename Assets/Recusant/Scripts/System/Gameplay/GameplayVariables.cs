using Core;
using System.Collections.Generic;
using System.Linq;

namespace Recusant
{
    public class GameplayVariablesShared : SystemShared
    {
        public List<AbstractVariable> SubscribedVariables = new();
    }

    public class GameplayVariables : SystemNetworkRoot<GameplayVariables, GameplayVariablesShared>
    {
        public void ResetToOriginal()
        {
            foreach (var variable in SharedData.SubscribedVariables)
            {
                variable.ResetToOriginal();
            }
        }

        private void OnVariableChanged(int id)
        {
            if (!NetworkManager.Initialized || !GameplayVariablesNetwork.Initialized || NetworkManager.Instance.IsClient)
            {
                return;
            }

            var targetVariable = SharedData.SubscribedVariables[id];
            var targetNetworkedVariable = GameplayVariablesNetwork.Instance.ReplicatedVariables[id];

            if (targetVariable.IsOriginal())
            {
                // We want to reset the value to a default state so that newly connecting players wont need to replicate it
                if (targetNetworkedVariable != default)
                {
                    GameplayVariablesNetwork.Instance.ReplicatedVariables[id] = default;
                }

                return;
            }

            var networked = targetVariable.ToNetwork();
            networked.Changed = true;
            GameplayVariablesNetwork.Instance.ReplicatedVariables[id] = networked;
        }

        public override void Initialize()
        {
            List<string> networkedVariables = new();
            List<string> nonNetworkedVariables = new();

            foreach (var unit in GameplayExecutor.Instance.GameplayUnits)
            {
                if (unit.Value.IsVariable)
                {
                    if (unit.Value.Variable.GetFlags().HasFlag(GameplayFlag.Replicated))
                    {
                        networkedVariables.Add(unit.Key);
                    }
                    else
                    {
                        nonNetworkedVariables.Add(unit.Key);
                    }
                }
            }

            int idCounter = 0;

            var sortedNetworked = networkedVariables.OrderBy(k => k);
            var sortedNonNetworked = nonNetworkedVariables.OrderBy(k => k);

            foreach (var key in sortedNetworked)
            {
                var unit = GameplayExecutor.Instance.GameplayUnits[key];

                unit.Variable.Id = idCounter;

                SharedData.SubscribedVariables.Add(unit.Variable);
                SharedData.SubscribedVariables[idCounter].Id = idCounter;
                SharedData.SubscribedVariables[idCounter].OnChanged += OnVariableChanged;

                idCounter++;
            }

            foreach (var key in sortedNonNetworked)
            {
                var unit = GameplayExecutor.Instance.GameplayUnits[key];

                unit.Variable.Id = idCounter;

                idCounter++;
            }
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            for (int i = 0; i < SharedData.SubscribedVariables.Count; i++)
            {
                if (SharedData.SubscribedVariables[i].GetFlags().HasFlag(GameplayFlag.Replicated))
                {
                    SharedData.SubscribedVariables[i].OnChanged -= OnVariableChanged;
                }
            }
        }
    }
}
