using Core;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Recusant
{
    public class CoreConsole : UiUnit
    {
        private VisualElement _root;
        private ScrollView _entryList;
        private TextField _input;

        public VisualTreeAsset ConsoleEntry;

        private Label[] _autocompleteLabels = new Label[MaxAutocompleteLines];
        private AbstractVariable[] _autocompleteVariables = new AbstractVariable[MaxAutocompleteLines];

        public int MaxHistoryEntries = 32;

        private string[] _historyArray = null;
        private Queue<string> _history = new();
        private int _historyIndex = -1;

        private int _currentConsoleLines = 0;
        public int MaxConsoleLines = 128;

        public const int MaxAutocompleteLines = 5;

        private readonly StringBuilder _builder = new();

        private static List<string> GetAutocompleteMatches(string input, bool strict = false, int maxMatches = int.MaxValue)
        {
            if (input.Contains(" "))
            {
                string[] spaceParts = input.ToUpper().Split(' ');
                input = spaceParts[0];
            }

            string[] inputParts = input.ToUpper().Split('.');

            List<string> results = new();

            int counter = 0;

            foreach (var target in GameplayExecutor.Instance.GameplayUnits.Keys)
            {
                if (counter >= maxMatches)
                {
                    break;
                }

                if (strict)
                {
                    if (input.ToUpper() == target.ToUpper())
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

        public void OnKeyDownEvent(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Tab)
            {
                var matches = GetAutocompleteMatches(_input.value);

                string commonString = GetCommonStartingSubString(matches);

                if (!string.IsNullOrWhiteSpace(commonString) && !_input.value.Contains(" "))
                {
                    _input.value = commonString;
                }

                _input.SelectRange(99999, 99999);

                UpdateAutocomplete();

                _input.Focus();

                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                if (_history.Count > 0)
                {
                    if (_historyIndex == -1)
                    {
                        _historyIndex = 0;
                    }
                    else
                    {
                        _historyIndex++;

                        if (_historyIndex == _history.Count)
                        {
                            _historyIndex = 0;
                        }
                    }

                    _input.value = _historyArray[_historyIndex];

                    _input.SelectRange(99999, 99999);

                    UpdateAutocomplete();

                    _input.Focus();

                    evt.StopPropagation();
                }
            }
            else if (evt.keyCode == KeyCode.DownArrow)
            {
                if (_history.Count > 0)
                {
                    if (_historyIndex == -1)
                    {
                        _historyIndex = _history.Count - 1;
                    }
                    else
                    {
                        _historyIndex--;

                        if (_historyIndex < 0)
                        {
                            _historyIndex = _history.Count - 1;
                        }
                    }

                    _input.value = _historyArray[_historyIndex];

                    _input.SelectRange(99999, 99999);

                    UpdateAutocomplete();

                    _input.Focus();

                    evt.StopPropagation();
                }
            }
            else
            {
                _historyIndex = -1;
            }
        }

        private void OnInputEvent(InputEvent evt)
        {
            if (Input.GetKeyDown(KeyCode.Tilde) || Input.GetKeyDown(KeyCode.BackQuote))
            {
                _input.SetValueWithoutNotify(evt.previousData);
            }
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

            _input.RegisterCallback<KeyDownEvent>(OnKeyDownEvent, TrickleDown.TrickleDown);
            _input.RegisterCallback<InputEvent>(OnInputEvent, TrickleDown.TrickleDown);

            _root.style.display = DisplayStyle.None;

            for (int i = 1; i <= MaxAutocompleteLines; i++)
            {
                _autocompleteLabels[i - 1] = Document.rootVisualElement.Q<Label>("ConsoleAutocomplete" + i);
                _autocompleteLabels[i - 1].style.opacity = 0.0f;
            }

            LogEvent.Instance.Subscribe(OnLog, this);
        }

        public override void Deinitialize()
        {
            LogEvent.Instance.Unsubscribe(this);
        }

        private void OnPrint(string Message, string Tooltip, Color color, Color outline, Core.Logger.LogType type)
        {
            if (_currentConsoleLines >= MaxConsoleLines)
            {
                _entryList.RemoveAt(0);
            }

            var NewEntry = ConsoleEntry.Instantiate();

            _entryList.Add(NewEntry);

            Foldout NewFoldout = NewEntry.Q<Foldout>("ConsoleEntry");

            NewFoldout.Q(className: Foldout.inputUssClassName).pickingMode = PickingMode.Ignore;
            NewFoldout.Q(className: Foldout.toggleUssClassName).pickingMode = PickingMode.Ignore;
            NewFoldout.Q(className: Foldout.textUssClassName).pickingMode = PickingMode.Ignore;


            NewFoldout.text = Message;
            NewFoldout.style.color = color;
            NewFoldout.style.unityTextOutlineColor = outline;
            NewFoldout.style.unityTextOutlineWidth = 0.25f;

            NewFoldout.Q(className: Foldout.textUssClassName).style.whiteSpace = WhiteSpace.Normal;
            NewFoldout.Q(className: Foldout.contentUssClassName).style.marginLeft = 8.0f;
            NewFoldout.Q(className: Foldout.checkmarkUssClassName).pickingMode = PickingMode.Position;

            bool validTooltip = !string.IsNullOrEmpty(Tooltip) && !string.IsNullOrWhiteSpace(Tooltip);

            if (type == Core.Logger.LogType.Log || !validTooltip)
            {
                NewFoldout.Q(className: Foldout.checkmarkUssClassName).pickingMode = PickingMode.Ignore;
                NewFoldout.toggleOnLabelClick = false;

                VisualElement ElementButton = NewFoldout.Q(className: Foldout.checkmarkUssClassName);
                ElementButton.style.display = DisplayStyle.None;
            }
            else
            {
                Label NewLabel = NewEntry.Q<Label>("ConsoleLabel");
                NewLabel.text = Tooltip;
                NewLabel.style.color = color;
                NewLabel.style.unityTextOutlineColor = outline;
                NewLabel.style.unityTextOutlineWidth = 0.25f;
            }

            if (_currentConsoleLines < MaxConsoleLines)
            {
                _currentConsoleLines++;
            }
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
                        OnPrint(data.Message, "No tooltip available", Color.white, Color.black, Core.Logger.LogType.Log);
                        break;
                    }
                case Core.Logger.LogType.Warning:
                    {
                        OnPrint("Warning: \"" + data.Message, data.StackTrace, Color.yellow, Color.black, Core.Logger.LogType.Warning);
                        break;
                    }
                case Core.Logger.LogType.Error:
                    {
                        OnPrint("Error: \"" + data.Message, data.StackTrace, Color.red, Color.black, Core.Logger.LogType.Error);
                        break;
                    }
            }

            return true;
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
                GameplayExecutorShared.GameplayUnit unit = GameplayExecutor.Instance.GameplayUnits[matchedName];

                _builder.Clear();

                _builder.Append(matchedName);
                _builder.Append(" ");

                if (unit.IsVariable)
                {
                    if (unit.Variable.IsRanged())
                    {
                        _builder.Append(GameplayShared.StringifyRanges(unit.Variable.GetMinRange(), unit.Variable.GetMaxRange(), GameplayShared.GetRangeForType(unit.Variable.GetTypeSystem())));
                    }
                    _builder.Append(GameplayShared.StringifyVariable(unit.Variable.GetObject(), unit.Variable.GetTypeEnum()));
                    _builder.Append("\n");
                    _builder.Append(unit.Variable.Description);

                    _autocompleteVariables[labelCounter] = unit.Variable;
                    _autocompleteVariables[labelCounter].OnChanged += OnVariableChange;
                }
                else
                {
                    var parameters = unit.Command.Method.GetParameters();

                    int parameterCounter = 0;

                    foreach (var parameter in parameters)
                    {
                        GameplayCommandRange range = unit.Command.Ranges[parameterCounter];

                        if (range is not GameplayCommandIgnore)
                        {
                            _builder.Append(GameplayShared.StringifyRanges(range.Min, range.Max, range.Type));
                        }

                        _builder.AppendFormat("{0}", GameplayShared.StringifyType(parameter.ParameterType));

                        if (parameterCounter < parameters.Length - 1)
                        {
                            _builder.Append(", ");
                        }

                        parameterCounter++;
                    }

                    _builder.Append('\n');
                    _builder.Append(unit.Command.Command.Description);
                }

                _autocompleteLabels[labelCounter].text = _builder.ToString();
                _autocompleteLabels[labelCounter].style.opacity = 1.0f;
                labelCounter++;
            }
        }

        private void OnVariableChange(int id)
        {
            UpdateAutocomplete();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Tilde))
            {
                if (_root.style.display == DisplayStyle.None)
                {
                    if (PlayerManager.Instance.LocalPlayer != null)
                    {
                        PlayerManager.Instance.LocalPlayer.GetComponent<PlayerInput>().AllowInputs = false;
                    }

                    _root.style.display = DisplayStyle.Flex;
                    _input.Focus();
                    _input.SelectRange(99999, 99999);
                    UnityEngine.Cursor.lockState = CursorLockMode.None;
                    StartCoroutine(DelayScroll());
                }
                else
                {
                    if (PlayerManager.Instance.LocalPlayer != null)
                    {
                        PlayerManager.Instance.LocalPlayer.GetComponent<PlayerInput>().AllowInputs = true;
                    }

                    _root.style.display = DisplayStyle.None;
                    UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                }
            }

            if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrEmpty(_input.value) && !string.IsNullOrWhiteSpace(_input.value))
            {
                _historyIndex = -1;

                bool unique = true;

                foreach (var item in _history)
                {
                    if (item == _input.value)
                    {
                        unique = false;
                        break;
                    }
                }

                if (unique)
                {
                    if (_history.Count == MaxHistoryEntries)
                    {
                        _history.Dequeue();
                    }

                    _history.Enqueue(_input.value);

                    _historyArray = _history.ToArray();
                }

                GameplayExecutor.Instance.Execute(_input.value);

                _input.value = string.Empty;
                StartCoroutine(DelayFocus());
            }
        }

        IEnumerator DelayScroll()
        {
            yield return new WaitForEndOfFrame();

            if (_entryList.childCount > 0)
            {
                _entryList.ScrollTo(_entryList.ElementAt(_entryList.childCount - 1));
            }
        }

        IEnumerator DelayFocus()
        {
            yield return new WaitForEndOfFrame();
            _input.Focus();
        }
    }
}
