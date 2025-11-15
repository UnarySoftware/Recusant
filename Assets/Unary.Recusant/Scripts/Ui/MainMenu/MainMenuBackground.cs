using Unary.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unary.Recusant
{
    public class MainMenuBackground : UiUnit
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

        private bool _firstTime = true;
        private bool _fading = false;
        private float _opacity = 1.0f;

        public override void Open()
        {
            if (_firstTime)
            {
                _fading = true;
                _firstTime = false;
                _background.style.backgroundImage = LoadingScreen.Instance.SelectedEntry.Asset.Value;
            }
            else
            {
                _fading = false;
            }

            if (LoadingScreen.Instance.Enabled)
            {
                LoadingScreen.Instance.Enabled = false;
            }
        }

        public void Update()
        {
            if (!IsOpen())
            {
                return;
            }

            if (_fading)
            {
                if (_opacity >= 0.0f)
                {
                    _opacity -= Time.deltaTime * 0.25f;
                    _background.style.opacity = _opacity;
                }
            }
            else
            {
                _background.style.opacity = 1.0f;
            }
        }

        public override void Close()
        {

        }
    }
}
