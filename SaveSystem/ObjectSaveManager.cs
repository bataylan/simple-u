
using System;
using System.Collections.Generic;
using SimpleU.Context;
using UnityEngine;

namespace SimpleU.SaveSystem
{
    //required negative execution order
    [DefaultExecutionOrder(-10)]
    public class ObjectSaveManager : MonoBehaviour, IObjectIdentifier
    {
        private const string CObject_Save_Dictionary = nameof(ObjectSaveManager) + "_dict";

        [SerializeField] private string id;
        [SerializeField] private bool isPermanentId;
        [SerializeField] private Component[] saveComponents;

        private SaveManager _saveManager;

        public string Id => id;
        private bool _idValid = false;

        void Awake()
        {
            _saveManager = SaveManager.Get();
            
            //scene placed objects
            if (!_idValid && !string.IsNullOrEmpty(id))
            {
                SetRuntimeId(id);
            }

            if (!_idValid)
            {
                Debug.LogError("Id not valid! Don't load progress: " + gameObject.name);
                return;
            }

            EnsureRegisterToDictionary();
            LoadProgress(_saveManager);
        }

        private void EnsureRegisterToDictionary()
        {
            var levelContext = LevelContext.Get();
            levelContext.ExtraData.TryGetExtra<Dictionary<string, ObjectSaveManager>>(CObject_Save_Dictionary, out var dict);
            if (dict == null)
            {
                dict = new Dictionary<string, ObjectSaveManager>();
                levelContext.ExtraData.SetExtra(CObject_Save_Dictionary, dict);
            }
            dict.Add(id, this);
        }

        public static bool GetById(string id, out ObjectSaveManager objectSaveManager)
        {
            objectSaveManager = null;

            if (!LevelContext.Get().ExtraData.TryGetExtra<Dictionary<string, ObjectSaveManager>>(CObject_Save_Dictionary, out var dict))
                return false;

            dict.TryGetValue(id, out objectSaveManager);
            return objectSaveManager != null;
        }

        public static bool GetComponentById(string objectId, string componentId, out ISaveComponent component)
        {
            component = null;

            if (!GetById(objectId, out var objectSave))
                return false;

            component = objectSave.GetComponentById(componentId);
            return component != null;
        }

        public ISaveComponent GetComponentById(string componentId)
        {
            for (int i = 0; i < saveComponents.Length; i++)
            {
                var comp = saveComponents[i] as ISaveComponent;
                if (comp.ComponentId.Equals(componentId))
                    return comp;
            }
            return null;
        }

        void OnEnable()
        {
            _saveManager.Subscribe(SaveProgress);
        }

        void OnDisable()
        {
            if (_saveManager != null)
                _saveManager.Unsubscribe(SaveProgress);
        }

        public void SaveProgress(SaveManager manager)
        {
            if (saveComponents == null)
                return;

            for (int i = 0; i < saveComponents.Length; i++)
            {
                (saveComponents[i] as ISaveComponent).SaveProgress(manager);
            }
        }

        public void LoadProgress(SaveManager manager)
        {
            if (saveComponents == null)
                return;

            for (int i = 0; i < saveComponents.Length; i++)
            {
                (saveComponents[i] as ISaveComponent).LoadProgress(manager);
            }
        }

        public void SetRuntimeId(string id)
        {
            this.id = id;
            _idValid = true;
        }

        void OnValidate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            //PrefabStage is not ready OnValidate, so wait to load prefab properly
            UnityEditor.EditorApplication.delayCall += ValidateComponents_Editor;
#endif
        }

#if UNITY_EDITOR

