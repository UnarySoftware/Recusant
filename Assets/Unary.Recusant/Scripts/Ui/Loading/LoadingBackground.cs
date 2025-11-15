using Unary.Core;
using UnityEngine.UIElements;

namespace Unary.Recusant
{
    public class LoadingBackground : UiUnit
    {
        private VisualElement _background;

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();

            _background = Document.rootVisualElement.Q<VisualElement>("Background");
        }

        public override void Deinitialize()
        {

        }

        public override void Open()
        {
            if (LoadingScreen.Instance.Enabled)
            {
                LoadingScreen.Instance.Enabled = false;
                _background.style.backgroundColor = new(StyleKeyword.Initial);
                _background.style.backgroundImage = LoadingScreen.Instance.SelectedEntry.Asset.Value;
            }
        }

        public override void Close()
        {

        }
    }
}
