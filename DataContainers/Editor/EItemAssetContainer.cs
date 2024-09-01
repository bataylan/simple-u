using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SimpleU.DataContainer
{
    [CustomEditor(typeof(ItemAssetContainer))]
    public class EItemAssetContainer : EItemAssetContainer<ItemAsset>
    {
        protected override SerializedProperty itemsProperty => serializedObject.FindProperty("items");
        protected override SerializedProperty intIdentifierProperty => serializedObject.FindProperty("lastIndex");
    }

    public abstract class EItemAssetContainer<T> : Editor where T : ScriptableObject, IItemAsset
    {
        protected abstract SerializedProperty itemsProperty { get; }
        protected abstract SerializedProperty intIdentifierProperty { get; }

        protected ReorderableList _reorderableList;

        void OnEnable()
        {
            _reorderableList = new ReorderableList(serializedObject, itemsProperty);
            _reorderableList.multiSelect = true;
            _reorderableList.onAddCallback += OnAdd;
            _reorderableList.onRemoveCallback += OnRemove;
            _reorderableList.drawElementCallback += OnDrawElement;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            serializedObject.Update();
            if (_reorderableList != null)
                _reorderableList.DoLayoutList();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
                AssetDatabase.SaveAssets();
            }
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = itemsProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element);
        }

        protected virtual void OnAdd(ReorderableList list)
        {
            Undo.RecordObjects(targets, "add item asset element");
            AddItemAsset();
        }

        private void OnRemove(ReorderableList list)
        {
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

        private void AddItemAsset()
        {
            T itemAsset = null;
            string path = "";
            SerializedProperty element = null;
            int id = intIdentifierProperty.intValue;

            try
            {
                itemAsset = CreateInstance<T>();
                itemAsset.name = itemAsset.GetAssetName(id);
                path = AssetDatabase.GetAssetPath(serializedObject.targetObject);
                int index = itemsProperty.arraySize;
                itemsProperty.InsertArrayElementAtIndex(index);
                element = itemsProperty.GetArrayElementAtIndex(index);
            }
            finally
            {
                AssetDatabase.AddObjectToAsset(itemAsset, path);
                element.objectReferenceValue = itemAsset;
                intIdentifierProperty.intValue++;
                EditorUtility.SetDirty(serializedObject.targetObject);
            }
        }

        private void RemoveItemAssetAt(int index)
        {
            T itemAsset = null;

            try
            {
                var element = itemsProperty.GetArrayElementAtIndex(index);
                if (element == null)
                {
                    throw new Exception("Element not found!");
                }

                itemAsset = (T)element.boxedValue;
                itemsProperty.DeleteArrayElementAtIndex(index);
            }
            finally
            {
                AssetDatabase.RemoveObjectFromAsset(itemAsset);
                EditorUtility.SetDirty(serializedObject.targetObject);
            }
        }
    }
}
