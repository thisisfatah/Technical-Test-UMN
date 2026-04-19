using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Aquascape
{
    public sealed class RuntimeAssetScanner : MonoBehaviour
    {
        private struct FileObservation
        {
            public long Length;
            public long LastWriteTicks;
            public int StablePasses;
        }

        private readonly Dictionary<string, FileObservation> pendingFiles = new();
        private readonly HashSet<string> processedFiles = new();
        private readonly HashSet<string> warnedFiles = new();

        private string runtimeFolderPath;
        private float pollInterval;
        private SpawnService spawnService;

        public void Initialize(string folderPath, float intervalSeconds, SpawnService service)
        {
            runtimeFolderPath = folderPath;
            pollInterval = Mathf.Max(0.2f, intervalSeconds);
            spawnService = service;
        }

        public void Begin()
        {
            StartCoroutine(ScanLoop());
        }

        private IEnumerator ScanLoop()
        {
            while (true)
            {
                ScanOnce();
                yield return new WaitForSeconds(pollInterval);
            }
        }

        private void ScanOnce()
        {
            if (string.IsNullOrWhiteSpace(runtimeFolderPath) || spawnService == null)
            {
                return;
            }

            Directory.CreateDirectory(runtimeFolderPath);

            string[] files;
            try
            {
                files = Directory.GetFiles(runtimeFolderPath, "*", SearchOption.AllDirectories);
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"Failed to scan runtime import folder. Reason: {exception.Message}");
                return;
            }

            for (var index = 0; index < files.Length; index++)
            {
                var filePath = files[index];
                if (!string.Equals(Path.GetExtension(filePath), ".png", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (warnedFiles.Add(filePath))
                    {
                        Debug.LogWarning($"Ignored non-PNG file in runtime import folder: {Path.GetFileName(filePath)}");
                    }

                    continue;
                }

                if (processedFiles.Contains(filePath))
                {
                    continue;
                }

                ObservePng(filePath);
            }
        }

        private void ObservePng(string filePath)
        {
            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    return;
                }
            }
            catch (IOException)
            {
                return;
            }

            if (!pendingFiles.TryGetValue(filePath, out var observation))
            {
                pendingFiles[filePath] = new FileObservation
                {
                    Length = fileInfo.Length,
                    LastWriteTicks = fileInfo.LastWriteTimeUtc.Ticks,
                    StablePasses = 0
                };
                return;
            }

            if (observation.Length == fileInfo.Length && observation.LastWriteTicks == fileInfo.LastWriteTimeUtc.Ticks)
            {
                observation.StablePasses++;
            }
            else
            {
                observation.Length = fileInfo.Length;
                observation.LastWriteTicks = fileInfo.LastWriteTimeUtc.Ticks;
                observation.StablePasses = 0;
            }

            if (observation.StablePasses < 1)
            {
                pendingFiles[filePath] = observation;
                return;
            }

            pendingFiles.Remove(filePath);
            processedFiles.Add(filePath);

            if (!RuntimeImportParser.TryParse(filePath, out var descriptor, out var failureReason))
            {
                Debug.LogWarning(failureReason);
                return;
            }

            spawnService.QueueSpawn(descriptor);
        }
    }
}
