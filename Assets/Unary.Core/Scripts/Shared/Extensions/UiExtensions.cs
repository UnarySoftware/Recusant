using UnityEngine.UIElements;

namespace Unary.Core
{
    public static class UiExtensions
    {
        public static void Click(this VisualElement element)
        {
            using (var clickEvent = ClickEvent.GetPooled())
            {
                clickEvent.target = element;
                element.panel.visualTree.SendEvent(clickEvent);
            }
            if (element is Button button)
            {
                using var ev = new MouseUpEvent() { target = button };
                button.SendEvent(ev);
            }
        }
    }
}
