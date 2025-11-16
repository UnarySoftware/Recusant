using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unary.Core
{
    public class Performance : CoreSystem<Performance>
    {
        public enum TextMetricType
        {
            LowerIsBetter,
            HigherIsBetter
        };

        public enum TextMetricValueType
        {
            Basic,
            Medium,
            Critical
        };

        public class PerformanceMetric
        {
            // Shared
            public bool TextMetric;
            public int Index;
            public Func<double> Variable;
            // In text metric we pass both text AND texture
            // In number metric we pass text OR texture
            public string Text;
            public Texture2D Texture;

            // Text Metrics only
            public Func<double> Medium;
            public Func<double> Critical;
            public TextMetricType TextType;
            public Action<TextMetricValueType, int> OnTextMetricChange;

            // Number Metrics only
            public Action<double, int> OnNumberMetricChange;

            public Func<bool> ShouldPoll;
        }

        private void Dummy(TextMetricValueType a, int b)
        {

        }

        private void Dummy(double a, int b)
        {

        }

        private readonly List<double> _previousMetricValue = new();
        public List<PerformanceMetric> Metrics { get; private set; } = new();

        public void RemoveMetric(int index)
        {
            if (index < 0 || index >= Metrics.Count)
            {
                return;
            }

            _previousMetricValue.RemoveAt(index);
            Metrics.RemoveAt(index);
        }

        public int AddTextMetric(Func<double> variable, string text, Texture2D texture, Func<double> medium, Func<double> critical, TextMetricType textType, Func<bool> shouldPoll = null)
        {
            int index = Metrics.Count;

            Metrics.Add(new()
            {
                TextMetric = true,
                Index = index,
                Variable = variable,
                OnTextMetricChange = Dummy,
                Texture = texture,

                Text = text,
                Medium = medium,
                Critical = critical,
                TextType = textType,

                ShouldPoll = shouldPoll
            });

            _previousMetricValue.Add(0.0);

            return index;
        }

        public int AddNumberMetric(Func<double> variable, Texture2D texture, Func<bool> shouldPoll = null)
        {
            int index = Metrics.Count;

            Metrics.Add(new()
            {
                TextMetric = false,
                Index = index,
                Variable = variable,
                OnNumberMetricChange = Dummy,
                Texture = texture,
                ShouldPoll = shouldPoll
            });

            _previousMetricValue.Add(0.0);

            return index;
        }

        public int AddNumberMetric(Func<double> variable, string text, Func<bool> shouldPoll = null)
        {
            int index = Metrics.Count;

            Metrics.Add(new()
            {
                TextMetric = false,
                Index = index,
                Variable = variable,
                OnNumberMetricChange = Dummy,
                Text = text,
                ShouldPoll = shouldPoll
            });

            _previousMetricValue.Add(0.0);

            return index;
        }

        public override bool Initialize()
        {
            return true;
        }

        private void UpdateLowerIsBetter(PerformanceMetric metric, int index)
        {
            double value = metric.Variable();

            if (value > metric.Critical())
            {
                metric.OnTextMetricChange(TextMetricValueType.Critical, index);
            }
            else if (value > metric.Medium())
            {
                metric.OnTextMetricChange(TextMetricValueType.Medium, index);
            }
            else
            {
                metric.OnTextMetricChange(TextMetricValueType.Basic, index);
            }
        }

        private void UpdateHigherIsBetter(PerformanceMetric metric, int index)
        {
            double value = metric.Variable();

            if (value < metric.Critical())
            {
                metric.OnTextMetricChange(TextMetricValueType.Critical, index);
            }
            else if (value < metric.Medium())
            {
                metric.OnTextMetricChange(TextMetricValueType.Medium, index);
            }
            else
            {
                metric.OnTextMetricChange(TextMetricValueType.Basic, index);
            }
        }

        private void UpdateNumberMetric(PerformanceMetric metric, int index)
        {
            double value = metric.Variable();
            double valuePrevious = _previousMetricValue[index];

            if (System.Math.Abs(value - valuePrevious) > Math.Epsilon)
            {
                metric.OnNumberMetricChange(value, index);
            }

            _previousMetricValue[index] = value;
        }

        private void Update()
        {
            for (int i = 0; i < Metrics.Count; i++)
            {
                PerformanceMetric metric = Metrics[i];

                if (metric.ShouldPoll != null && !metric.ShouldPoll())
                {
                    continue;
                }

                if (!metric.TextMetric)
                {
                    UpdateNumberMetric(metric, i);
                }
                else if (metric.TextType == TextMetricType.LowerIsBetter)
                {
                    UpdateLowerIsBetter(metric, i);
                }
                else if (metric.TextType == TextMetricType.HigherIsBetter)
                {
                    UpdateHigherIsBetter(metric, i);
                }
            }
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }
    }
}
