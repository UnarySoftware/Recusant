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
            LevelManager.Instance.OnTransitionRequest.Publish(new()
            {
                CurrentCount = CurrentCount,
                TargetCount = TargetCount
            });

            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            if (LevelManager.Instance == null || LevelManager.Instance.LevelData == null)
            {
                return;
            }

            string levelName = LevelManager.Instance.LevelData.NextLevelName;

            bool validLevel = !string.IsNullOrEmpty(levelName) && !string.IsNullOrWhiteSpace(levelName);

            if (CurrentCount == TargetCount && validLevel)
            {
                LevelManager.Instance.LoadLevelNetworked(levelName);
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