        private void ValidateComponents_Editor()
        {
            if (this == null) return; // Object might have been destroyed

            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            bool isEditingPrefabAsset = prefabStage != null && prefabStage.IsPartOfPrefabContents(gameObject);

            var prefabAssetType = UnityEditor.PrefabUtility.GetPrefabAssetType(gameObject);
            var prefabInstanceStatus = UnityEditor.PrefabUtility.GetPrefabInstanceStatus(gameObject);

            bool isPreviewPrefab = prefabAssetType != UnityEditor.PrefabAssetType.NotAPrefab &&
                         prefabInstanceStatus == UnityEditor.PrefabInstanceStatus.NotAPrefab;

            bool isPrefabSource = isEditingPrefabAsset || isPreviewPrefab;

            if (isPrefabSource)
            {
                UpdatePrefab_Editor(this);
            }
            else //scene instance
            {
                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                var assetPath = UnityEditor.AssetDatabase.GetAssetPath(prefab);
                var loadedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
                var prefabComponent = loadedPrefab.GetComponent<ObjectSaveManager>();

                UpdatePrefab_Editor(prefabComponent);
                UpdateInstance_Editor(prefabComponent);
            }
        }

        private static void UpdatePrefab_Editor(ObjectSaveManager prefabIdentifier)
        {
            if (!prefabIdentifier)
                return;

            if (!string.IsNullOrEmpty(prefabIdentifier.id) && !prefabIdentifier.isPermanentId)
            {
                prefabIdentifier.id = "";
                UnityEditor.EditorUtility.SetDirty(prefabIdentifier.gameObject);
            }

            var prefabSaveComponents = prefabIdentifier.gameObject.GetComponentsInChildren<ISaveComponent>();
            int cachedComponentLength = prefabIdentifier.saveComponents == null ? 0 : prefabIdentifier.saveComponents.Length;
            int foundComponentLength = prefabSaveComponents == null ? 0 : prefabSaveComponents.Length;
            bool hasComponentListChange = cachedComponentLength != foundComponentLength;

            if (prefabSaveComponents != null)
            {
                for (int i = 0; i < prefabSaveComponents.Length; i++)
                {
                    var saveComp = prefabSaveComponents[i];
                    if (!hasComponentListChange && saveComp is Component component
                        && prefabIdentifier.saveComponents[i] != component)
                    {
                        hasComponentListChange = true;
                    }

                    string componentId = saveComp.ComponentId;

                    if ((object)saveComp.Identifier != prefabIdentifier)
                    {
                        saveComp.Identifier = prefabIdentifier;
                        UnityEditor.EditorUtility.SetDirty(prefabIdentifier.gameObject);
                    }

                    if (string.IsNullOrEmpty(componentId))
                    {
                        componentId = GetNewId();
                        saveComp.ComponentId = componentId;
                        UnityEditor.EditorUtility.SetDirty(prefabIdentifier.gameObject);
                    }
                }
            }
            else
            {
                if (prefabIdentifier.saveComponents != null && prefabIdentifier.saveComponents.Length > 0)
                {
                    hasComponentListChange = true;
                }
            }

            if (hasComponentListChange)
            {
                prefabIdentifier.saveComponents = new Component[foundComponentLength];
                for (int i = 0; i < foundComponentLength; i++)
                {
                    prefabIdentifier.saveComponents[i] = prefabSaveComponents[i] as Component;
                }
                UnityEditor.EditorUtility.SetDirty(prefabIdentifier.gameObject);
            }

            UnityEditor.AssetDatabase.SaveAssetIfDirty(prefabIdentifier.gameObject);
        }

        private void UpdateInstance_Editor(ObjectSaveManager prefabIdentifier)
        {
            bool isPrefabIdMatch = prefabIdentifier != null && string.Equals(prefabIdentifier.Id, id);

            if (string.IsNullOrEmpty(id) || isPrefabIdMatch)
            {
                var so = new UnityEditor.SerializedObject(this);
                var prop = so.FindProperty(nameof(id));
                prop.stringValue = GetNewId();
                so.ApplyModifiedProperties();
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
        }
#endif

        public static string GetNewId()
        {
            return Guid.NewGuid().ToString();
        }
    }

    public interface IObjectIdentifier
    {
        /// <summary>
        /// writed on scene instance/instance
        /// </summary>
        public string Id { get; }
    }

    public interface ISaveComponent<T> : ISaveComponent
    {
    }

    public interface ISaveComponent
    {
        public IObjectIdentifier Identifier { get; set; }
        /// <summary>
        /// serialized on prefab
        /// </summary>
        public string ComponentId { get; set; }
        void SaveProgress(SaveManager manager);
        void LoadProgress(SaveManager manager);
    }
}
