using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SimpleU.DataContainer;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleU.Editors.DataContainer
{
    [CustomEditor(typeof(ItemAssetContainer))]
    public class EItemAssetContainer : Editor
    {
        protected virtual SerializedProperty itemsProperty => serializedObject.FindProperty("items");

        protected ReorderableList _reorderableList;
        private ItemAssetContainer _container;
        private UnityEvent<ItemAsset> addEvent;
        private UnityEvent<ItemAsset> removeEvent;
        private SerializedObject addEventSerialized;
        private ItemAsset _itemAssetToAdd;
        private Type[] _itemTypes;
        private Regex _regexItem;

        void OnEnable()
        {
            _container = (ItemAssetContainer)target;
            ValidateItemListMatchWithAssets(_container);
            CacheAddDropdownOptionTypes();
            _regexItem = new Regex("^[a-zA-Z0-9 _]*$");

            _reorderableList = new ReorderableList(serializedObject, itemsProperty);
            _reorderableList.multiSelect = true;
            // _reorderableList.onAddCallback += OnAdd;
            _reorderableList.onAddDropdownCallback += OnAddDropdown;
            _reorderableList.onRemoveCallback += OnRemove;
            _reorderableList.drawElementCallback += OnDrawElement;
            _reorderableList.onReorderCallback += OnReorder;
        }

        private void CacheAddDropdownOptionTypes()
        {
            _itemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(ItemAsset)) && !type.IsAbstract).ToArray();
            // alternative: .GetExportedTypes()
            // alternative: typeof(B).IsAssignableFrom(type)
            // alternative: => type.IsSubclassOf(typeof(B))
            // alternative: && type != typeof(B)
            // alternative: && ! type.IsAbstract
        }

        public static void ValidateItemListMatchWithAssets(ItemAssetContainer container)
        {
            var serializedObject = new SerializedObject(container);
            var itemsProperty = serializedObject.FindProperty("items");
            var assetPath = AssetDatabase.GetAssetPath(container);
            var allItemAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);

            for (int i = 0; i < allItemAssets.Length; i++)
            {
                var loadedItem = allItemAssets[i] as ItemAsset;
                if (loadedItem is not ItemAsset itemAsset)
                    continue;

                if (itemsProperty.arraySize > i)
                {
                    var serializedItem = GetItemAtIndex(itemsProperty, i);
                    if (serializedItem == loadedItem)
                    {
                        continue;
                    }
                    else
                    {
                        itemsProperty.DeleteArrayElementAtIndex(i);
                    }
                }

                itemsProperty.InsertArrayElementAtIndex(i);
                var element = itemsProperty.GetArrayElementAtIndex(i);
                element.objectReferenceValue = itemAsset;

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(container);

                Debug.Log("ItemContainer mismatch found: " + itemAsset.name + " and updated!");
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            serializedObject.Update();
            if (_reorderableList != null)
                _reorderableList.DoLayoutList();


            _itemAssetToAdd = EditorGUILayout.ObjectField(_itemAssetToAdd, typeof(ItemAsset), false) as ItemAsset;
            if (GUILayout.Button("Insert into Container"))
            {
                AddItemAsset(_itemAssetToAdd);
                _itemAssetToAdd = null;
            }

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

            string userUpdatedName = EditorGUI.TextField(rect, customItemName);

            if (EditorGUI.EndChangeCheck())
            {
                if (_regexItem.IsMatch(userUpdatedName))
                {
                    element.objectReferenceValue.name = GetFormattedItemName(index, userUpdatedName);
                    EditorUtility.SetDirty(element.objectReferenceValue);
                }
                else
                {
                    Debug.LogError("Please don't use special characters");
                }
            }
        }

        private void OnAddDropdown(Rect buttonRect, ReorderableList list)
        {
            var menu = new GenericMenu();
            for (int i = 0; i < _itemTypes.Length; i++)
            {
                var itemType = _itemTypes[i];
                menu.AddItem(new GUIContent(itemType.Name), false, AddDropdownClickHandler, itemType);
            }
            menu.ShowAsContext();
        }

        private void AddDropdownClickHandler(object selectedItemType)
        {
            var itemType = selectedItemType as Type;
            var itemAsset = ScriptableObject.CreateInstance(itemType);
            AddItemAsset(itemAsset as ItemAsset, false);
        }

        private void OnRemove(ReorderableList list)
        {
            Undo.RecordObjects(targets, "remove item asset element");

            try
            {
                if (list.selectedIndices != null && list.selectedIndices.Count > 0)
                {
                    for (int i = list.selectedIndices.Count - 1; i >= 0; i--)
                    {
                        RemoveFromMainAsset(itemsProperty, list.selectedIndices[i], true);
                    }
                }
                else
                {
                    int lastElementIndex = list.count - 1;
                    RemoveFromMainAsset(itemsProperty, lastElementIndex, true);
                }
            }
            catch (System.Exception e)
            {
                Undo.PerformUndo();
                throw new Exception(e.Message);
            }
        }

        private void OnReorder(ReorderableList list)
        {
            Undo.RecordObjects(targets, "reorder item asset container");
            try
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
            catch (System.Exception e)
            {
                Undo.PerformRedo();
                throw new Exception(e.Message);
            }
        }

        private void AddItemAsset(ItemAsset itemAsset, bool checkParent = true)
        {
            Undo.RecordObjects(targets, "add item asset element");

            try
            {
                if (checkParent)
                {
                    CheckRemoveFromParent(itemAsset);
                }

                int index = itemsProperty.arraySize;
                if (string.IsNullOrEmpty(itemAsset.name))
                {
                    itemAsset.name = GetFormattedItemName(index, itemAsset.name);
                }
                else
                {
                    string itemName = GetItemNameFromFormattedName(itemAsset.name);
                    string correctAssetName = GetFormattedItemName(index, itemName);
                    itemAsset.name = correctAssetName;
                }

                var path = AssetDatabase.GetAssetPath(serializedObject.targetObject);
                AssetDatabase.AddObjectToAsset(itemAsset, path);
                itemsProperty.InsertArrayElementAtIndex(index);
                var element = itemsProperty.GetArrayElementAtIndex(index);
                element.objectReferenceValue = itemAsset;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_container);
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Undo.PerformUndo();
                throw new Exception(e.Message);
            }
        }

        private static void CheckRemoveFromParent(ItemAsset toRemoveItem)
        {
            var otherItemAssetPath = AssetDatabase.GetAssetPath(toRemoveItem);
            var otherItemAssetContainer = AssetDatabase.LoadAssetAtPath<ItemAssetContainer>(otherItemAssetPath);
            if (otherItemAssetContainer == null)
                return;

            var otherSerialized = new SerializedObject(otherItemAssetContainer);
            var otherItems = otherSerialized.FindProperty("items");
            if (!otherItems.isArray)
            {
                throw new Exception("ItemContainer remove source items not found!");
            }

            for (int i = 0; i < otherItems.arraySize; i++)
            {
                var item = GetItemAtIndex(otherItems, i);
                if (item == toRemoveItem)
                {
                    RemoveFromMainAsset(otherItems, i);
                    break;
                }
            }
        }

        private static void RemoveFromMainAsset(SerializedProperty itemsProperty, int index, bool delete = false)
        {
            ItemAsset itemAsset = null;

            try
            {
                itemAsset = GetItemAtIndex(itemsProperty, index);
                itemsProperty.DeleteArrayElementAtIndex(index);
                itemsProperty.serializedObject.ApplyModifiedProperties();
            }
            finally
            {
                AssetDatabase.RemoveObjectFromAsset(itemAsset);
                if (delete)
                {
                    var path = AssetDatabase.GetAssetPath(itemAsset);
                    AssetDatabase.DeleteAsset(path);
                }
                EditorUtility.SetDirty(itemsProperty.serializedObject.targetObject);
            }
        }

        private static ItemAsset GetItemAtIndex(SerializedProperty itemsProperty, int index)
        {
            ItemAsset itemAsset;
            var element = itemsProperty.GetArrayElementAtIndex(index);
            if (element == null)
            {
                throw new Exception("Element not found!");
            }

            itemAsset = (ItemAsset)element.boxedValue;
            return itemAsset;
        }

        private string GetFormattedItemName(int index, string name)
        {
            string prefix = _container.GetPrefix(index);
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                name = DefaultItemName;
            }

            return prefix + "-" + name;
        }

        private string DefaultItemName => typeof(ItemAsset).Name;

        private string GetItemNameFromFormattedName(string itemName)
        {
            return itemName.Split("-")[1];
        }
    }
}
