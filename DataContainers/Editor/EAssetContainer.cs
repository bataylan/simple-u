using SimpleU.DataContainer;
using UnityEditor;
using UnityEngine;

namespace SimpleU.Editors.DataContainer
{
    public class EAssetContainer<T> : Editor where T : ScriptableObject
    {
        protected virtual SerializedProperty GetItemsProperty() => serializedObject.FindProperty("items");

        private EAssetContainerDrawer<T> _drawer;

        protected virtual EAssetContainerDrawer<T> CreateDrawer()
        {
            return new EAssetContainerDrawer<T>(serializedObject, GetItemsProperty());
        }

        protected virtual void OnEnable()
        {
            _drawer ??= CreateDrawer();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            _drawer.DrawInspectorGUI();
        }
    }
}
