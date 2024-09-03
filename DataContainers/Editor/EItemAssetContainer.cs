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
    }

    public abstract class EItemAssetContainer<T> : Editor where T : ScriptableObject, IItemAsset
    {
        protected abstract SerializedProperty itemsProperty { get; }

        protected ReorderableList _reorderableList;
        private ItemAssetContainer<T> _itemAssetContainer;

        void OnEnable()
        {
            _itemAssetContainer = target as ItemAssetContainer<T>;
            _reorderableList = new ReorderableList(serializedObject, itemsProperty);
            _reorderableList.multiSelect = true;
            _reorderableList.onAddCallback += OnAdd;
            _reorderableList.onRemoveCallback += OnRemove;
            _reorderableList.drawElementCallback += OnDrawElement;
            _reorderableList.onReorderCallback += OnReorder;
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

            var itemAsset = element.objectReferenceValue as ScriptableObject;
            string itemAssetName = itemAsset.name;
            string customItemName = "";

            try
            {
                customItemName = GetItemNameFromFormattedName(itemAssetName);
            }
            catch (Exception)
            {
                string formattedDefaultName = GetFormattedItemName(index, null);
                element.objectReferenceValue.name = GetFormattedItemName(index, null);
                customItemName = DefaultItemName;
                Debug.LogError($"Not valid item name! Reformatted from {itemAssetName} to {formattedDefaultName}");
            }

            EditorGUI.BeginChangeCheck();

            customItemName = EditorGUI.TextField(rect, customItemName);

            if (EditorGUI.EndChangeCheck())
            {
                element.objectReferenceValue.name = GetFormattedItemName(index, customItemName);
                EditorUtility.SetDirty(element.objectReferenceValue);
            }
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

        private void OnReorder(ReorderableList list)
        {
            for (int i = 0; i < itemsProperty.arraySize; i++)
            {
                var element = itemsProperty.GetArrayElementAtIndex(i);
                var assetName = element.objectReferenceValue.name;
                string itemName = GetItemNameFromFormattedName(assetName);
                string correctAssetName = GetFormattedItemName(i, itemName);

                if (!string.Equals(assetName, correctAssetName))
                {
                    element.objectReferenceValue.name = correctAssetName;
                    EditorUtility.SetDirty(element.objectReferenceValue);
                }
            }
        }

        private void AddItemAsset()
        {
            T itemAsset = null;
            string path = "";
            SerializedProperty element = null;

            try
            {
                itemAsset = CreateInstance<T>();
                int index = itemsProperty.arraySize;
                itemAsset.name = GetFormattedItemName(index, null);
                path = AssetDatabase.GetAssetPath(serializedObject.targetObject);
                itemsProperty.InsertArrayElementAtIndex(index);
                element = itemsProperty.GetArrayElementAtIndex(index);
            }
            finally
            {
                AssetDatabase.AddObjectToAsset(itemAsset, path);
                element.objectReferenceValue = itemAsset;
                EditorUtility.SetDirty(serializedObject.targetObject);
            }
        }

        private string GetFormattedItemName(int index, string name)
        {
            string prefix = _itemAssetContainer.GetPrefix(index);
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                name = DefaultItemName;
            }

            return prefix + "-" + name;
        }

        private string DefaultItemName => typeof(T).Name;

        private string GetItemNameFromFormattedName(string itemName)
        {
            return itemName.Split("-")[1];
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
