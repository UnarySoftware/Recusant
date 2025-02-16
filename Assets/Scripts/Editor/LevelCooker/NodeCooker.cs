using UnityEngine;

public class NodeCooker : Cooker
{
    public override void Cook()
    {
        if(NodeManager.Instance != null)
        {
            NodeManager.Instance.UpdateNodes();
        }
    }
}
