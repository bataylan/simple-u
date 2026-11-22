using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SimpleU.DataContainer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleU.Editors.DataContainer
{
    internal static class ItemSubAssetMover
    {
        private static readonly Regex Document = new(@"(?m)^--- !u!\d+ &(?<id>-?\d+)\b");
        private static readonly Regex Reference = new(@"\{fileID: *(?<id>-?\d+)(?:, *guid: *(?<guid>[a-f0-9]{32}), *type: *\d+)? *\}");
        private static readonly Regex ItemList = new(@"(?m)^  items:(?: \[\])?\r?\n(?:  - [^\r\n]*\r?\n)*");

        internal static ItemAsset Move(ItemAsset item, ItemAssetContainer destination, ItemAssetContainer source)
        {
            string ownerPath = AssetDatabase.GetAssetPath(item);
            string destinationPath = AssetDatabase.GetAssetPath(destination);
            string sourcePath = source ? AssetDatabase.GetAssetPath(source) : null;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(item, out string ownerGuid, out long fileId))
                throw new InvalidOperationException("Could not identify the item subasset.");

            // Unsaved scene or prefab edits can contain references absent from the dependency index.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty && ReferencesItem(scene, item, ownerPath))
                    throw new InvalidOperationException($"Save the changes to {scene.name} before moving the item.");
            }
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.scene.isDirty &&
                (AssetDatabase.GetDependencies(prefabStage.assetPath, false).Contains(ownerPath) ||
                 ReferencesItem(prefabStage.scene, item, ownerPath)))
                throw new InvalidOperationException("Save the open prefab before moving the item.");

            string oldId = fileId.ToString(CultureInfo.InvariantCulture);
            string destinationGuid = AssetDatabase.AssetPathToGUID(destinationPath);
            var paths = new HashSet<string> { ownerPath, destinationPath };
            if (sourcePath != null)
                paths.Add(sourcePath);

            foreach (var asset in Resources.FindObjectsOfTypeAll<ScriptableObject>())
            {
                if (EditorUtility.IsPersistent(asset) && EditorUtility.IsDirty(asset) && ReferencesItem(asset, item))
                    paths.Add(AssetDatabase.GetAssetPath(asset));
            }

            var candidates = AssetDatabase.GetAllAssetPaths().Where(path =>
                (path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("Packages/", StringComparison.Ordinal)) &&
                !paths.Contains(path) && Path.HasExtension(path)).ToArray();
            const int batchSize = 512;
            for (int start = 0; start < candidates.Length; start += batchSize)
            {
                int batchEnd = Math.Min(start + batchSize, candidates.Length);
                CollectDependents(candidates[start..batchEnd], ownerPath, paths);
            }

            var files = new Dictionary<string, string>();
            foreach (string path in paths)
            {
                if (!File.Exists(path))
                    throw new InvalidOperationException($"Cannot update the item reference in {path}.");

                AssetDatabase.SaveAssetIfDirty(new GUID(AssetDatabase.AssetPathToGUID(path)));
                files.Add(path, ReadYaml(path));
            }

            string owner = files[ownerPath];
            string target = files[destinationPath];
            var oldDocument = FindDocument(owner, oldId);
            if (!oldDocument.Success)
                throw new InvalidOperationException($"Could not find the item document in {ownerPath}.");

            var targetIds = new HashSet<string>(Document.Matches(target).Select(match => match.Groups["id"].Value));
            string newId = oldId;
            for (long candidate = long.MinValue; targetIds.Contains(newId); candidate++)
                newId = candidate.ToString(CultureInfo.InvariantCulture);

            var nextDocument = oldDocument.NextMatch();
            int end = nextDocument.Success ? nextDocument.Index : owner.Length;
            string moved = owner.Substring(oldDocument.Index, end - oldDocument.Index);
            string movedHeader = oldDocument.Value;
            moved = movedHeader[..(movedHeader.Length - oldId.Length)] + newId + moved[movedHeader.Length..];

            var changes = new Dictionary<string, string>();
            foreach (var file in files)
            {
                string path = file.Key;
                string guid = AssetDatabase.AssetPathToGUID(path);
                string contents = path == ownerPath ? owner.Remove(oldDocument.Index, end - oldDocument.Index) : file.Value;
                contents = RewriteReferences(contents, guid, guid, ownerGuid, oldId, destinationGuid, newId);

                if (path == sourcePath && path != destinationPath)
                    contents = UpdateMembership(contents, AssetDatabase.LoadMainAssetAtPath(path), guid, destinationGuid, newId, false);

                if (path == destinationPath)
                {
                    contents = UpdateMembership(contents, destination, guid, destinationGuid, newId, true);
                    if (!contents.EndsWith("\n", StringComparison.Ordinal))
                        contents += "\n";
                    contents += RewriteReferences(moved, ownerGuid, destinationGuid, ownerGuid, oldId, destinationGuid, newId);
                }

                if (contents != file.Value)
                    changes.Add(path, contents);
            }

            foreach (string path in changes.Keys)
            {
                if (!AssetDatabase.IsOpenForEdit(path) || (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0)
                    throw new InvalidOperationException($"Cannot write {path}.");
            }

            return WriteChanges(changes, destinationPath, newId);
        }

        private static void CollectDependents(string[] candidates, string ownerPath, HashSet<string> paths)
        {
            if (!AssetDatabase.GetDependencies(candidates, false).Contains(ownerPath))
                return;

            if (candidates.Length == 1)
            {
                paths.Add(candidates[0]);
                return;
            }

            int middle = candidates.Length / 2;
            CollectDependents(candidates[..middle], ownerPath, paths);
            CollectDependents(candidates[middle..], ownerPath, paths);
        }

        private static bool ReferencesItem(Scene scene, ItemAsset item, string ownerPath)
        {
            if (!string.IsNullOrEmpty(scene.path) && AssetDatabase.GetDependencies(scene.path, false).Contains(ownerPath))
                return true;

            return scene.GetRootGameObjects().Any(root =>
                root.GetComponentsInChildren<Component>(true).Any(component => component && ReferencesItem(component, item)));
        }

        private static bool ReferencesItem(UnityEngine.Object asset, ItemAsset item)
        {
            using var properties = new SerializedObject(asset);
            using var property = properties.GetIterator();
            var visited = new HashSet<long>();
            bool enterChildren = true;
            while (property.Next(enterChildren))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == item)
                    return true;

                enterChildren = property.propertyType != SerializedPropertyType.ManagedReference ||
                    visited.Add(property.managedReferenceId);
            }

            return false;
        }

        private static string RewriteReferences(string contents, string oldContext, string newContext,
            string ownerGuid, string oldId, string destinationGuid, string newId)
        {
            return Reference.Replace(contents, match =>
            {
                string id = match.Groups["id"].Value;
                if (id == "0")
                    return match.Value;

                var guidGroup = match.Groups["guid"];
                string guid = guidGroup.Success ? guidGroup.Value : oldContext;
                if (guid == ownerGuid && id == oldId)
                    return FormatReference(newContext, destinationGuid, newId);

                return !guidGroup.Success && oldContext != newContext
                    ? FormatReference(newContext, oldContext, id)
                    : match.Value;
            });
        }

        private static string UpdateMembership(string contents, UnityEngine.Object container, string context,
            string targetGuid, string id, bool insert)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(container, out _, out long containerId))
                throw new InvalidOperationException("Could not identify the container document.");

            var doc = FindDocument(contents, containerId.ToString(CultureInfo.InvariantCulture));
            if (!doc.Success)
                throw new InvalidOperationException($"Could not find the document for {container.name}.");

            var next = doc.NextMatch();
            int end = next.Success ? next.Index : contents.Length;
            var list = ItemList.Match(contents, doc.Index, end - doc.Index);
            if (!list.Success)
                throw new InvalidOperationException($"Could not find the item list for {container.name}.");

            string reference = "  - " + FormatReference(context, targetGuid, id);
            string newline = list.Value.Contains("\r\n") ? "\r\n" : "\n";
            var entries = list.Value.Split(new[] { newline }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToList();
            if (insert && !entries.Contains(reference))
                entries.Add(reference);
            if (!insert)
                entries.RemoveAll(entry => entry == reference);

            string updated = entries.Count == 0
                ? "  items: []" + newline
                : "  items:" + newline + string.Join(newline, entries) + newline;
            return contents.Remove(list.Index, list.Length).Insert(list.Index, updated);
        }

        private static Match FindDocument(string contents, string id)
        {
            return Document.Matches(contents).FirstOrDefault(match => match.Groups["id"].Value == id) ?? Match.Empty;
        }

        private static string FormatReference(string context, string guid, string id)
        {
            return context == guid ? $"{{fileID: {id}}}" : $"{{fileID: {id}, guid: {guid}, type: 2}}";
        }

        private static string ReadYaml(string path)
        {
            string contents = File.ReadAllText(path);
            if (!contents.StartsWith("%YAML ", StringComparison.Ordinal))
                throw new InvalidOperationException($"The asset move requires Unity text serialization for {path}.");
            return contents;
        }

        private static ItemAsset WriteChanges(Dictionary<string, string> changes, string destinationPath, string fileId)
        {
            var originals = new Dictionary<string, byte[]>();
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                foreach (var change in changes)
                {
                    var bytes = File.ReadAllBytes(change.Key);
                    originals.Add(change.Key, bytes);
                    bool hasBom = bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble());
                    File.WriteAllText(change.Key, change.Value, new UTF8Encoding(hasBom));
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var moved = AssetDatabase.LoadAllAssetsAtPath(destinationPath).OfType<ItemAsset>().Single(asset =>
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out _, out long id) &&
                    id.ToString(CultureInfo.InvariantCulture) == fileId);
                var destination = AssetDatabase.LoadAssetAtPath<ItemAssetContainer>(destinationPath);
                if (!destination || !destination.Items.Contains(moved))
                    throw new InvalidOperationException("Unity could not reload the moved item in its destination container.");
                return moved;
            }
            catch
            {
                foreach (var original in originals)
                    File.WriteAllBytes(original.Key, original.Value);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                throw;
            }
            finally
            {
                AssetDatabase.AllowAutoRefresh();
            }
        }
    }
}
