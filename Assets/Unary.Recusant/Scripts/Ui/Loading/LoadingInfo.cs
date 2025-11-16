using System.Collections.Generic;
using Unary.Core;
using UnityEngine.UIElements;

namespace Unary.Recusant
{
    public class LoadingInfo : UiUnit
    {
        public AssetRef<VisualTreeAsset> LoadingInfoEntry;
        private VisualElement _infoList;
        private ProgressBar _globalProgress;

        private readonly Dictionary<string, VisualElement> _nameToElement = new();
        private readonly HashSet<string> _removeQueue = new();

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();

            _infoList = Document.rootVisualElement.Q<VisualElement>("LocalProgress");
            _globalProgress = Document.rootVisualElement.Q<ProgressBar>("GlobalProgressBar");
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

            _globalProgress.value = LoadingManager.Instance.GlobalValue;
            _globalProgress.title = $"{_globalProgress.value:P0}";

            var values = LoadingManager.Instance.GetJobs();

            foreach (var name in _nameToElement)
            {
                if (!values.Contains(name.Key))
                {
                    _removeQueue.Add(name.Key);
                }
            }

            foreach (var value in values)
            {
                if (_removeQueue.Contains(value))
                {
                    continue;
                }

                if (!_nameToElement.TryGetValue(value, out var element))
                {
                    element = LoadingInfoEntry.Value.Instantiate();
                    _infoList.Add(element);
                    _nameToElement[value] = element;
                }

                Label label = element.Q<Label>("Label");
                label.text = value;
            }

            foreach (var name in _removeQueue)
            {
                _infoList.Remove(_nameToElement[name]);
                _nameToElement.Remove(name);
            }

            _removeQueue.Clear();
        }

        public override void Open()
        {

        }

        public override void Close()
        {

        }
    }
}
