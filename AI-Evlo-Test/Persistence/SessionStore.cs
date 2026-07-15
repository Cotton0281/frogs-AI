using AI_Evlo_Test.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace AI_Evlo_Test.Persistence
{
    /// <summary>
    /// Owns durable session persistence, including atomic replacement, one backup, and
    /// migration from the old per-population GUID file format.
    /// </summary>
    internal sealed class SessionStore
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        private readonly string directory;

        internal SessionStore(string directory)
        {
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        internal string SessionPath => Path.Combine(directory, "session.json");
        internal string BackupPath => Path.Combine(directory, "session.backup.json");

        internal void Save(IReadOnlyCollection<Population> populations)
        {
            Directory.CreateDirectory(directory);
            string json = JsonConvert.SerializeObject(populations, Formatting.Indented, JsonSettings);
            AtomicWrite(SessionPath, json, BackupPath);
            CleanupLegacyFiles();
        }

        internal List<Population> Load()
        {
            List<Population> populations = TryRead(SessionPath) ?? TryRead(BackupPath);
            return populations != null && populations.Count > 0 ? populations : LoadLegacyFiles();
        }

        internal void CleanupLegacyFiles()
        {
            if (!Directory.Exists(directory))
                return;

            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                if (IsLegacyPopulationPath(file))
                    File.Delete(file);
            }
        }

        internal List<Population> LoadLegacyFiles()
        {
            var populations = new List<Population>();
            if (!Directory.Exists(directory))
                return populations;

            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                if (!IsLegacyPopulationPath(file))
                    continue;

                try
                {
                    Population population = JsonConvert.DeserializeObject<Population>(File.ReadAllText(file));
                    if (population != null && population.SizeLimit > 0)
                        populations.Add(population);
                }
                catch (JsonException)
                {
                    // A malformed legacy file should not prevent other populations from loading.
                }
                catch (IOException)
                {
                    // A file may disappear during one-time migration; skip it.
                }
            }

            return populations;
        }

        internal static void AtomicWrite(string destinationPath, string contents, string backupPath = null)
        {
            string targetDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            string tempPath = destinationPath + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None,
                    4096, FileOptions.WriteThrough))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(contents);
                    writer.Flush();
                    stream.Flush(flushToDisk: true);
                }

                if (File.Exists(destinationPath))
                {
                    if (!string.IsNullOrWhiteSpace(backupPath))
                    {
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);
                        File.Replace(tempPath, destinationPath, backupPath, ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Move(tempPath, destinationPath, overwrite: true);
                    }
                }
                else
                {
                    File.Move(tempPath, destinationPath);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        private static bool IsLegacyPopulationPath(string path) =>
            Guid.TryParse(Path.GetFileNameWithoutExtension(path), out _);

        private static List<Population> TryRead(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<List<Population>>(File.ReadAllText(path));
            }
            catch (JsonException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
