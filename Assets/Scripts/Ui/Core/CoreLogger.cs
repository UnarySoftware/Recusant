using UnityEngine;
using UnityEngine.UIElements;

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

        Logger.Instance.OnLog += OnLog;
        Logger.Instance.OnWarning += OnWarning;
        Logger.Instance.OnError += OnError;
    }

    public override void Deinitialize()
    {
        Logger.Instance.OnLog -= OnLog;
        Logger.Instance.OnWarning -= OnWarning;
        Logger.Instance.OnError -= OnError;
    }

    private void OnLog(string Message, string StackTrace)
    {
        _logCounter++;
        _logLabel.text = _logCounter.ToString();
        _logTimer = 3.0f;
        _logElement.style.opacity = 1.0f;
    }

    private void OnWarning(string Message, string StackTrace)
    {
        _warningCounter++;
        _warningLabel.text = _warningCounter.ToString();
        _warningTimer = 3.0f;
        _warningElement.style.opacity = 1.0f;
    }

    private void OnError(string Message, string StackTrace)
    {
        _errorCounter++;
        _errorLabel.text = _errorCounter.ToString();
        _errorTimer = 3.0f;
        _errorElement.style.opacity = 1.0f;
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
            timer -= Time.deltaTime * 0.33f;
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
