using System;
using Core;
using UnityEngine.UIElements;

namespace Recusant
{
    public class CoreFlow : UiUnit
    {
        private Label _flowLabel;

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();

            _flowLabel = Document.rootVisualElement.Q<Label>("FlowLabel");
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

            _flowLabel.text += "\n" + GC.GetTotalMemory(false).ToSizeString();
        }
    }
}
