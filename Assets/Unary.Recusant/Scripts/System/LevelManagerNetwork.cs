using Unary.Core;
using Netick;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unary.Recusant
{
    public class LevelManagerNetwork : SystemNetworkPrefab<LevelManagerNetwork, LevelManagerShared>
    {
        [Networked]
        public int CurrentCount { get; set; } = 0;

        [Networked]
        public int TargetCount { get; set; } = 0;

        [OnChanged(nameof(CurrentCount))]
        public void OnCurrentCountChanged(OnChangedData data)
        {
            UpdateCounts();
        }

        [OnChanged(nameof(TargetCount))]
        public void OnTargetCountChanged(OnChangedData data)
        {
            UpdateCounts();
        }

        public override void Initialize()
        {
            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            PlayerManager.Instance.OnPlayerAdded.Subscribe(OnPlayerAdded, this);
            PlayerManager.Instance.OnPlayerRemoved.Subscribe(OnPlayerRemoved, this);

            TargetCount = Sandbox.Players.Count;
        }

        public override void Deinitialize()
        {
            LevelManager.Instance.OnTransitionRequest.Publish(new()
            {
                CurrentCount = 0,
                TargetCount = 0
            });

            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            PlayerManager.Instance.OnPlayerAdded.Unsubscribe(this);
            PlayerManager.Instance.OnPlayerRemoved.Unsubscribe(this);
        }

        private void UpdateCounts()
        {
            if (LevelManager.Instance == null || LevelManager.Instance.LevelDefinition == null)
            {
                return;
            }

            LevelManager.Instance.OnTransitionRequest.Publish(new()
            {
                CurrentCount = CurrentCount,
                TargetCount = TargetCount
            });

            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            LevelDefinition nextLevel = LevelManager.Instance.LevelDefinition.NextLevel.Value;

            if (CurrentCount == TargetCount && nextLevel != null)
            {
                LevelManager.Instance.LoadLevelNetworked(nextLevel);
            }
        }

        private bool OnPlayerAdded(ref PlayerManager.PlayerChangedData data)
        {
            TargetCount = Sandbox.Players.Count;
            UpdateCounts();
            return true;
        }

        private bool OnPlayerRemoved(ref PlayerManager.PlayerChangedData data)
        {
            TargetCount = Sandbox.Players.Count;
            UpdateCounts();
            return true;
        }
    }
}
