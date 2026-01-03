using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace SimpleU.SaveSystem
{
    /// <summary>
    /// namespace changes broke save data since type parsed from save
    /// lists are supported
    /// </summary>
    public class SaveFileHandler : IDisposable
    {
        private const string CFileExtension = ".dat";

        private string _filePath;
        private Dictionary<string, InstanceSave> _instanceSaves;
        private string _folderPath;

        public static JsonSerializerSettings JsonSerializerSettings { get; private set; }

        public SaveFileHandler(string folderPath, string fileName)
        {
            _folderPath = folderPath;
            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
                Debug.Log("Save folder created: " + _folderPath);
            }

            _filePath = Path.Combine(_folderPath, fileName + CFileExtension);
            if (!File.Exists(_filePath))
            {
                var fileStream = File.Create(_filePath);
                fileStream.Close();
                Debug.Log("Save file created: " + _filePath);
            }

            JsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            };

            ReadSaveFile();
        }

        public void AddData(string id, string componentId, object value)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(componentId))
            {
                Debug.LogError("Save data with null id!");
                return;
            }
            
            if (!_instanceSaves.ContainsKey(id))
            {
                _instanceSaves.Add(id, new InstanceSave(id));
            }

            var instanceSave = _instanceSaves[id];
            var componentSave = new ComponentSave(componentId, value);
            instanceSave.componentSaves[componentId] = componentSave;
        }

        public object ReadComponentData<T>(string id, string componentId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(componentId))
            {
                Debug.LogError("Save data with null id!");
                return null;
            }

            if (_instanceSaves.TryGetValue(id, out var instanceSave)
                && instanceSave.componentSaves.TryGetValue(componentId, out var componentSave))
                return componentSave.value;
            else
                return null;
        }

        public List<T?> ReadObjectData<T>(string id) where T : struct
        {
            if (_instanceSaves.TryGetValue(id, out var instanceSave))
                return instanceSave.componentSaves.Select(x=>x.Value.value as T?).ToList();
            else
                return null;
        }

        public void DeleteData(string instanceId, string componentId)
        {
            if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(componentId))
            {
                Debug.LogError("Save data with null id!");
                return;
            }

            if (_instanceSaves.TryGetValue(instanceId, out var instanceSave)
                && instanceSave.componentSaves.ContainsKey(componentId))
            {
                instanceSave.componentSaves.Remove(componentId);
            }
            else
            {
                Debug.Log("No save data found to delete for id: " + instanceId + " compId: " + componentId);
            }
        }

        public void DeleteData(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                Debug.LogError("Save data with null id!");
                return;
            }
            else if (!_instanceSaves.ContainsKey(instanceId))
            {
                Debug.Log("No save data found to delete for id: " + instanceId);
                return;
            }

            _instanceSaves.Remove(instanceId);
        }

        public void DeleteSave()
        {
            File.Delete(_filePath);
        }

        private void ReadSaveFile()
        {
            _instanceSaves ??= new Dictionary<string, InstanceSave>();

            var fileAsText = File.ReadAllText(_filePath);
            if (string.IsNullOrEmpty(fileAsText) || string.IsNullOrWhiteSpace(fileAsText))
                return;

            var splittedSaveFile = fileAsText.Split(InstanceSave.CEnd);
            for (int i = 0; i < splittedSaveFile.Length; i++)
            {
                var rawSavePart = splittedSaveFile[i];
                if (string.IsNullOrEmpty(rawSavePart))
                    continue;

                if (!InstanceSave.TryParse(rawSavePart, out InstanceSave instanceSave))
                {
                    Debug.LogError("Save can't be parsed! " + instanceSave.id);
                    continue;
                }

                _instanceSaves[instanceSave.id] = instanceSave;
            }
        }

        public void WriteSaveFile()
        {
            string fileString = "";
            var instanceSaveEn = _instanceSaves.GetEnumerator();
            bool isFirst = true;
            while (instanceSaveEn.MoveNext())
            {
                if (!isFirst)
                {
                    fileString += Environment.NewLine;
                }

                var instanceSave = instanceSaveEn.Current.Value;
                fileString += instanceSave.FormatJson();
                isFirst = false;
            }

            File.WriteAllText(_filePath, fileString);
        }
        
        public void Dispose()
        {
            var instanceSaveEn = _instanceSaves.GetEnumerator();
            while (instanceSaveEn.MoveNext())
            {
                instanceSaveEn.Current.Value.Dispose();
            }
            _instanceSaves.Clear();
        }

        [Serializable]
        private class InstanceSave : IDisposable
        {
            public const string CStart = "<instance-start>";
            public const string CEnd = "<instance-end>";
            public const string CIdPrefix = "<instance-id>";
            public const string CIdSuffix = "</instance-id>";

            public string id;
            public Dictionary<string, ComponentSave> componentSaves;

            public InstanceSave(string id)
            {
                componentSaves = new Dictionary<string, ComponentSave>();
                this.id = id;
            }

            internal string FormatJson()
            {
                string json = CStart + Environment.NewLine + CIdPrefix + id + CIdSuffix + Environment.NewLine;

                var compEn = componentSaves.GetEnumerator();
                while (compEn.MoveNext())
                {
                    json += compEn.Current.Value.FormatJson();
                }
                json += Environment.NewLine;
                json += CEnd;
                return json;
            }

            public static bool TryParse(string rawString, out InstanceSave instanceSave)
            {
                instanceSave = null;

                int idPrefixIndex = rawString.IndexOf(CIdPrefix);
                if (idPrefixIndex == -1)
                    return false;

                int idStartIndex = idPrefixIndex + CIdPrefix.Length;
                int idEndIndex = rawString.IndexOf(CIdSuffix, idStartIndex);
                string id = rawString.Substring(idStartIndex, idEndIndex - idStartIndex);
                instanceSave = new InstanceSave(id);

                var splittedComponentsRaw = rawString.Split(ComponentSave.CEnd);
                for (int i = 0; i < splittedComponentsRaw.Length; i++)
                {
                    var rawComponentSave = splittedComponentsRaw[i];
                    if (string.IsNullOrEmpty(rawComponentSave) || string.Equals(rawComponentSave, Environment.NewLine))
                        continue;

                    if (!ComponentSave.TryParse(rawComponentSave, out ComponentSave componentSave))
                    {
                        Debug.LogError("Component save can't be parsed! " + componentSave.id);
                        continue;
                    }

                    instanceSave.componentSaves.Add(componentSave.id, componentSave);
                }

                return true;
            }

            public void Dispose()
            {
                componentSaves.Clear();
            }
        }

        [Serializable]
        private struct ComponentSave
        {
            public const string CStart = "<component-start>";
            public const string CEnd = "</component-end>";
            public const string CIdPrefix = "<s-comp-id>";
            public const string CIdSuffix = "</s-comp-id>";
            public const string CTypePrefix = "<s-type>";
            public const string CTypeSuffix = "</s-type>";
            public const string CDataPrefix = "<s-value>";
            public const string CDataSuffix = "</s-value>";

            public string id;
            public Type type;
            public object value;

            public ComponentSave(string id, object value)
            {
                this.id = id;
                this.value = value;
                type = value.GetType();
            }

            public string FormatJson()
            {
                return FormatJson(id, value);
            }

            public static string FormatJson(string id, object value)
            {
                return CStart + Environment.NewLine
                + CIdPrefix + id + CIdSuffix + Environment.NewLine
                + CTypePrefix + JsonConvert.SerializeObject(value.GetType(), JsonSerializerSettings) + CTypeSuffix
                + CDataPrefix + JsonUtility.ToJson(value) + CDataSuffix + Environment.NewLine
                + CEnd;
            }

            public static bool TryParse(string rawString, out ComponentSave componentSave)
            {
                componentSave = new ComponentSave();

                int idPrefixIndex = rawString.IndexOf(CIdPrefix);
                if (idPrefixIndex == -1)
                    return false;

                int idStartIndex = idPrefixIndex + CIdPrefix.Length;
                int idEndIndex = rawString.IndexOf(CIdSuffix, idStartIndex);

                int typePrefixIndex = rawString.IndexOf(CTypePrefix);
                if (typePrefixIndex == -1)
                    return false;

                int typeStartIndex = typePrefixIndex + CTypePrefix.Length;
                int typeEndIndex = rawString.IndexOf(CTypeSuffix, typeStartIndex);

                int dataPrefixIndex = rawString.IndexOf(CDataPrefix, idEndIndex);
                if (dataPrefixIndex == -1)
                    return false;

                int dataStartIndex = dataPrefixIndex + CDataPrefix.Length;
                int dataEndIndex = rawString.IndexOf(CDataSuffix, dataStartIndex);

                componentSave.id = rawString.Substring(idStartIndex, idEndIndex - idStartIndex);

                string typeJson = rawString.Substring(typeStartIndex, typeEndIndex - typeStartIndex);
                componentSave.type = JsonConvert.DeserializeObject<Type>(typeJson, JsonSerializerSettings);

                string dataJson = rawString.Substring(dataStartIndex, dataEndIndex - dataStartIndex);
                componentSave.value = JsonUtility.FromJson(dataJson, componentSave.type);
                return true;
            }
        }
    }
}
