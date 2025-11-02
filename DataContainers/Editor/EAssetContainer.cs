using SimpleU.DataContainer;
using UnityEditor;
using UnityEngine;

namespace SimpleU.Editors.DataContainer
{
    public class EAssetContainer<T> : Editor where T : ScriptableObject
    {
        protected virtual SerializedProperty GetItemsProperty() => serializedObject.FindProperty("items");

        private EAssetContainerDrawer<T> _drawer;

        protected virtual void OnEnable()
        {
            _drawer ??= new EAssetContainerDrawer<T>(serializedObject, GetItemsProperty());
        }

        public override void OnInspectorGUI()
        {
            _drawer.DrawInspectorGUI();
        }
    }
}
