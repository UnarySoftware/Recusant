using HighlightPlus;
using Unary.Core;

namespace Unary.Recusant
{
    public class Outliner : NetworkBehaviourExtended
    {
        protected HighlightEffect Highlight = null;

        public override void NetworkStart()
        {
            Highlight = GetComponentInChildren<HighlightEffect>(true);
        }
    }
}
