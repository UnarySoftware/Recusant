using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    public class PerformanceManager : System<PerformanceManager>
    {
        [SystemAssetInject("scriptableobjects/system/performancemanagerdata.asset")]
        private readonly AssetRef<PerformanceManagerData> _data = default;

        float[] _fpsAverage;
        int _fpsAverageIndex = 0;

        [SystemAssetInject("textures/ui/performance/clientfps.png")]
        private readonly AssetRef<Texture2D> _clientFpsTexture = default;
        [SystemAssetInject("textures/ui/performance/serverfps.png")]
        private readonly AssetRef<Texture2D> _serverFpsTexture = default;
        [SystemAssetInject("textures/ui/performance/bandwith.png")]
        private readonly AssetRef<Texture2D> _bandwithTexture = default;
        [SystemAssetInject("textures/ui/performance/latency.png")]
        private readonly AssetRef<Texture2D> _latencyTexture = default;
        [SystemAssetInject("textures/ui/performance/packetloss.png")]
        private readonly AssetRef<Texture2D> _packetLossTexture = default;

        [SystemAssetInject("textures/ui/performance/log.png")]
        private readonly AssetRef<Texture2D> _logTexture = default;
        [SystemAssetInject("textures/ui/performance/warning.png")]
        private readonly AssetRef<Texture2D> _warningTexture = default;
        [SystemAssetInject("textures/ui/performance/error.png")]
        private readonly AssetRef<Texture2D> _errorTexture = default;

        public float Fps { get; private set; } = 0.0f;

        public override void Initialize()
        {
            _fpsAverage = new float[_data.Value.ClientFpsAverageSample];
            Fps = _data.Value.ClientFpsAverageSample;

            Performance.Instance.AddTextMetric(
                () => Fps,
                "Low\nFramerate",
                _clientFpsTexture.Value,
                () => _data.Value.ClientFpsMediumThreshold,
                () => _data.Value.ClientFpsHighThreshold,
                Performance.TextMetricType.HigherIsBetter);

            Performance.Instance.AddTextMetric(
                () => NetworkManager.Instance.Sandbox.FixedDeltaTime,
                "Low Server\nFramerate",
                _serverFpsTexture.Value,
                () => NetworkManager.Instance.Sandbox.Monitor.ServerTickTime.Max,
                () => NetworkManager.Instance.Sandbox.Monitor.ServerTickTime.Average,
                Performance.TextMetricType.HigherIsBetter,
                ShouldPollNetworkMetrics);

            Performance.Instance.AddTextMetric(
                () => Mathf.Max(NetworkManager.Instance.Sandbox.InKBps, NetworkManager.Instance.Sandbox.OutKBps),
                "High\nBandwith",
                _bandwithTexture.Value,
                () => _data.Value.BandwithMediumThreshold,
                () => _data.Value.BandwithHighThreshold,
                Performance.TextMetricType.LowerIsBetter,
                ShouldPollNetworkMetrics);

            Performance.Instance.AddTextMetric(
                () => (NetworkManager.Instance.Sandbox.RTT * 1000.0) / 2.0,
                "High\nLatency",
                _latencyTexture.Value,
                () => _data.Value.LatencyMediumThreshold,
                () => _data.Value.LatencyHighThreshold,
                Performance.TextMetricType.LowerIsBetter,
                ShouldPollNetworkMetrics);

            Performance.Instance.AddTextMetric(
                () => Mathf.Max(NetworkManager.Instance.Sandbox.InPacketLoss * 100.0f, NetworkManager.Instance.Sandbox.OutPacketLoss * 100.0f),
                "High\nPacket Loss",
                _packetLossTexture.Value,
                () => _data.Value.PacketLossMediumThreshold,
                () => _data.Value.PacketLossHighThreshold,
                Performance.TextMetricType.LowerIsBetter,
                ShouldPollNetworkMetrics);

            if (Bootstrap.Instance.IsDebug)
            {
                Performance.Instance.AddNumberMetric(() => Core.Logger.Instance.LogCount, _logTexture.Value);
            }

            Performance.Instance.AddNumberMetric(() => Core.Logger.Instance.WarningCount, _warningTexture.Value);
            Performance.Instance.AddNumberMetric(() => Core.Logger.Instance.ErrorCount, _errorTexture.Value);
        }

        private bool ShouldPollNetworkMetrics()
        {
            if (NetworkManager.Instance != null && NetworkManager.Instance.IsRunning)
            {
                var sandbox = NetworkManager.Instance.Sandbox;

                if (sandbox != null && sandbox.IsConnected && sandbox.IsVisible)
                {
                    return true;
                }
            }

            return false;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }

        public void Update()
        {
            _fpsAverage[_fpsAverageIndex] = 1.0f / Time.unscaledDeltaTime;
            _fpsAverageIndex++;

            if (_fpsAverageIndex >= _fpsAverage.Length)
            {
                _fpsAverageIndex = 0;

                float final = 0.0f;

                for (int i = 0; i < _fpsAverage.Length; i++)
                {
                    final += _fpsAverage[i];
                }

                final /= _fpsAverage.Length;

                Fps = final;
            }
        }
    }
}
