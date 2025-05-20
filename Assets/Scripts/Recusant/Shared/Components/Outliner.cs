using HighlightPlus;
using Netick.Unity;

namespace Recusant
{
    public class Outliner : NetworkBehaviour
    {
        protected HighlightEffect Highlight = null;

        public override void NetworkStart()
        {
            Highlight = GetComponentInChildren<HighlightEffect>(true);
        }
    }
}
