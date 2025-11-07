using Unary.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unary.Recusant
{
    public class LoadingSpinner : UiUnit
    {
        private VisualElement _spinner;
        private float _degree = 0.0f;

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();

            _spinner = Document.rootVisualElement.Q<VisualElement>("Spinner");
        }

        public override void Deinitialize()
        {

        }

        private void Update()
        {
            if (!IsOpen())
            {
                return;
            }

            _degree += 270.0f * Time.deltaTime;
            _spinner.style.rotate = new Rotate(_degree);
        }

        public override void Open()
        {
            _degree = 0.0f;
        }

        public override void Close()
        {

        }
    }
}
