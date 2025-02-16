using UnityEngine.UIElements;

public static class UiUtils
{
    public static void Click(this VisualElement element)
    {
        // For RegisterCallback<ClickEvent>
        using (var clickEvent = ClickEvent.GetPooled())
        {
            clickEvent.target = element;
            element.panel.visualTree.SendEvent(clickEvent);
        }
        // For clicked += event registration
        if (element is Button button)
        {
            using var ev = new MouseUpEvent() { target = button };
            button.SendEvent(ev);
        }
    }
}
