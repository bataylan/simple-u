using SimpleU.DataContainer;
using UnityEditor;

namespace SimpleU.Editors.DataContainer
{
    [CustomEditor(typeof(ItemAssetContainer), true)]
    public class EItemAssetContainer : EAssetContainer<ItemAsset>
    {
    }
}
