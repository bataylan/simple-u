
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
        private const string CSaveFileNameKey = "save_file_name_k";

        public bool SaveInstantiating { get; set; }

        private Action<SaveManager> _saveTrigger;
        private SaveFileHandler _saveFileHandler;

        public static SaveManager Get()
        {
            if (LevelContext.Get().ExtraData.TryGetExtra(CKey, out SaveManager instance))
                return instance;

            instance = new SaveManager();
            LevelContext.Get().ExtraData.SetExtra(CKey, instance);
            
            return instance;
        }
        
        private static string GetFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, "Saves");
        }
        
        public static SaveFileHandler GetSaveFileHandler()
        {
            return new SaveFileHandler(GetFolderPath(), GetSaveFileName());
        }

        private static string GetSaveFileName()
        {
            return PlayerPrefs.GetString(CSaveFileNameKey, "progress");
        }
        
        private static string GetSaveFileFullPath()
        {
            return PlayerPrefs.GetString(CSaveFileNameKey, "progress") + ".dat";
        }

        private SaveManager()
        {
            SetFileHandler();
        }

        public void Subscribe(Action<SaveManager> onSave) { _saveTrigger += onSave; }
        public void Unsubscribe(Action<SaveManager> onSave) { _saveTrigger -= onSave; }

        public bool TrySetFileName(string fileName, bool copyLastSave)
        {
            if (copyLastSave)
            {
                bool saveFileCopied = _saveFileHandler.TryCopyFileToNewPath(fileName);
                if (!saveFileCopied)
                    return false;
            }

            PlayerPrefs.SetString(CSaveFileNameKey, fileName);
            
            _saveFileHandler.Dispose();
            _saveFileHandler = null;
            SetFileHandler();
            return true;
        }
        
        private void SetFileHandler()
        {
            string folderPath = GetFolderPath();
            string fileName = GetSaveFileName();
            _saveFileHandler = new SaveFileHandler(folderPath, fileName);
            Debug.Log("Save manager set with path: " + folderPath + " and file name: " + fileName);
        }
        
        public SaveFileInfo GetCurrentSaveFileInfo()
        {
            string folderPath = GetFolderPath();
            var fileInfo = new FileInfo(Path.Combine(folderPath, GetSaveFileFullPath()));
            return new SaveFileInfo
            {
                FileName = fileInfo.Name,
                FileFolder = fileInfo.DirectoryName,
                FilePath = fileInfo.FullName,
                LastWriteTime = fileInfo.LastWriteTime
            };
        }

        public List<SaveFileInfo> GetSaveFileInfos()
        {
            string folderPath = GetFolderPath();
            var filePaths = Directory.GetFiles(folderPath);
            var saveFileInfos = new List<SaveFileInfo>();
            foreach (var filePath in filePaths)
            {
                var fileInfo = new FileInfo(filePath);
                saveFileInfos.Add(new SaveFileInfo
                {
                    FileName = fileInfo.Name,
                    FileFolder = fileInfo.DirectoryName,
                    FilePath = filePath,
                    LastWriteTime = fileInfo.LastWriteTime
                });
            }

            return saveFileInfos;
        }

        public struct SaveFileInfo
        {
            public string FileName;
            public string FileFolder;
            public string FilePath;
            public DateTime LastWriteTime;
        }

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
