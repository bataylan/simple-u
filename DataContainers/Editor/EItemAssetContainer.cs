using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SimpleU.DataContainer
{
    [CustomEditor(typeof(ItemAssetContainer))]
    public class EItemAssetContainer : Editor
    {
        private ReorderableList _reorderableList;

        void OnEnable()
        {
            _reorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("items"));
            _reorderableList.multiSelect = true;
            _reorderableList.onAddCallback += OnAdd;
            _reorderableList.onRemoveCallback += OnRemove;
            _reorderableList.drawElementCallback += OnDrawElement;
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = serializedObject.FindProperty("items").GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element);
        }

        private void OnAdd(ReorderableList list)
        {
            Debug.Log("OnAdd");
            Undo.RecordObjects(targets, "add item asset element");
            AddItemAsset();
        }

        private void OnRemove(ReorderableList list)
        {
            Debug.Log("OnRemove");
            Undo.RecordObjects(targets, "remove item asset element");

            if (list.selectedIndices != null && list.selectedIndices.Count > 0)
            {
                for (int i = list.selectedIndices.Count - 1; i >= 0 ; i--)
                {
                    RemoveItemAssetAt(list.selectedIndices[i]);
                }
            }
            else
            {
                int lastElementIndex = list.count - 1;
                RemoveItemAssetAt(lastElementIndex);
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            serializedObject.Update();
            if (_reorderableList != null)
                _reorderableList.DoLayoutList();

            if (EditorGUI.EndChangeCheck())
            {
                var listProperty = serializedObject.FindProperty("items");
                listProperty.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
                AssetDatabase.SaveAssets();
            }
        }

        private void AddItemAsset()
        {
            var listProperty = serializedObject.FindProperty("items");

            ItemAsset itemAsset = null;
            string path = "";
            SerializedProperty element = null;
            var lastIndexProperty = serializedObject.FindProperty("lastIndex");
            int lastIndex = lastIndexProperty.intValue;

            try
            {
                itemAsset = CreateInstance<ItemAsset>();
                itemAsset.name = itemAsset.GetAssetName(lastIndex);
                path = AssetDatabase.GetAssetPath(serializedObject.targetObject);
                int index = listProperty.arraySize;
                listProperty.InsertArrayElementAtIndex(index);
                element = listProperty.GetArrayElementAtIndex(index);
            }
            finally
            {
                AssetDatabase.AddObjectToAsset(itemAsset, path);
                element.objectReferenceValue = itemAsset;
                lastIndexProperty.intValue++;
                EditorUtility.SetDirty(serializedObject.targetObject);
            }
        }

        private void RemoveItemAssetAt(int index)
        {
            var listProperty = serializedObject.FindProperty("items");
            ItemAsset itemAsset = null;
            SerializedProperty element = null;
            try
            {
                element = listProperty.GetArrayElementAtIndex(index);
                if (element == null)
                {
                    throw new Exception("Element not found!");
                }

                itemAsset = (ItemAsset)element.boxedValue;
                listProperty.DeleteArrayElementAtIndex(index);
            }
            finally
            {
                AssetDatabase.RemoveObjectFromAsset(itemAsset);
                EditorUtility.SetDirty(serializedObject.targetObject);
            }
        }
    }
}
