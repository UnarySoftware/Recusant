using Core;
using Netick;
using System.Diagnostics;
using UnityEngine.SceneManagement;

namespace Recusant
{
    public class LevelManagerNetwork : SystemNetworkPrefab<LevelManagerNetwork, LevelManagerShared>
    {
        private int _currentCount = 0;

        [Networked]
        public int CurrentCount { get; set; } = 0;

        [Networked]
        public int TargetCount { get; set; } = 0;

#pragma warning disable IDE0051

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

        public override void NetworkFixedUpdate()
        {
            if (CurrentCount != _currentCount)
            {
                Logger.Instance.Log("Changed to: " + CurrentCount);
                _currentCount = CurrentCount;
            }
        }

#pragma warning restore IDE0051

        public override void Initialize()
        {
            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            //NetworkPlayerJoinedEvent.Instance.Subscribe(OnPlayerJoined, this);
            //NetworkPlayerLeftEvent.Instance.Subscribe(OnPlayerLeft, this);

            TargetCount = Sandbox.Players.Count;
        }

        public override void Deinitialize()
        {
            LevelTransitionEvent.Instance.Publish(0, 0);

            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            //NetworkPlayerJoinedEvent.Instance.Unsubscribe(this);
            //NetworkPlayerLeftEvent.Instance.Unsubscribe(this);
        }

        private void UpdateCounts()
        {
            //LevelTransitionEvent.Instance.Publish(CurrentCount, TargetCount);

            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            if(LevelManager.Instance == null || LevelManager.Instance.LevelData == null)
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

        private bool OnPlayerJoined(NetworkPlayerJoinedEvent data)
        {
            TargetCount = Sandbox.Players.Count;
            UpdateCounts();
            return true;
        }

        private bool OnPlayerLeft(NetworkPlayerLeftEvent data)
        {
            TargetCount = Sandbox.Players.Count;
            UpdateCounts();
            return true;
        }

#pragma warning restore UNT0006

    }
}
