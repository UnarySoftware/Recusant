using Unary.Core;
using Netick;
using System.Collections.Generic;
using UnityEngine;

namespace Unary.Recusant
{
    public class FlowManagerShared : SystemShared
    {
        public Dictionary<NetworkPlayerId, PlayerFlow> Players = new();
        public NetworkPlayerId Leader = default;
    }

    public class FlowManager : SystemNetworkRoot<FlowManager, FlowManagerShared>
    {
        public override void Initialize()
        {
            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            LevelManager.Instance.OnAwake.Subscribe(OnDataReset, this);
            LevelManager.Instance.OnDestroyNetwork.Subscribe(OnDataReset, this);
            PlayerManager.Instance.OnPlayerAdded.Subscribe(OnPlayerAdded, this);
            PlayerManager.Instance.OnPlayerRemoved.Subscribe(OnPlayerRemoved, this);
        }

        private bool OnDataReset(ref LevelManager.LevelEventData _)
        {
            SharedData.Players.Clear();
            SharedData.Leader = default;

            HashSet<Vector3> _spawnPositions = new();

            var data = LevelManager.Instance.LevelData;

            List<Vector3> randomProbe = new();
            int addedCounter;

            foreach (var bound in data.AiBounds)
            {
                if (bound.Triangles.Length <= 4)
                {
                    _spawnPositions.Add(data.AiTriangles[Random.Range(0, bound.Triangles.Length)].Center);
                }
                else
                {
                    randomProbe.Clear();
                    addedCounter = 0;

                    foreach (var triangle in bound.Triangles)
                    {
                        randomProbe.Add(data.AiTriangles[triangle].Center);
                    }

                    while (addedCounter <= 4)
                    {
                        int index = Random.Range(0, randomProbe.Count);
                        _spawnPositions.Add(randomProbe[index]);
                        randomProbe.RemoveAt(index);
                        addedCounter++;
                    }

                }
            }

#if UNITY_EDITOR

            Vector3 position = new();
            RaycastHit[] hits = new RaycastHit[1];

            foreach (var randomSpawn in _spawnPositions)
            {
                position = randomSpawn;
                position.y += 1.0f;

                if (Physics.RaycastNonAlloc(new Ray(position, Vector3.down), hits, 2.0f) > 0)
                {
                    GameObject newObject = new();
                    newObject.AddComponent<AiSpawnVisualizer>();
                    newObject.transform.parent = LevelManager.Instance.LevelRoot.transform;

                    position.x = hits[0].point.x;
                    position.y = hits[0].point.y + 1.1f;
                    position.z = hits[0].point.z;

                    newObject.transform.position = position;
                }
            }

#endif

            return true;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            if (NetworkManager.Instance.IsClient)
            {
                return;
            }

            LevelManager.Instance.OnAwake.Unsubscribe(this);
            LevelManager.Instance.OnDestroyNetwork.Unsubscribe(this);
            PlayerManager.Instance.OnPlayerAdded.Unsubscribe(this);
            PlayerManager.Instance.OnPlayerRemoved.Unsubscribe(this);
        }

        private bool OnPlayerAdded(ref PlayerManager.PlayerChangedData data)
        {
            SharedData.Players[data.Id] = data.GameObject.GetComponent<PlayerFlow>();
            return true;
        }

        private bool OnPlayerRemoved(ref PlayerManager.PlayerChangedData data)
        {
            SharedData.Players.Remove(data.Id);

            if (data.Id == SharedData.Leader)
            {
                SharedData.Leader = default;
            }
            return true;
        }
    }
}
