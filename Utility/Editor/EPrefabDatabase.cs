using UnityEditor;
using UnityEngine;

namespace SimpleU.Utility
{
    [CustomEditor(typeof(EPrefabDatabase<>))]
    public class EPrefabDatabase<T> : Editor where T : Component
    {
        private PrefabDatabase<T> _target;

        void OnEnable()
        {
            _target = (PrefabDatabase<T>)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUI.BeginChangeCheck();

            if (GUILayout.Button("Fetch all items"))
            {
                _target.FetchAllItems();
            }

            IdentifiedSetEditorHelper.DrawIdentifiedSet(_target.prefabs);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
                AssetDatabase.SaveAssets();
            }
        }


    }
}
