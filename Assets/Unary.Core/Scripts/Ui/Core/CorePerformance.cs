using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unary.Core
{
    public class CorePerformance : UiUnit
    {
        public AssetRef<VisualTreeAsset> TextEntry;
        public AssetRef<VisualTreeAsset> NumberEntry;

        public VisualElement TextRoot;
        public VisualElement NumberRoot;

        private readonly List<VisualElement> _roots = new();
        private readonly List<Performance.PerformanceMetric> _metrics = new();
        private readonly List<float> _timers = new();

        // For Text metrics
        private readonly List<VisualElement> _icons = new();

        // For Number metrics
        private readonly List<Label> _labels = new();

        private void OnTextChanged(Performance.TextMetricValueType type, int index)
        {
            if (type == Performance.TextMetricValueType.Basic)
            {
                return;
            }

            _timers[index] = 3.0f;

            if (type == Performance.TextMetricValueType.Medium)
            {
                _icons[index].style.unityBackgroundImageTintColor = Color.yellow;
            }
            else if (type == Performance.TextMetricValueType.Critical)
            {
                _icons[index].style.unityBackgroundImageTintColor = Color.red;
            }
        }

        private void OnNumberChanged(double value, int index)
        {
            _timers[index] = 3.0f;
            _labels[index].text = value.ToString("G");
        }

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();

            TextRoot = Document.rootVisualElement.Q<VisualElement>("TextRoot");
            NumberRoot = Document.rootVisualElement.Q<VisualElement>("NumberRoot");

            foreach (var metric in Performance.Instance.Metrics)
            {
                _metrics.Add(metric);
                _timers.Add(0.0f);

                if (metric.TextMetric)
                {
                    var NewEntry = TextEntry.Value.Instantiate();

                    _roots.Add(NewEntry);
                    TextRoot.Add(NewEntry);

                    Label label = NewEntry.Q<Label>("Text");
                    label.text = metric.Text;

                    VisualElement icon = NewEntry.Q<VisualElement>("Icon");
                    icon.style.backgroundImage = metric.Texture;

                    _icons.Add(icon);
                    _labels.Add(null);

                    metric.OnTextMetricChange += OnTextChanged;
                }
                else
                {
                    var NewEntry = NumberEntry.Value.Instantiate();

                    _roots.Add(NewEntry);
                    NumberRoot.Add(NewEntry);

                    VisualElement icon = NewEntry.Q<VisualElement>("Icon");
                    Label label = NewEntry.Q<Label>("Label");

                    if (metric.Texture != null)
                    {
                        icon.style.backgroundImage = metric.Texture;
                        label.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        label.text = metric.Text;
                        icon.style.display = DisplayStyle.None;
                    }

                    _icons.Add(null);
                    _labels.Add(NewEntry.Q<Label>("Text"));

                    metric.OnNumberMetricChange += OnNumberChanged;
                }
            }
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

        void Update()
        {
            for (int i = 0; i < _roots.Count; i++)
            {
                float timer = _timers[i];
                VisualElement root = _roots[i];

                timer -= Time.deltaTime;

                if (timer >= 1.0f)
                {
                    root.style.opacity = 1.0f;
                    root.style.display = DisplayStyle.Flex;
                }
                else if (timer <= 0.0f)
                {
                    root.style.opacity = 1.0f;
                    root.style.display = DisplayStyle.None;
                }
                else
                {
                    root.style.opacity = timer;
                }

                if (timer <= 0.0f)
                {
                    timer = 0.0f;
                }

                _timers[i] = timer;
            }
        }
    }
}
