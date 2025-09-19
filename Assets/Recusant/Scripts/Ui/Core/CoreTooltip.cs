using UnityEngine;
using UnityEngine.UIElements;

namespace Recusant
{
    public class CoreTooltip : UiUnit
    {
        public static CoreTooltip Instance = null;

        //Create and hide a label in the UI Builder and name it "TooltipLabel". 
        //Set its position as 'Absolute'.
        //Set Shrink to 1, Grow to 1, Direction to 'Row', Wrap to 'Wrap', max width 
        //to 10 or 15% or however large you want your tooltip to be, max height to 'none', 
        //and then min height/width to whatever looks good.

        public VisualTreeAsset TooltipEntry;

        private Label _tooltipLabel;

        public override void Initialize()
        {
            // This is a temp intentional comment out of tooltip-related code
            if (Instance == null)
            {
                return;
            }

            var Document = GetComponent<UIDocument>();

            var NewEntry = TooltipEntry.Instantiate();
            Document.rootVisualElement.Add(NewEntry);
            _tooltipLabel = Document.rootVisualElement.Q<Label>("TooltipLabel");
            _tooltipLabel.visible = false;
        }

        public void Show(string tooltipText, Color color)
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            float mouseXPosition = Input.mousePosition.x;
            float mouseYPosition = Input.mousePosition.y;
            //The below code calculates the size of the text in the tooltip.
            //Yes, the 'Undefined' MeasureMode below is correct.
            _tooltipLabel.BringToFront();
            Vector2 textSize = _tooltipLabel.MeasureTextSize(tooltipText, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            //_tooltipLabel.visible = true;
            /*
            if (mouseXPosition >= screenWidth - (textSize.x))
            {
                //Keeps tooltip from getting offscreen to the right.
                //The code 'style.left = X' means that the left border of the tooltip element 
                //is X distance from the left side of the screen.
                _tooltipLabel.style.left = screenWidth - textSize.x - 5;
                //The -5 adds a small margin between the screen boundary and the tooltip.
            }
            else
            {
                _tooltipLabel.style.left = mouseXPosition;
            }
            if (mouseYPosition >= screenHeight - textSize.y)
            //Keeps tooltip from going offscreen to the top.
            {
                _tooltipLabel.style.top = screenHeight - mouseYPosition;
                //Style.top is the distance from the top of the 
                //screen to the top of the box.
                _tooltipLabel.style.bottom = mouseYPosition - textSize.y - 15;
                //style.bottom is the distance from the bottom of the screen to the bottom 
                //of the tooltip box.
                //The -15 is an offset to ensure the black background of the textbox 
                //is correctly placed.
            }
            else
            {
                _tooltipLabel.style.bottom = mouseYPosition;
                _tooltipLabel.style.top = screenHeight - mouseYPosition - textSize.y - 15;
                //The -15 is an additional buffer I needed. Change if need be.
            }
            */
            _tooltipLabel.style.left = mouseXPosition;
            _tooltipLabel.style.top = mouseYPosition;

            _tooltipLabel.style.color = color;
            _tooltipLabel.text = tooltipText;
        }

        public void Hide()
        {
            //_tooltipLabel.visible = false;
        }

        public override void Deinitialize()
        {

        }

        public override void Open()
        {

        }

        public override void Close()
        {

        }
    }
}
