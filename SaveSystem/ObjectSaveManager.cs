
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
        
        /// <summary>
        /// serialized on scene instance or runtime instance
        /// </summary>
        [SerializeField] private string id;
        
        /// <summary>
        /// override manual id to prefab permanently
        /// </summary>
        [SerializeField] private bool isPermanentId;
        
        /// <summary>
        /// all save components on this object, auto filled in editor
        /// </summary>
        [SerializeField] private Component[] saveComponents;

        // Network players cannot resolve their persistent identity until after spawning.
        [SerializeField] private bool deferInitialization;

        private SaveManager _saveManager;

        public string Id => id;
        private bool _idValid = false;

        public bool IsScenePlaced { get; private set; }

        public const string CDestroyedInstancesKey = "destroyed-instances";

        void Awake()
        {
            _saveManager = SaveManager.Get();
            if (deferInitialization)
                return;

            //scene placed objects
            if (!_idValid && !string.IsNullOrEmpty(id))
            {
                //must be set before SetRuntimeId: this branch only runs when the serialized id
                //was already present before Awake, which only happens for scene-placed instances
                IsScenePlaced = true;
                SetRuntimeId(id);
            }

            if (!_idValid)
            {
                Debug.LogError("Id not valid! Don't load progress: " + gameObject.name);
                return;
            }

            if (IsMarkedDestroyed(id))
            {
                Destroy(gameObject);
                return;
            }

            EnsureRegisterToDictionary();
            LoadProgress(_saveManager);
        }

        public static bool IsMarkedDestroyed(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            return SaveManager.Get().TryReadData<bool>(CDestroyedInstancesKey, id, out var destroyed) && destroyed;
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
            if (deferInitialization)
                dict[id] = this; // A reconnect replaces the old, inactive player instance.
            else
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
            if ((deferInitialization && !_idValid) || saveComponents == null)
                return;

            for (int i = 0; i < saveComponents.Length; i++)
            {
                (saveComponents[i] as ISaveComponent).SaveProgress(manager);
            }
        }

        public void LoadProgress(SaveManager manager)
        {
            if ((deferInitialization && !_idValid) || saveComponents == null)
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

        /// <summary>Initializes a deferred object once its authoritative identity is known.</summary>
        public bool InitializeRuntimeId(string runtimeId)
        {
            if (!deferInitialization || string.IsNullOrEmpty(runtimeId))
                return false;

            if (GetById(runtimeId, out var existing) && existing != this && existing.isActiveAndEnabled)
            {
                Debug.LogError($"Save identity is already in use: {runtimeId}", this);
                return false;
            }

            SetRuntimeId(runtimeId);
            EnsureRegisterToDictionary();
            LoadProgress(_saveManager);
            return true;
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
                if (prefab)
                {
                    var assetPath = UnityEditor.AssetDatabase.GetAssetPath(prefab);
                    var loadedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
                    var prefabIdentifier = loadedPrefab.GetComponent<ObjectSaveManager>();

                    UpdatePrefab_Editor(prefabIdentifier);
                    UpdateInstance_Editor(prefabIdentifier);
                }
                else //not prefab, only game objects
                {
                    var instanceIdentifier = gameObject.GetComponent<ObjectSaveManager>();
                    UpdateComponentIds_Editor(instanceIdentifier);
                    UpdateInstance_Editor(null);
                }
            }
        }

        private static void UpdatePrefab_Editor(ObjectSaveManager prefabIdentifier)
        {
            UpdatePrefabId_Editor(prefabIdentifier);
            UpdateComponentIds_Editor(prefabIdentifier);
        }

        private static void UpdatePrefabId_Editor(ObjectSaveManager prefabIdentifier)
        {
            if (!prefabIdentifier)
                return;

            if (!string.IsNullOrEmpty(prefabIdentifier.id) && !prefabIdentifier.isPermanentId)
            {
                prefabIdentifier.id = "";
                UnityEditor.EditorUtility.SetDirty(prefabIdentifier.gameObject);
            }
        }

        private static void UpdateComponentIds_Editor(ObjectSaveManager prefabIdentifier)
        {
            if (!prefabIdentifier)
                return;

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
            bool isPrefabIdMatch = prefabIdentifier != null && !prefabIdentifier.isPermanentId
                && string.Equals(prefabIdentifier.Id, id);

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
