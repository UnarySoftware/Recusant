using Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Recusant
{
    public class CoreLogger : UiUnit
    {
        private VisualElement _logElement;
        private int _logCounter = 0;
        private Label _logLabel;

        private VisualElement _warningElement;
        private int _warningCounter = 0;
        private Label _warningLabel;

        private VisualElement _errorElement;
        private int _errorCounter = 0;
        private Label _errorLabel;

        private float _logTimer = 0.0f;
        private float _warningTimer = 0.0f;
        private float _errorTimer = 0.0f;

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();

            _logElement = Document.rootVisualElement.Q("Log");
            _logLabel = Document.rootVisualElement.Q<Label>("LogLabel");
            _logElement.style.opacity = 0.0f;

            _warningElement = Document.rootVisualElement.Q("Warning");
            _warningLabel = Document.rootVisualElement.Q<Label>("WarningLabel");
            _warningElement.style.opacity = 0.0f;

            _errorElement = Document.rootVisualElement.Q("Error");
            _errorLabel = Document.rootVisualElement.Q<Label>("ErrorLabel");
            _errorElement.style.opacity = 0.0f;

            LogEvent.Instance.Subscribe(OnLog, this);
        }

        public override void Deinitialize()
        {
            LogEvent.Instance.Unsubscribe(this);
        }

        private bool OnLog(LogEvent data)
        {
            if (data.Reciever == Core.Logger.LogReciever.None)
            {
                return true;
            }

            switch (data.Type)
            {
                default:
                case Core.Logger.LogType.Log:
                    {
                        _logCounter++;
                        _logLabel.text = _logCounter.ToString();
                        _logTimer = 3.0f;
                        _logElement.style.opacity = 1.0f;
                        break;
                    }
                case Core.Logger.LogType.Warning:
                    {
                        _warningCounter++;
                        _warningLabel.text = _warningCounter.ToString();
                        _warningTimer = 3.0f;
                        _warningElement.style.opacity = 1.0f;
                        break;
                    }
                case Core.Logger.LogType.Error:
                    {
                        _errorCounter++;
                        _errorLabel.text = _errorCounter.ToString();
                        _errorTimer = 3.0f;
                        _errorElement.style.opacity = 1.0f;
                        break;
                    }
            }

            return true;
        }

        public override void Open()
        {

        }

        public override void Close()
        {

        }

        private void ProcessTimer(ref VisualElement element, ref float timer)
        {
            if (timer > 0.0f)
            {
                timer -= Time.deltaTime;
                if (timer < 1.0f)
                {
                    element.style.opacity = timer;
                }
            }
        }

        void Update()
        {
            ProcessTimer(ref _logElement, ref _logTimer);
            ProcessTimer(ref _warningElement, ref _warningTimer);
            ProcessTimer(ref _errorElement, ref _errorTimer);
        }
    }
}
