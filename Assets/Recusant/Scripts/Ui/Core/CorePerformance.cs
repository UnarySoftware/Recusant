using UnityEngine;
using UnityEngine.UIElements;

namespace Recusant
{
    public class CorePerformance : UiUnit
    {
        private readonly VisualElement[] _roots = new VisualElement[PerformanceManager.PerformanceMetricCount];
        private readonly VisualElement[] _icons = new VisualElement[PerformanceManager.PerformanceMetricCount];
        private readonly float[] _timers = new float[PerformanceManager.PerformanceMetricCount];

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();

            _roots[0] = Document.rootVisualElement.Q("ClientFps");
            _icons[0] = Document.rootVisualElement.Q("ClientFpsIcon");
            _roots[0].style.opacity = 0.0f;
            _timers[0] = 0.0f;

            _roots[1] = Document.rootVisualElement.Q("ServerFps");
            _icons[1] = Document.rootVisualElement.Q("ServerFpsIcon");
            _roots[1].style.opacity = 0.0f;
            _timers[1] = 0.0f;

            _roots[2] = Document.rootVisualElement.Q("Bandwith");
            _icons[2] = Document.rootVisualElement.Q("BandwithIcon");
            _roots[2].style.opacity = 0.0f;
            _timers[2] = 0.0f;

            _roots[3] = Document.rootVisualElement.Q("Latency");
            _icons[3] = Document.rootVisualElement.Q("LatencyIcon");
            _roots[3].style.opacity = 0.0f;
            _timers[3] = 0.0f;

            _roots[4] = Document.rootVisualElement.Q("PacketLoss");
            _icons[4] = Document.rootVisualElement.Q("PacketLossIcon");
            _roots[4].style.opacity = 0.0f;
            _timers[4] = 0.0f;
        }

        public override void Deinitialize()
        {

        }

        public override void Open()
        {

        }

        public override void Close()
        {

        }

        private void ProcessVisual(PerformanceManager manager, int index)
        {
            if (manager.PerformanceScales[index] != PerformanceManager.ProblemScale.None)
            {
                _roots[index].style.opacity = 1.0f;
                _timers[index] = 3.0f;
            }

            if (manager.PerformanceScales[index] == PerformanceManager.ProblemScale.High)
            {
                _icons[index].style.unityBackgroundImageTintColor = Color.red;
            }
            else if (manager.PerformanceScales[index] == PerformanceManager.ProblemScale.Medium)
            {
                _icons[index].style.unityBackgroundImageTintColor = Color.yellow;
            }
        }

        private void ProcessTimer(int index)
        {
            if (_timers[index] > 0.0f)
            {
                _timers[index] -= Time.deltaTime;
                if (_timers[index] < 1.0f)
                {
                    _roots[index].style.opacity = _timers[index];
                }
            }
        }

        void Update()
        {
            if (PerformanceManager.Instance == null)
            {
                return;
            }

            PerformanceManager manager = PerformanceManager.Instance;

            if (!manager.Profiling)
            {
                for (int i = 0; i < _timers.Length; i++)
                {
                    _timers[i] = 0.0f;
                }
            }

            for (int i = 0; i < _timers.Length; i++)
            {
                ProcessVisual(manager, i);
                ProcessTimer(i);
            }
        }
    }
}
