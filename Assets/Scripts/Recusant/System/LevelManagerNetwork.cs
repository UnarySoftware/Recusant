using Core;
using Netick;
using UnityEngine.SceneManagement;

namespace Recusant
{
    public class LevelManagerNetwork : SystemNetworkPrefab<LevelManagerNetwork, LevelManagerShared>
    {
        [Networked]
        public int CurrentCount { get; set; } = 0;

        [Networked]
        public int TargetCount { get; set; } = 0;

#pragma warning disable IDE0051

        [OnChanged(nameof(CurrentCount))]
        private void OnCurrentCountChanged(OnChangedData _)
        {
            UpdateCounts();
        }

        [OnChanged(nameof(TargetCount))]
        private void OnTargetCountChanged(OnChangedData _)
        {
            UpdateCounts();
        }

#pragma warning restore IDE0051

        public override void Initialize()
        {
            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            NetworkPlayerConnectedEvent.Instance.Subscribe(OnPlayerConnected, this);
            NetworkPlayerDisconnectedEvent.Instance.Subscribe(OnPlayerDisconnected, this);

            TargetCount = Sandbox.ConnectedPlayers.Count;
        }

        public override void Deinitialize()
        {
            LevelTransitionEvent.Instance.Publish(0, 0);

            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            NetworkPlayerConnectedEvent.Instance.Unsubscribe(this);
            NetworkPlayerDisconnectedEvent.Instance.Unsubscribe(this);
        }

        private void UpdateCounts()
        {
            LevelTransitionEvent.Instance.Publish(CurrentCount, TargetCount);

            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            string levelName = LevelManager.Instance.LevelData.NextLevelName;

            bool validLevel = !string.IsNullOrEmpty(levelName) && !string.IsNullOrWhiteSpace(levelName);

            if (CurrentCount == TargetCount && validLevel)
            {
                // TODO Move this to a custom method
                NetworkManager.Instance.Sandbox.LoadCustomSceneAsync(NetickSceneHandler.Instance.GetSceneIndex(levelName), 
                    new() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.Physics3D });
            }
        }

#pragma warning disable UNT0006

        private bool OnPlayerConnected(NetworkPlayerConnectedEvent data)
        {
            TargetCount = Sandbox.ConnectedPlayers.Count;
            UpdateCounts();
            return true;
        }

        private bool OnPlayerDisconnected(NetworkPlayerDisconnectedEvent data)
        {
            TargetCount = Sandbox.ConnectedPlayers.Count;
            UpdateCounts();
            return true;
        }

#pragma warning restore UNT0006

    }
}
