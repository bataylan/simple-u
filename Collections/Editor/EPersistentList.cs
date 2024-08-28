using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SimpleU.Collections
{
    [CustomPropertyDrawer(typeof(PersistentList<>))]
    public class EPersistentList : PropertyDrawer
    {
        private bool initialized = false;
        private ReorderableList _reorderableList;
        private SerializedProperty _property;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _property = property;

            if (!initialized)
                Initialize(property);

            EditorGUI.BeginChangeCheck();
            // property.serializedObject.Update();
            _reorderableList.DoList(position);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (_reorderableList != null)
                return _reorderableList.GetHeight();

            return base.GetPropertyHeight(property, label);
        }

        private void Initialize(SerializedProperty property)
        {
            initialized = true;

            var items = GetItemProperty(property);
            _reorderableList = new ReorderableList(property.serializedObject, items);
            _reorderableList.onAddCallback += OnAdd;
            // _reorderableList.drawElementCallback += OnDraw;
        }

        // private void OnDraw(Rect rect, int index, bool isActive, bool isFocused)
        // {
        //     var elementProperty = GetItemProperty(_property).GetArrayElementAtIndex(index);
        //     // var idPropery = elementProperty.FindPropertyRelative("id");
        //     // var valueProperty = elementProperty.FindPropertyRelative("value");
        //     // GUIContent idLabel = new GUIContent(idPropery.intValue.ToString());
        //     // EditorGUI.LabelField(rect, idLabel);
            
        //     EditorGUI.PropertyField(rect, elementProperty, true);
        // }

        private void OnAdd(ReorderableList list)
        {
            var currentIndexProperty = _property.FindPropertyRelative("_currentIndex");
            // currentIndexProperty.intValue++;
            Debug.Log("OnAdd CurrentIndex: " + currentIndexProperty.intValue);
            
            var persistentList = GetTargetObjectOfProperty(_property) as IList;
            persistentList.Add(default);
        }

        private SerializedProperty GetItemProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("_items");
        }

        /// <summary>
        /// Gets the object the property represents.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }

        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }

        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }
    }
}
