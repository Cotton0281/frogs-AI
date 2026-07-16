using System;
using System.Windows;

namespace AI_Evlo_Test.Objects
{
    internal static class AgentWorldBoundaryPolicy
    {
        internal const double OffscreenGracePixels = 500;

        internal static bool ShouldRetire(ISmartObject agent, double worldWidth, double worldHeight)
        {
            if (agent == null)
                return false;

            if (agent.HP <= 0)
                return true;

            if (!HasUsableWorldSize(worldWidth, worldHeight))
                return false;

            Point location = agent.Location;
            if (!double.IsFinite(location.X) || !double.IsFinite(location.Y))
                return true;

            return location.X < -OffscreenGracePixels
                || location.X > worldWidth + OffscreenGracePixels
                || location.Y < -OffscreenGracePixels
                || location.Y > worldHeight + OffscreenGracePixels;
        }

        internal static Point NormalizeSpawnLocation(
            Point proposedLocation,
            double worldWidth,
            double worldHeight)
        {
            if (!HasUsableWorldSize(worldWidth, worldHeight))
                return proposedLocation;

            double x = double.IsFinite(proposedLocation.X) ? proposedLocation.X : worldWidth / 2;
            double y = double.IsFinite(proposedLocation.Y) ? proposedLocation.Y : worldHeight / 2;

            return new Point(
                Math.Clamp(x, -OffscreenGracePixels, worldWidth + OffscreenGracePixels),
                Math.Clamp(y, -OffscreenGracePixels, worldHeight + OffscreenGracePixels));
        }

        private static bool HasUsableWorldSize(double width, double height)
        {
            return double.IsFinite(width)
                && double.IsFinite(height)
                && width > 0
                && height > 0;
        }

    }
}
