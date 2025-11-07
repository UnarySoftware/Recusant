using UnityEngine.UIElements;

namespace Unary.Recusant
{
    public class GameplayTransition : Core.UiUnit
    {
        private Label _transitionLabel;

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();
            _transitionLabel = Document.rootVisualElement.Q<Label>("TransitionLabel");
            _transitionLabel.style.display = DisplayStyle.None;

            LevelManager.Instance.OnTransitionRequest.Subscribe(TransitionRequest, this);
        }

        public override void Deinitialize()
        {
            LevelManager.Instance.OnTransitionRequest.Unsubscribe(this);
        }

        private bool TransitionRequest(ref LevelManager.LevelTransitionInfo data)
        {
            if (data.CurrentCount != 0 && data.TargetCount != 0)
            {
                SteamAchievementManager.Instance.GiveAchievement("FinishedGame");
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
