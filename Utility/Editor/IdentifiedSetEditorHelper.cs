using UnityEditor;

namespace SimpleU.Utility
{
    public static class IdentifiedSetEditorHelper
    {
        public static void DrawIdentifiedSet<K>(IdentifiedSet<K> items) where K : UnityEngine.Object
        {
            EditorGUILayout.LabelField("Read only items");
            for (int i = 0; i < items.Count; i++)
            {
                items.TryGetItemAndIdByIndex(i, out var itemIdPair);
                EditorGUILayout.ObjectField(itemIdPair.Key.ToString(), itemIdPair.Value, typeof(K), false);
            }
        }
    }
}
