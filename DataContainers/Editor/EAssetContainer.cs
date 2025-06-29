using SimpleU.DataContainer;
using UnityEditor;
using UnityEngine;

namespace SimpleU.Editors.DataContainer
{
    [CustomEditor(typeof(AssetContainer))]
    public class EAssetContainer : EAssetContainer<ScriptableObject>
    {

    }

    public class EAssetContainer<T> : Editor where T : ScriptableObject
    {
        protected virtual SerializedProperty GetItemsProperty() => serializedObject.FindProperty("items");

        private EAssetContainerDrawer<T> _drawer;

        void OnEnable()
        {
            _drawer ??= new EAssetContainerDrawer<T>(serializedObject, GetItemsProperty());
        }

        public override void OnInspectorGUI()
        {
            _drawer.DrawInspectorGUI();
        }
    }
}
