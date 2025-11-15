using UnityEngine;

namespace Unary.Core
{
    [CreateAssetMenu(fileName = nameof(UiStateAsset), menuName = "Core/Data/" + nameof(UiStateAsset))]
    public class UiStateAsset : BaseScriptableObject
    {
        public PrefabRef<UiState> State;
    }
}
