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

            _prefix = GetNamePrefix(source.targetObject.name, prefix);

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

        private static string GetNamePrefix(string containerName, string prefix = null)
        {
            if (string.IsNullOrEmpty(containerName))
                containerName = typeof(T).Name;

            if (string.IsNullOrEmpty(prefix))
            {
                prefix = new string(containerName.Where(char.IsUpper).ToArray());
                if (string.IsNullOrEmpty(prefix))
                    prefix = containerName;
            }

            return prefix + "-";
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
            // Item containers can hold references to subassets owned by other files.
            if (_serializedObject.targetObject is ItemAssetContainer)
                return;

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

        public virtual void DrawInspectorGUI()
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

            if (EditorGUI.EndChangeCheck())
            {
                _serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_serializedObject.targetObject);
                AssetDatabase.SaveAssets();
            }
        }

        protected void ShowSetNameEditor(T item)
        {
            var container = _serializedObject.targetObject;
            var itemsPath = _arrayProperty.propertyPath;
            UpdateStringWindow.GetWindowForName(GetItemNameFromFormattedName(item.name), customName =>
            {
                if (!container || !item)
                    return;

                using var containerProperties = new SerializedObject(container);
                var items = containerProperties.FindProperty(itemsPath);
                for (int index = 0; index < items.arraySize; index++)
                {
                    if (GetItemAtIndex(items, index) != item)
                        continue;

                    SetItemName(item, index, customName);
                    break;
                }
            });
        }

        protected virtual void SetItemName(T item, int index, string customItemName)
        {
            RenameAsset(item, GetFormattedItemName(index, customItemName, _prefix));
        }

        private static void RenameAsset(ScriptableObject item, string formattedName)
        {
            if (item.name == formattedName)
                return;

            Undo.RecordObject(item, "Rename item");
            item.name = formattedName;
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssetIfDirty(item);
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var item = GetItemAtIndex(_arrayProperty, index);
            if (!item && _serializedObject.targetObject is not ItemAssetContainer)
            {
                Debug.LogError($"ItemAsset reference not found and deleted at index: {index}");
                RemoveFromMainAsset(_arrayProperty, index, true);
                return;
            }

            DrawItemRow(rect, item, index, isActive, isFocused);
        }

        protected virtual void DrawItemRow(Rect rect, T item, int index, bool isActive, bool isFocused)
        {
            const float buttonWidth = 68f;
            const float spacing = 4f;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            rect.y += (rect.height - lineHeight) / 2f;
            rect.height = lineHeight;

            float nameWidth = Mathf.Min(buttonWidth, rect.width);
            var buttonRect = new Rect(rect.xMax - nameWidth, rect.y, nameWidth, lineHeight);
            rect.width = Mathf.Max(0f, rect.width - nameWidth - spacing);

            DrawItemObjectField(rect, item);
            DrawSetNameButton(buttonRect, item);
        }

        protected virtual void DrawItemObjectField(Rect rect, T item)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.ObjectField(rect, item, typeof(T), false);
        }

        protected virtual void DrawSetNameButton(Rect rect, T item)
        {
            using (new EditorGUI.DisabledScope(!item))
            {
                if (GUI.Button(rect, "Set Name", EditorStyles.miniButton))
                    ShowSetNameEditor(item);
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
            AddItemAsset(itemAsset as T, false);
        }

        private void OnRemove(ReorderableList list)
        {
            Undo.RecordObjects(_serializedObject.targetObjects, "remove item asset element");

            try
            {
                if (list.selectedIndices != null && list.selectedIndices.Count > 0)
                {
                    foreach (var index in list.selectedIndices.OrderByDescending(index => index).ToArray())
                        RemoveFromMainAsset(_arrayProperty, index, true);
                }
                else
                {
                    int lastElementIndex = list.count - 1;
                    RemoveFromMainAsset(_arrayProperty, lastElementIndex, true);
                }

                ReformatItemNames();
            }
            catch (System.Exception e)
            {
                Undo.PerformUndo();
                throw new Exception(e.Message);
            }
        }

        private void OnReorder(ReorderableList list)
        {
            _serializedObject.ApplyModifiedProperties();
            ReformatItemNames();
            AssetDatabase.SaveAssetIfDirty(_serializedObject.targetObject);
        }

        private void ReformatItemNames()
        {
            for (int index = 0; index < _arrayProperty.arraySize; index++)
            {
                var item = GetItemAtIndex(_arrayProperty, index);
                if (item)
                    SetItemName(item, index, GetItemNameFromFormattedName(item.name));
            }
        }

        private void AddItemAsset(T itemAsset, bool checkParent = true)
        {
            if (!itemAsset)
                return;

            if (checkParent && itemAsset is ItemAsset item && _serializedObject.targetObject is ItemAssetContainer destination)
            {
                _serializedObject.ApplyModifiedProperties();
                InsertItemReference(item, destination);
                return;
            }

            Undo.RecordObjects(_serializedObject.targetObjects, "add item asset element");

            try
            {
                if (checkParent)
                {
                    CheckRemoveFromParent(itemAsset);
                }

                int index = _arrayProperty.arraySize;
                AssetDatabase.AddObjectToAsset(itemAsset, _serializedObject.targetObject);
                _arrayProperty.InsertArrayElementAtIndex(index);
                var element = _arrayProperty.GetArrayElementAtIndex(index);
                element.objectReferenceValue = itemAsset;
                _serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_serializedObject.targetObject);
                SetItemName(itemAsset, index, GetItemNameFromFormattedName(itemAsset.name));
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Undo.PerformUndo();
                throw new Exception(e.Message);
            }
        }

        private void InsertItemReference(ItemAsset item, ItemAssetContainer destination)
        {
            if (destination.Items.Contains(item))
                return;

            var sources = AssetDatabase.FindAssets($"t:{nameof(ItemAssetContainer)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemAssetContainer>)
                .Where(source => source && source != destination && source.Items.Contains(item))
                .ToArray();

            if (sources.Length <= 1)
            {
                MoveItemReference(item, destination, sources.FirstOrDefault());
                return;
            }

            var menu = new GenericMenu();
            foreach (var source in sources)
                menu.AddItem(new GUIContent($"Move from {source.name}"), false, () => MoveItemReference(item, destination, source));
            menu.ShowAsContext();
        }

        private void MoveItemReference(ItemAsset item, ItemAssetContainer destination, ItemAssetContainer source)
        {
            if (!item || !destination || destination.Items.Contains(item))
                return;

            // Keep the original asset file and local ID so serialized references keep resolving.
            Undo.RecordObject(destination, "Move item between containers");
            int destinationIndex = destination.Items.Count;
            destination.Items.Add(item);
            EditorUtility.SetDirty(destination);
            AssetDatabase.SaveAssetIfDirty(destination);

            int sourceIndex = source ? source.Items.IndexOf(item) : -1;
            if (sourceIndex >= 0)
            {
                Undo.RecordObject(source, "Move item between containers");
                source.Items.RemoveAt(sourceIndex);
                EditorUtility.SetDirty(source);
                AssetDatabase.SaveAssetIfDirty(source);

                var sourcePrefix = GetNamePrefix(source.name);
                for (int index = sourceIndex; index < source.Items.Count; index++)
                {
                    var remainingItem = source.Items[index];
                    if (!remainingItem)
                        continue;

                    string name = remainingItem.name;
                    int separator = name.IndexOf('-');
                    string updatedName = separator > 0 && int.TryParse(name[..separator], out _)
                        ? $"{index}{name[separator..]}"
                        : GetFormattedItemName(index, name, sourcePrefix);
                    RenameAsset(remainingItem, updatedName);
                }
            }

            SetItemName(item as T, destinationIndex, GetItemNameFromFormattedName(item.name));
            _serializedObject.Update();
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
            if (itemsProperty.serializedObject.targetObject is ItemAssetContainer container)
            {
                itemsProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
                itemsProperty.DeleteArrayElementAtIndex(index);
                itemsProperty.serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(container);
                return;
            }

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

        private static string GetFormattedItemName(int index, string name, string prefix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                name = DefaultItemName;
            }

            return $"{index}-{prefix}{name}";
        }

        private static string DefaultItemName => typeof(T).Name;

        private static string GetItemNameFromFormattedName(string itemName)
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
