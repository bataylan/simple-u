using System;
using System.Linq;
using System.Text.RegularExpressions;
using SimpleU.DataContainer;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;

namespace SimpleU.Editors.DataContainer
{
    public class EAssetContainerDrawer<T> where T : ScriptableObject
    {
        protected ReorderableList _reorderableList;
        private string _prefix;
        private T _itemAssetToAdd;
        private Type[] _itemTypes;
        private Regex _regexItem;
        private SerializedObject _serializedObject;
        private SerializedProperty _arrayProperty;
        private int _selectedIndexForRename;

        public EAssetContainerDrawer(SerializedObject source, SerializedProperty arrayProperty,
            string prefix = null)
        {
            if (source.targetObject is not ScriptableObject)
            {
                Debug.LogError("Can't be used on other types than scriptable objects");
                return;
            }

            _serializedObject = source;
            _arrayProperty = arrayProperty;

            EnsurePrefix(prefix);

            ValidateItemListMatchWithAssets();
            CacheAddDropdownOptionTypes();
            _regexItem = new Regex("^[a-zA-Z0-9 _]*$");

            _reorderableList = new ReorderableList(_serializedObject, _arrayProperty);
            _reorderableList.multiSelect = true;
            // _reorderableList.onAddCallback += OnAdd;
            _reorderableList.onAddDropdownCallback += OnAddDropdown;
            _reorderableList.onRemoveCallback += OnRemove;
            _reorderableList.drawElementCallback += OnDrawElement;
            _reorderableList.onReorderCallback += OnReorder;
        }

        private void EnsurePrefix(string prefix)
        {
            string name = _serializedObject.targetObject.name;
            if (string.IsNullOrEmpty(name))
            {
                name = typeof(T).Name;
            }

            _prefix = prefix;
            if (string.IsNullOrEmpty(prefix))
            {
                _prefix = new string(name.Where(x => char.IsUpper(x)).ToArray());

                if (string.IsNullOrEmpty(_prefix))
                {
                    _prefix = name.ToString();
                }
            }

            if (!string.IsNullOrEmpty(_prefix))
                _prefix += "-";
        }

        private void CacheAddDropdownOptionTypes()
        {
            var assetType = typeof(T);
            _itemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => !type.IsAbstract && (type.Equals(assetType) || type.IsSubclassOf(assetType))).ToArray();
        }

        public void ValidateItemListMatchWithAssets()
        {
            var assetPath = AssetDatabase.GetAssetPath(_serializedObject.targetObject);
            var allItemAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);

            for (int i = 0; i < allItemAssets.Length; i++)
            {
                var loadedItem = allItemAssets[i] as T;
                if (loadedItem is not T itemAsset)
                    continue;

                if (_arrayProperty.arraySize > i)
                {
                    var serializedItem = GetItemAtIndex(_arrayProperty, i);
                    if (serializedItem == loadedItem)
                    {
                        continue;
                    }
                    else
                    {
                        _arrayProperty.DeleteArrayElementAtIndex(i);
                    }
                }

                _arrayProperty.InsertArrayElementAtIndex(i);
                var element = _arrayProperty.GetArrayElementAtIndex(i);
                element.objectReferenceValue = itemAsset;

                _serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_serializedObject.targetObject);

