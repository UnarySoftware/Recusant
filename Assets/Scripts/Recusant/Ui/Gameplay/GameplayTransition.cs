using System;
using UnityEngine.UIElements;

namespace Recusant
{
    public class GameplayTransition : UiUnit
    {
        private Label _transitionLabel;

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();
            _transitionLabel = Document.rootVisualElement.Q<Label>("TransitionLabel");
            _transitionLabel.style.display = DisplayStyle.None;

            LevelTransitionEvent.Instance.Subscribe(TransitionRequest, this);
        }

        public override void Deinitialize()
        {
            LevelTransitionEvent.Instance.Unsubscribe(this);
        }

        private bool TransitionRequest(LevelTransitionEvent data)
        {
            if (data.CurrentCount != 0 && data.TargetCount != 0)
            {
                _transitionLabel.style.display = DisplayStyle.Flex;
                _transitionLabel.text = $"Waiting for players( {data.CurrentCount} / {data.TargetCount} )";
            }
            else
            {
                _transitionLabel.style.display = DisplayStyle.None;
            }

            return true;
        }

        public override void Open()
        {

        }

        public override void Close()
        {

        }
    }
}
