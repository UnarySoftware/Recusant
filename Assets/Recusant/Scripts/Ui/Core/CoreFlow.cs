using System;
using Core;
using Unity.Profiling;
using UnityEngine.UIElements;

namespace Recusant
{
    public class CoreFlow : UiUnit
    {
        private Label _flowLabel;

        ProfilerRecorder systemMemoryRecorder;
        ProfilerRecorder gcMemoryRecorder;
        ProfilerRecorder mainThreadTimeRecorder;

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();

            _flowLabel = Document.rootVisualElement.Q<Label>("FlowLabel");

            systemMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            gcMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        }

        static double GetRecorderFrameAverage(ProfilerRecorder recorder)
        {
            var samplesCount = recorder.Capacity;
            if (samplesCount == 0)
                return 0;

            double r = 0;
            unsafe
            {
                var samples = stackalloc ProfilerRecorderSample[samplesCount];
                recorder.CopyTo(samples, samplesCount);
                for (var i = 0; i < samplesCount; ++i)
                    r += samples[i].Value;
                r /= samplesCount;
            }

            return r;
        }

        public override void Deinitialize()
        {
            systemMemoryRecorder.Dispose();
            gcMemoryRecorder.Dispose();
            mainThreadTimeRecorder.Dispose();
        }

        public override void Open()
        {

        }

        public override void Close()
        {

        }

        void Update()
        {
            if (PlayerManager.Instance == null ||
                PlayerManager.Instance.LocalPlayer == null ||
                LevelManager.Instance == null ||
                LevelManager.Instance.LevelData == null)
            {
                return;
            }

            PlayerFlow flow = PlayerManager.Instance.LocalPlayer.GetComponent<PlayerFlow>();

            int triangleIndex = flow.AiTriangle;

            if (triangleIndex == -1)
            {
                return;
            }

            string Flags = string.Empty;

            foreach (var value in AiMarkup.TypeValues)
            {
                if (!flow.HasTriangleFlag(value))
                {
                    continue;
                }

                Flags += value.ToString() + " ";
            }

            AiTriangleData triangle = LevelManager.Instance.LevelData.AiTriangles[triangleIndex];

            _flowLabel.text = "Flow: " + triangle.Flow;

            if (Flags != string.Empty)
            {
                _flowLabel.text += "\nFlags: " + Flags;
            }

            _flowLabel.text += $"\nFrame Time: {GetRecorderFrameAverage(mainThreadTimeRecorder) * (1e-6f):F1} ms";
            _flowLabel.text += $"\nGC Memory: {gcMemoryRecorder.LastValue / (1024 * 1024)} MB";
            _flowLabel.text += $"\nSystem Memory: {systemMemoryRecorder.LastValue / (1024 * 1024)} MB";
        }
    }
}
