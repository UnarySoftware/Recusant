using Unary.Core;

namespace Unary.Recusant
{
    public class FlowManagerNetwork : SystemNetworkPrefab<FlowManagerNetwork, FlowManagerShared>
    {
        public override void Initialize()
        {

        }

        public override void Deinitialize()
        {

        }

        public override void NetworkFixedUpdate()
        {
            if (NetworkManager.Instance.IsClient ||
                LevelManager.Instance == null ||
                LevelManager.Instance.LevelData == null)
            {
                return;
            }

            AiTriangleData[] flowTriangles = LevelManager.Instance.LevelData.AiTriangles;

            float flow = float.PositiveInfinity;

            if (SharedData.Leader != default)
            {
                flow = flowTriangles[SharedData.Players[SharedData.Leader].AiTriangle].Flow;
            }

            int count = 0;

            foreach (var player in SharedData.Players)
            {
                if (player.Value.AiTriangle == -1)
                {
                    continue;
                }

                if (player.Value.HasTriangleFlag(AiMarkup.AiMarkupType.End))
                {
                    count++;
                }

                if (player.Key == SharedData.Leader)
                {
                    continue;
                }

                float playerFlow = flowTriangles[player.Value.AiTriangle].Flow;

                if (flow >= float.PositiveInfinity || playerFlow < flow)
                {
                    SharedData.Leader = player.Key;
                    flow = playerFlow;
                }
            }

            LevelManagerNetwork.Instance.CurrentCount = count;

            foreach (var player in SharedData.Players)
            {
                player.Value.Leader = player.Key == SharedData.Leader;
            }
        }
    }
}
