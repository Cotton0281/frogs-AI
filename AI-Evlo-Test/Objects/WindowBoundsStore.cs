using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Persists per-window sizes (width, height) to a small JSON file under %AppData%\AI-Evlo so
    /// each window/form reopens at the size the user last left it. Only the size is stored (not the
    /// position) to avoid windows restoring off-screen after a monitor change.
    /// </summary>
    internal static class WindowBoundsStore
    {
        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AI-Evlo", "window-sizes.json");

        private static Dictionary<string, double[]> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonConvert.DeserializeObject<Dictionary<string, double[]>>(File.ReadAllText(FilePath))
                           ?? new Dictionary<string, double[]>();
            }
            catch { /* corrupt or unreadable — start fresh */ }
            return new Dictionary<string, double[]>();
        }

        /// <summary>Returns the saved width/height for the given key, if a valid one exists.</summary>
        public static bool TryGet(string key, out double width, out double height)
        {
            width = height = 0;
            var dict = Load();
            if (dict.TryGetValue(key, out double[] v) && v != null && v.Length == 2 && v[0] > 0 && v[1] > 0)
            {
                width = v[0];
                height = v[1];
                return true;
            }
            return false;
        }

        public static void Save(string key, double width, double height)
        {
            if (width <= 0 || height <= 0)
                return;
            try
            {
                var dict = Load();
                dict[key] = new[] { width, height };
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(dict));
            }
            catch { /* best effort */ }
        }
    }
}
