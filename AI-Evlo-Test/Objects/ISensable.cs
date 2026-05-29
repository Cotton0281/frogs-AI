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
        Food = 0,
        Raft = 1,
        Raft_Sunk = 2,
        Frog = 3,
        Bird = 4,
        Bird_Landed = 5,
        Shark = 6
    }

    internal static class ObjectCategoryExtensions
    {
        public static double ToSignalValue(this ObjectCategory category)
        {
            return ((int)category + 1.0) / Enum.GetValues(typeof(ObjectCategory)).Length;
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
}
