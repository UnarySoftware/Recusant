using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class CoreConsole : UiUnit
{
    private VisualElement _root;
    private ScrollView _entryList;
    private TextField _input;

    public VisualTreeAsset _consoleEntry;

    private Label[] _autocompleteLabels = new Label[MaxAutocompleteLines];
    private AbstractVariable[] _autocompleteVariables = new AbstractVariable[MaxAutocompleteLines];

    private int _currentConsoleLines = 0;
    public int MaxConsoleLines = 128;

    public const int MaxAutocompleteLines = 5;

    private StringBuilder _builder = new();

    private static List<string> GetAutocompleteMatches(string input, bool strict = false, int maxMatches = int.MaxValue)
    {
        string[] inputParts = input.ToUpper().Split('.');

        List<string> results = new();

        int counter = 0;

        foreach (var target in Reflector.Instance.GameplayUnits.Keys)
        {
            if (counter >= maxMatches)
            {
                break;
            }

            if(strict)
            {
                if(input.ToUpper() == target.ToUpper())
                {
                    results.Add(target);
                    counter++;
                }
            }
            else
            {
                string[] targetParts = target.ToUpper().Split('.');

                if (targetParts.Length < inputParts.Length)
                {
                    continue;
                }

                bool match = true;

                for (int i = 0; i < inputParts.Length; i++)
                {
                    if (targetParts[i] != inputParts[i] && !targetParts[i].StartsWith(inputParts[i]))
                    {
                        match = false;
                    }
                }

                if (match)
                {
                    results.Add(target);
                    counter++;
                }
            }
        }

        return results;
    }

    public static string GetCommonStartingSubString(List<string> strings)
    {
        if (strings.Count == 0)
        {
            return "";
        }

        if (strings.Count == 1)
        {
            return strings[0];
        }

        int charIndex = 0;

        while (IsCommonChar(strings, charIndex))
        {
            ++charIndex;
        }

        return strings[0][..charIndex];
    }

    private static bool IsCommonChar(List<string> strings, int charIndex)
    {
        if (strings[0].Length <= charIndex)
        {
            return false;
        }

        for (int i = 1; i < strings.Count; ++i)
        {
            if (strings[i].Length <= charIndex || strings[i][charIndex] != strings[0][charIndex])
            {
                return false;
            }
        }

        return true;
    }

    public override void Initialize()
    {
        var Document = GetComponent<UIDocument>();

        _root = Document.rootVisualElement.Q("Console");
        _entryList = Document.rootVisualElement.Q<ScrollView>("ConsoleList");
        _input = Document.rootVisualElement.Q<TextField>("ConsoleInput");

        _input.RegisterValueChangedCallback(evt =>
        {
            UpdateAutocomplete();
        });

        _input.RegisterCallback<KeyDownEvent>((evt) =>
        {
            if (evt.keyCode == KeyCode.Tab)
            {
                var matches = GetAutocompleteMatches(_input.value);

                _input.value = GetCommonStartingSubString(matches);

                _input.SelectRange(99999, 99999);

                UpdateAutocomplete();

                _input.Focus();

                evt.StopPropagation();
            }
        }, TrickleDown.TrickleDown);

        _root.style.display = DisplayStyle.None;

        for (int i = 1; i <= MaxAutocompleteLines; i++)
        {
            _autocompleteLabels[i - 1] = Document.rootVisualElement.Q<Label>("ConsoleAutocomplete" + i);
            _autocompleteLabels[i - 1].style.opacity = 0.0f;
        }

        Logger.Instance.OnLog += OnLog;
        Logger.Instance.OnWarning += OnWarning;
        Logger.Instance.OnError += OnError;

        // TODO: Move to quality related manager
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 75;
    }

    public override void Deinitialize()
    {
        Logger.Instance.OnLog -= OnLog;
        Logger.Instance.OnWarning -= OnWarning;
        Logger.Instance.OnError -= OnError;
    }

    private void OnPrint(string Message, Color color, Color outline)
    {
        if (_currentConsoleLines >= MaxConsoleLines)
        {
            _entryList.RemoveAt(0);
        }

        var NewEntry = _consoleEntry.Instantiate();
        _entryList.Add(NewEntry);
        Label NewLabel = NewEntry.Q<Label>("ConsoleEntry");
        NewLabel.text = Message;
        NewLabel.style.color = color;
        NewLabel.style.unityTextOutlineColor = outline;
        NewLabel.style.unityTextOutlineWidth = 0.15f;

        _currentConsoleLines++;
    }

    private void OnLog(string Message, string StackTrace)
    {
        OnPrint(Message, Color.white, Color.black);
    }

    private void OnWarning(string Message, string StackTrace)
    {
        OnPrint("Warning: \"" + Message + "\"\nStack trace:\n" + StackTrace, Color.yellow, Color.black);
    }

    private void OnError(string Message, string StackTrace)
    {
        OnPrint("Error: \"" + Message + "\"\nStack trace:\n" + StackTrace, Color.red, Color.black);
    }

    public override void Open()
    {
        UpdateAutocomplete();
    }

    public override void Close()
    {

    }

    private void UpdateAutocomplete()
    {
        for (int i = 0; i < MaxAutocompleteLines; i++)
        {
            if (_autocompleteVariables[i] != null)
            {
                if (_autocompleteVariables[i].OnChanged != null)
                {
                    _autocompleteVariables[i].OnChanged -= OnVariableChange;
                }
            }

            _autocompleteLabels[i].style.opacity = 0.0f;
        }

        string targetInput = string.Empty;
        bool strict = false;

        if (_input.value.Contains(" "))
        {
            targetInput = _input.value.Split(' ')[0];
            strict = true;
        }
        else
        {
            targetInput = _input.value;
        }

        var matches = GetAutocompleteMatches(targetInput, strict, MaxAutocompleteLines);

        int labelCounter = 0;

        foreach (var matchedName in matches)
        {
            Reflector.GameplayUnit unit = Reflector.Instance.GameplayUnits[matchedName];

            _builder.Clear();

            _builder.Append(matchedName);
            _builder.Append(" ");

            if (unit.IsVariable)
            {
                if (unit.Variable.IsRanged())
                {
                    _builder.Append(unit.Variable.GetRanges());
                    _builder.Append(" ");
                }
                _builder.Append(unit.Variable.GetObject().ToString());
                _builder.Append("\n");
                _builder.Append(unit.Variable.Description);

                _autocompleteVariables[labelCounter] = unit.Variable;
                _autocompleteVariables[labelCounter].OnChanged += OnVariableChange;
            }
            else
            {
                var parameters = unit.Command.Method.GetParameters();

                _builder.Append("[");

                int parameterCounter = 0;

                foreach (var parameter in parameters)
                {
                    _builder.AppendFormat("{0}", GameplayShared.StringifyType(parameter.ParameterType));

                    if (parameterCounter < parameters.Length - 1)
                    {
                        _builder.Append(", ");
                    }

                    parameterCounter++;
                }

                _builder.Append("]\n");
                _builder.Append(unit.Command.Command.Description);
            }

            _autocompleteLabels[labelCounter].text = _builder.ToString();
            _autocompleteLabels[labelCounter].style.opacity = 1.0f;
            labelCounter++;
        }
    }

    private void OnVariableChange(int id, int replicatedId)
    {
        UpdateAutocomplete();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (_root.style.display == DisplayStyle.None)
            {
                _root.style.display = DisplayStyle.Flex;
                _input.Focus();
                UnityEngine.Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                _root.style.display = DisplayStyle.None;
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            }
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            _input.value = string.Empty;
            StartCoroutine(DelayFocus());
        }
    }

    IEnumerator DelayFocus()
    {
        yield return new WaitForSeconds(0.1f);
        _input.Focus();
    }
}
