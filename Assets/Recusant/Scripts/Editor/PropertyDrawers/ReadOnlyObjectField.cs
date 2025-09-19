
using UnityEditor;
using UnityEngine;

namespace Recusant.Editor
{
    public static class ReadonlyObjectField
    {
        private static readonly GUIStyle pickerButtonStyle = new GUIStyle("ObjectFieldButton");

        public static void Draw(Rect position, Object obj, GUIContent label, bool hidePicker, System.Action onPickerClicked = null)
        {
            if (label == null)
                label = GUIContent.none;

            // Begin property-like layout
            Rect totalRect = EditorGUI.PrefixLabel(position, label);

            // Split the rect: field area and button area (button is ~20px wide, like Unity)
            float buttonWidth = 20f;
            Rect fieldRect = new Rect(totalRect.x, totalRect.y, totalRect.width - buttonWidth, totalRect.height);
            Rect buttonRect = new Rect(totalRect.xMax - buttonWidth, totalRect.y, buttonWidth, totalRect.height);

            // Get object content (icon, name, tooltip)
            GUIContent content = EditorGUIUtility.ObjectContent(obj, obj != null ? obj.GetType() : typeof(UnityEngine.Object));

            // Draw the readonly field (as a label, no interaction)
            //GUI.enabled = false; // Temporarily disable to prevent interaction (grayed out)

            if (GUI.Button(fieldRect, content, EditorStyles.objectField))
            {
                EditorGUIUtility.PingObject(obj);
            }

            //GUI.enabled = true;

            if(hidePicker)
            {
                return;
            }

            Texture2D bgTexture = EditorStyles.objectField.normal.background;
            if (bgTexture != null)
            {
                //GUI.DrawTexture(buttonRect, bgTexture);
            }
            else
            {
                // Fallback: Use a solid color background matching the field's style
                //EditorGUI.DrawRect(buttonRect, EditorStyles.objectField.normal.textColor); // Or a gray color, e.g., Color.gray
            }

            // Draw the picker button
            if (GUI.Button(buttonRect, GUIContent.none, pickerButtonStyle))
            {
                // Run custom code when clicked (e.g., open picker)
                onPickerClicked?.Invoke();
            }
        }
    }
}