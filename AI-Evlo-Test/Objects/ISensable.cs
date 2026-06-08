using System.Windows;
using System;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Categories of objects that can be sensed by raycasting.
    /// Integer values are used for encoding in neural network inputs.
    /// </summary>
    public enum ObjectCategory
    {
        Food = -6,
        Raft = -5,
        Raft_Sunk = -3,
        Frog = 1,
        Frog_OnRaft = 2,
        Bird_Landed = 4,
        Bird = 5,
        Shark = 6
    }

    internal static class ObjectCategoryExtensions
    {
        private static readonly int CategoryCount = Enum.GetValues(typeof(ObjectCategory)).Length;
        public static double ToSignalValue(this ObjectCategory category)
        {
            return ((int)category + 1.0) / CategoryCount;
        }
    }

    /// <summary>
    /// Any world object that can be detected by perception rays.
    /// Implemented by TargetObj and SmartObject.
    /// </summary>
    public interface ISensable
    {
        Point Location { get; }
        double Size { get; set; }
        ObjectCategory Category { get; set; }
    }

    /// <summary>
    /// Immutable per-tick copy of the sensable state used by background raycasting.
    /// </summary>
    public struct SensableSnapshot
    {
        public SensableSnapshot(string id, Point location, double size, ObjectCategory category)
        {
            Id = id;
            Location = location;
            Size = size;
            Category = category;
        }

        public string Id { get; }
        public Point Location { get; }
        public double Size { get; }
        public ObjectCategory Category { get; }
    }
}