                Debug.Log("ItemContainer mismatch found: " + itemAsset.name + " and updated!");
            }
        }

        public void DrawInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            _serializedObject.Update();
            if (_reorderableList != null)
                _reorderableList.DoLayoutList();

            _itemAssetToAdd = EditorGUILayout.ObjectField(_itemAssetToAdd, typeof(T), false) as T;
            if (GUILayout.Button("Insert into Container"))
            {
                AddItemAsset(_itemAssetToAdd);
                _itemAssetToAdd = null;
            }

            if (_reorderableList.selectedIndices != null
                && _reorderableList.selectedIndices.Count == 1
                && GUILayout.Button("Rename selected"))
            {
                _selectedIndexForRename = _reorderableList.selectedIndices[0];
                var element = _arrayProperty.GetArrayElementAtIndex(_selectedIndexForRename);
                if (element != null)
                {
                    string customItemName = GetCustomItemName(element, _selectedIndexForRename);
                    UpdateStringWindow.GetWindowForName(customItemName, ReceiveItemName);
                }
            }

            if (_reorderableList.selectedIndices != null && _reorderableList.selectedIndices.Count > 0
                && GUILayout.Button("Reformat selected asset names"))
            {
                for (int i = 0; i < _reorderableList.selectedIndices.Count; i++)
                {
                    ReformatSelectedName(i);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                _serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_serializedObject.targetObject);
                AssetDatabase.SaveAssets();
            }
        }

        private bool ReformatSelectedName(int index)
        {
            _selectedIndexForRename = _reorderableList.selectedIndices[index];
            var element = _arrayProperty.GetArrayElementAtIndex(_selectedIndexForRename);
            if (element == null)
                return false;

            string customItemName = GetCustomItemName(element, _selectedIndexForRename);
            ReceiveItemName(customItemName);
            return true;
        }

        private void ReceiveItemName(string customItemName)
        {
            var element = _arrayProperty.GetArrayElementAtIndex(_selectedIndexForRename);
            element.objectReferenceValue.name = GetFormattedItemName(_selectedIndexForRename, customItemName);
            EditorUtility.SetDirty(_serializedObject.targetObject);
            AssetDatabase.SaveAssetIfDirty(_serializedObject.targetObject);
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _arrayProperty.GetArrayElementAtIndex(index);

            var itemAsset = element.objectReferenceValue as ScriptableObject;
            if (!itemAsset)
            {
                Debug.LogError($"ItemAsset reference not found and deleted at index: {index}");
                RemoveFromMainAsset(_arrayProperty, index, true);
                return;
            }

            string customItemName = GetCustomItemName(element, index);
            EditorGUI.LabelField(rect, customItemName);
        }

        private string GetCustomItemName(SerializedProperty element, int index)
        {
            string itemAssetName = element.objectReferenceValue.name;
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

            return customItemName;
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
            AddItemAsset(itemAsset as T, false);
        }

        private void OnRemove(ReorderableList list)
        {
            Undo.RecordObjects(_serializedObject.targetObjects, "remove item asset element");

            try
            {
                if (list.selectedIndices != null && list.selectedIndices.Count > 0)
                {
                    for (int i = list.selectedIndices.Count - 1; i >= 0; i--)
                    {
                        RemoveFromMainAsset(_arrayProperty, list.selectedIndices[i], true);
                    }
                }
                else
                {
                    int lastElementIndex = list.count - 1;
                    RemoveFromMainAsset(_arrayProperty, lastElementIndex, true);
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
            Undo.RecordObjects(_serializedObject.targetObjects, "reorder item asset container");
            try
            {
                for (int i = 0; i < _arrayProperty.arraySize; i++)
                {
                    var element = _arrayProperty.GetArrayElementAtIndex(i);
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

        private void AddItemAsset(T itemAsset, bool checkParent = true)
        {
            Undo.RecordObjects(_serializedObject.targetObjects, "add item asset element");

            try
            {
                if (checkParent)
                {
                    CheckRemoveFromParent(itemAsset);
                }

                int index = _arrayProperty.arraySize;
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

                var path = AssetDatabase.GetAssetPath(_serializedObject.targetObject);
                AssetDatabase.AddObjectToAsset(itemAsset, _serializedObject.targetObject);
                _arrayProperty.InsertArrayElementAtIndex(index);
                var element = _arrayProperty.GetArrayElementAtIndex(index);
                element.objectReferenceValue = itemAsset;
                _serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_serializedObject.targetObject);
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Undo.PerformUndo();
                throw new Exception(e.Message);
            }
        }

        private static void CheckRemoveFromParent(T toRemoveItem)
        {
            var otherItemAssetPath = AssetDatabase.GetAssetPath(toRemoveItem);
            var otherItemAssetContainer = AssetDatabase.LoadAssetAtPath<AssetContainer>(otherItemAssetPath);
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
            T itemAsset = null;

            try
            {
                itemAsset = GetItemAtIndex(itemsProperty, index);
                itemsProperty.DeleteArrayElementAtIndex(index);
                itemsProperty.serializedObject.ApplyModifiedProperties();
            }
            finally
            {
                if (itemAsset)
                {
                    AssetDatabase.RemoveObjectFromAsset(itemAsset);
                    if (delete)
                    {
                        var path = AssetDatabase.GetAssetPath(itemAsset);
                        AssetDatabase.DeleteAsset(path);
                    }
                }
                EditorUtility.SetDirty(itemsProperty.serializedObject.targetObject);
            }
        }

        private static T GetItemAtIndex(SerializedProperty itemsProperty, int index)
        {
            T itemAsset;
            var element = itemsProperty.GetArrayElementAtIndex(index);
            if (element == null)
            {
                throw new Exception("Element not found!");
            }

            itemAsset = (T)element.boxedValue;
            return itemAsset;
        }

        private string GetFormattedItemName(int index, string name)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                name = DefaultItemName;
            }

            return $"{index}-{_prefix}{name}";
        }

        private string DefaultItemName => typeof(T).Name;

        private string GetItemNameFromFormattedName(string itemName)
        {
            return itemName.Split("-")[^1];
        }
    }

    public class UpdateStringWindow : EditorWindow
    {
        private Action<string> onComplete;
        private string currentName;
        private Regex regexItem;

        public static UpdateStringWindow GetWindowForName(string name, Action<string> onComplete)
        {
            var window = GetWindow<UpdateStringWindow>(nameof(UpdateStringWindow));
            window.currentName = name;
            window.regexItem = new Regex("^[a-zA-Z0-9 _]*$");
            window.onComplete = onComplete;
            return window;
        }

        private void OnGUI()
        {
            string userUpdatedName = EditorGUILayout.TextField(currentName);
            if (regexItem.IsMatch(userUpdatedName))
            {
                currentName = userUpdatedName;
            }
            else
            {
                Debug.LogError("Please don't use special characters");
            }

            if (GUILayout.Button("Update"))
            {
                var temp = onComplete;
                onComplete = null;
                temp.Invoke(currentName);
                Close();
            }
        }
    }
}
