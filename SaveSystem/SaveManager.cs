
using System;
using System.Collections.Generic;
using System.IO;
using SimpleU.Context;
using UnityEngine;

namespace SimpleU.SaveSystem
{
    public class SaveManager
    {
        private const string CKey = nameof(SaveManager);
        public bool SaveInstantiating { get; set; }

        private Action<SaveManager> _saveTrigger;
        private SaveFileHandler _saveFileHandler;

        public static SaveManager Get()
        {
            if (LevelContext.Get().ExtraData.TryGetExtra(CKey, out SaveManager instance))
                return instance;

            string folderPath = GetFolderPath();
            instance = new SaveManager(folderPath);
            LevelContext.Get().ExtraData.SetExtra(CKey, instance);
            Debug.Log("Save manager set with path: " + folderPath);
            return instance;
        }
        
        private static string GetFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, "Saves");
        }
        
        public static SaveFileHandler GetSaveFileHandler()
        {
            return new SaveFileHandler(GetFolderPath(), "progress");
        }

        public SaveManager(string folderPath)
        {
            _saveFileHandler = new SaveFileHandler(folderPath, "progress");
        }

        public void Subscribe(Action<SaveManager> onSave) { _saveTrigger += onSave; }
        public void Unsubscribe(Action<SaveManager> onSave) { _saveTrigger -= onSave; }

        public void SaveAll()
        {
            _saveTrigger?.Invoke(this);
            _saveFileHandler.WriteSaveFile();
        }

        public void SaveData(string instanceId, string componentId, object saveData)
            => _saveFileHandler.AddData(instanceId, componentId, saveData);
        
        public void DeleteData(string instanceId)
            => _saveFileHandler.DeleteData(instanceId);
            
        public void DeleteData(string instanceId, string componentId)
            => _saveFileHandler.DeleteData(instanceId, componentId);
        
            
        public bool TryReadData<T>(string instanceId, string componentId, out T saveData) where T : struct
        {
            var data = _saveFileHandler.ReadComponentData<T>(instanceId, componentId) as T?;
            saveData = data.HasValue ? data.Value : default(T);
            return data.HasValue;
        }
        
        
        public bool TryReadObjectData<T>(string instanceId, out List<T?> saveData) where T : struct
        {
            saveData = _saveFileHandler.ReadObjectData<T>(instanceId);
            return saveData != null;
        }
        
        public void DeleteSaveFile() => _saveFileHandler.DeleteSave();
    }
}
