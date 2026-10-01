using System;
using System.Collections.Generic;
using System.Windows;

namespace AI_Evlo_Test.Objects
{
    /// <summary>Rebuilt once per tick, then read concurrently by all agent sensors.</summary>
    public sealed class SpatialPerceptionIndex
    {
        private const double CellSize = 250;
        private readonly Dictionary<(int X, int Y), List<int>> cells = new();
        private readonly Stack<List<int>> pool = new();
        private IReadOnlyList<SensableSnapshot> objects = Array.Empty<SensableSnapshot>();
        private double maxRadius;

        public void Rebuild(IReadOnlyList<SensableSnapshot> snapshot)
        {
            foreach (var cell in cells.Values) { cell.Clear(); pool.Push(cell); }
            cells.Clear();
            objects = snapshot;
            maxRadius = 0;
            for (int i = 0; i < snapshot.Count; i++)
            {
                var item = snapshot[i];
                if (!double.IsFinite(item.Location.X) || !double.IsFinite(item.Location.Y)) continue;
                maxRadius = Math.Max(maxRadius, Math.Max(0, item.Size / 2));
                var key = (Cell(item.Location.X), Cell(item.Location.Y));
                if (!cells.TryGetValue(key, out var bucket))
                    cells.Add(key, bucket = pool.Count > 0 ? pool.Pop() : new List<int>());
                bucket.Add(i);
            }
        }

        public IReadOnlyList<SensableSnapshot> Query(Point origin, double range, List<int> indices, List<SensableSnapshot> result)
        {
            indices.Clear();
            result.Clear();
            if (!double.IsFinite(origin.X) || !double.IsFinite(origin.Y)) return result;
            double radius = range + maxRadius;
            long queryCells = (long)(Cell(origin.X + radius) - Cell(origin.X - radius) + 1)
                * (Cell(origin.Y + radius) - Cell(origin.Y - radius) + 1);
            // Dense or small worlds are cheaper to scan than to query and sort.
            if (queryCells > cells.Count / 2) return objects;
            for (int x = Cell(origin.X - radius); x <= Cell(origin.X + radius); x++)
                for (int y = Cell(origin.Y - radius); y <= Cell(origin.Y + radius); y++)
                    if (cells.TryGetValue((x, y), out var bucket))
                        foreach (int index in bucket)
                        {
                            var item = objects[index];
                            double dx = item.Location.X - origin.X, dy = item.Location.Y - origin.Y;
                            double limit = range + item.Size / 2;
                            if (dx * dx + dy * dy <= limit * limit) indices.Add(index);
                        }
            // Preserve full-scan tie-breaking order, including equal-distance categories.
            if (indices.Count > objects.Count / 2) return objects;
            indices.Sort();
            foreach (int index in indices) result.Add(objects[index]);
            return result;
        }

        private static int Cell(double coordinate) => (int)Math.Floor(coordinate / CellSize);
    }
}
