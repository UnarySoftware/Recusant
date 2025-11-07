using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unary.Core
{
    public class CoreConsole : UiUnit
    {
        private VisualElement _root;
        private ScrollView _entryList;

        public AssetRef<VisualTreeAsset> ConsoleEntry;

        private int _currentConsoleLines = 0;
        private bool _userScrolledUp = false;

        public int MaxConsoleLines = 128;

        private StringBuilder _builder = new();

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();

            _root = Document.rootVisualElement.Q("Console");
            _entryList = Document.rootVisualElement.Q<ScrollView>("ConsoleList");
            _entryList.verticalScroller.valueChanged += OnScrollChanged;

            _root.style.display = DisplayStyle.None;

            Logger.Instance.OnLog.Subscribe(OnLog, this);
        }

        private void OnScrollChanged(float value)
        {
            float maxScroll = _entryList.verticalScroller.highValue - _entryList.verticalScroller.lowValue;
            _userScrolledUp = (_entryList.scrollOffset.y < maxScroll - 10f);
        }

        public override void Deinitialize()
        {
            Logger.Instance.OnLog.Unsubscribe(this);
        }

        private void OnPrint(string message, string stackTrace, Color color, Color outline, Logger.LogType type)
        {
            if (_currentConsoleLines >= MaxConsoleLines)
            {
                _entryList.RemoveAt(0);
            }
            else
            {
                _currentConsoleLines++;
            }

            var NewEntry = ConsoleEntry.Value.Instantiate();

            Button NewButton = NewEntry.Q<Button>("ConsoleEntry");

            _builder.Append(message);
            _builder.Append("\nStack Trace:\n");
            _builder.Append(stackTrace);

            string formated = _builder.ToString();
            _builder.Clear();

            if (type == Logger.LogType.Log)
            {
                NewButton.text = message;
            }
            else
            {
                NewButton.text = formated;
            }

            NewButton.style.color = color;
            NewButton.style.unityTextOutlineColor = outline;
            NewButton.style.unityTextOutlineWidth = 0.25f;

            NewButton.RegisterCallback<ClickEvent>(ev =>
            {
                GUIUtility.systemCopyBuffer = formated;
            });

            _entryList.Add(NewEntry);

            if (!_userScrolledUp)
            {
                StartCoroutine(ScrollToBottom());
            }
        }

        private bool OnLog(ref Logger.LogEventData data)
        {
            switch (data.Type)
            {
                default:
                case Logger.LogType.Log:
                    {
                        OnPrint(data.Message, "No stacktrace available", Color.white, Color.black, Logger.LogType.Log);
                        break;
                    }
                case Logger.LogType.Warning:
                    {
                        if (string.IsNullOrEmpty(data.StackTrace) || string.IsNullOrWhiteSpace(data.StackTrace))
                        {
                            OnPrint("Warning: \"" + data.Message, "No stacktrace available", Color.yellow, Color.black, Logger.LogType.Warning);
                        }
                        else
                        {
                            OnPrint("Warning: \"" + data.Message, data.StackTrace, Color.yellow, Color.black, Logger.LogType.Warning);
                        }
                        break;
                    }
                case Logger.LogType.Error:
                    {
                        if (string.IsNullOrEmpty(data.StackTrace) || string.IsNullOrWhiteSpace(data.StackTrace))
                        {
                            OnPrint("Error: \"" + data.Message, "No stacktrace available", Color.red, Color.black, Logger.LogType.Error);
                        }
                        else
                        {
                            OnPrint("Error: \"" + data.Message, data.StackTrace, Color.red, Color.black, Logger.LogType.Error);
                        }
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

        private CursorLockMode _lockMode;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Tilde))
            {
                if (_root.style.display == DisplayStyle.None)
                {
                    _root.style.display = DisplayStyle.Flex;

                    _lockMode = UnityEngine.Cursor.lockState;
                    UnityEngine.Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    _root.style.display = DisplayStyle.None;
                    UnityEngine.Cursor.lockState = _lockMode;
                }
            }
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();

            _entryList.verticalScroller.value = _entryList.verticalScroller.highValue > 0 ? _entryList.verticalScroller.highValue : 0;
        }
    }
}
