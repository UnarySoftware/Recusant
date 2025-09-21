using Core;
using Netick.Unity;
using UnityEngine;

namespace Recusant
{
    public class PerformanceManager : System<PerformanceManager>
    {
        [SystemAssetInject("scriptableobjects/system/performancemanagerdata.asset")]
        private readonly PerformanceManagerData _data;

        public enum ProblemScale
        {
            None = 0,
            Medium,
            High
        };

        public const int PerformanceMetricCount = 5;
        public ProblemScale[] PerformanceScales { get; private set; } = new ProblemScale[PerformanceMetricCount];

        public int FpsValue { get; private set; }
        public ProblemScale FpsProblems
        {
            get
            {
                return PerformanceScales[0];
            }
            private set
            {
                PerformanceScales[0] = value;
            }
        }

        public float ServerTickTimeAverage { get; private set; }
        public float ServerTickTimeMax { get; private set; }
        public ProblemScale ServerTickTimeProblems
        {
            get
            {
                return PerformanceScales[1];
            }
            private set
            {
                PerformanceScales[1] = value;
            }
        }

        public float BandwithMax { get; private set; }
        public ProblemScale BandwithProblems
        {
            get
            {
                return PerformanceScales[2];
            }
            private set
            {
                PerformanceScales[2] = value;
            }
        }

        public float Rtt { get; private set; }
        public ProblemScale RttProblems
        {
            get
            {
                return PerformanceScales[3];
            }
            private set
            {
                PerformanceScales[3] = value;
            }
        }

        public float PacketLossMax { get; private set; }
        public ProblemScale PacketLossProblems
        {
            get
            {
                return PerformanceScales[4];
            }
            private set
            {
                PerformanceScales[4] = value;
            }
        }

        private void ResetValues()
        {
            FpsValue = 60;
            FpsProblems = ProblemScale.None;

            ServerTickTimeAverage = 0.01f;
            ServerTickTimeMax = 0.01f;
            ServerTickTimeProblems = ProblemScale.None;

            BandwithMax = 0.01f;
            BandwithProblems = ProblemScale.None;

            Rtt = 0.0f;
            RttProblems = ProblemScale.None;

            PacketLossMax = 0.0f;
            PacketLossProblems = ProblemScale.None;
        }

        private bool _profiling = false;
        public bool Profiling
        {
            get
            {
                return _profiling;
            }
            set
            {
                ResetValues();
                _profiling = value;
            }
        }

        public override void Initialize()
        {
            ResetValues();
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }

        private void ProcessClientFps()
        {
            FpsValue = Mathf.RoundToInt(1.0f / Time.unscaledDeltaTime);
            FpsProblems = ProblemScale.None;

            if (FpsValue < _data.ClientFpsHighThreshold)
            {
                FpsProblems = ProblemScale.High;
            }
            else if (FpsValue < _data.ClientFpsMediumThreshold)
            {
                FpsProblems = ProblemScale.Medium;
            }
        }

        private void ProcessServerFps(NetworkSandbox sandbox)
        {
            ServerTickTimeAverage = sandbox.Monitor.ServerTickTime.Average;
            ServerTickTimeMax = sandbox.Monitor.ServerTickTime.Max;
            ServerTickTimeProblems = ProblemScale.None;

            if (ServerTickTimeAverage >= sandbox.FixedDeltaTime)
            {
                ServerTickTimeProblems = ProblemScale.High;
            }
            else if (ServerTickTimeMax >= sandbox.FixedDeltaTime)
            {
                ServerTickTimeProblems = ProblemScale.Medium;
            }
        }

        private void ProcessBandwith(NetworkSandbox sandbox)
        {
            BandwithMax = Mathf.Max(sandbox.InKBps, sandbox.OutKBps); // Kb/s
            BandwithProblems = ProblemScale.None;

            if (BandwithMax > _data.BandwithHighThreshold)
            {
                BandwithProblems = ProblemScale.High;
            }
            else if (BandwithMax > _data.BandwithMediumThreshold)
            {
                BandwithProblems = ProblemScale.Medium;
            }
        }

        private void ProcessLatency(NetworkSandbox sandbox)
        {
            Rtt = (float)sandbox.RTT * 1000.0f; // multiplying by 1000 to convert from seconds to milliseconds.
            RttProblems = ProblemScale.None;

            if (Rtt > _data.LatencyHighThreshold)
            {
                RttProblems = ProblemScale.High;
            }
            else if (Rtt > _data.LatencyMediumThreshold)
            {
                RttProblems = ProblemScale.Medium;
            }
        }

        private void ProcessPacketLoss(NetworkSandbox sandbox)
        {
            float inLoss = sandbox.InPacketLoss * 100.0f; // %
            float outLoss = sandbox.OutPacketLoss * 100.0f; // %
            PacketLossMax = Mathf.Max(inLoss, outLoss);
            PacketLossProblems = ProblemScale.None;

            if (PacketLossMax > _data.PacketLossHighThreshold)
            {
                PacketLossProblems = ProblemScale.High;
            }
            else if (PacketLossMax > _data.PacketLossMediumThreshold)
            {
                PacketLossProblems = ProblemScale.Medium;
            }
        }

        public void Update()
        {
            if (!_profiling)
            {
                return;
            }

            ProcessClientFps();

            if (NetworkManager.Instance != null && NetworkManager.Instance.IsRunning)
            {
                var sandbox = NetworkManager.Instance.Sandbox;

                if (sandbox != null && sandbox.IsConnected && sandbox.IsVisible)
                {
                    ProcessServerFps(sandbox);
                    ProcessBandwith(sandbox);
                    ProcessLatency(sandbox);
                    ProcessPacketLoss(sandbox);
                }
            }
        }
    }
}
