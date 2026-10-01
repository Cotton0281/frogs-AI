using System;

namespace AI_Evlo_Test.Objects
{
    /// <summary>Chronological ring buffer. Its owner supplies synchronization.</summary>
    internal sealed class BoundedHistory<T>
    {
        private T[] values = Array.Empty<T>();
        private int start;
        private int count;

        public void Add(T value, int capacity)
        {
            capacity = Math.Max(0, capacity);
            if (values.Length != capacity)
            {
                T[] replacement = new T[capacity];
                int retained = Math.Min(count, capacity);
                for (int i = 0; i < retained; i++)
                    replacement[i] = values[(start + count - retained + i) % values.Length];
                values = replacement;
                start = 0;
                count = retained;
            }
            if (capacity == 0) return;
            if (count == capacity)
            {
                values[start] = value;
                start = (start + 1) % capacity;
            }
            else values[(start + count++) % capacity] = value;
        }

        public T[] Snapshot()
        {
            T[] result = new T[count];
            for (int i = 0; i < count; i++) result[i] = values[(start + i) % values.Length];
            return result;
        }
    }
}
