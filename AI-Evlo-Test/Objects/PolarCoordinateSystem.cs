using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CoordinatesUtil
{
    /// <summary>
    /// Polar coordinates use angle and distance to locate a position in relation to a view point (base relation)
    /// 
    /// </summary>
    public static class PolarCoordinateSystem
    {
        /// <summary>
        ///  Multiplier for degrees to convert to radians.
        /// </summary>
        public const double ToRadRatio = Math.PI / 180.0;

        /// <summary>
        /// Multiplier for radians to convert to degrees.
        /// </summary>
        public const double ToDegreeRatio = 180.0 / Math.PI;

        /// <summary>
        /// Calculates angle in degrees between two points and x-axis.
        /// </summary>
        public static double AngleToXAxis(Point start, Point end)
        {
            return Math.Atan2(start.Y - end.Y, end.X - start.X) * ToDegreeRatio;
        }

        public static PolarLocation CartesianToPolarCoordinates(Point start, Point end)
        {
            return new PolarLocation(DistanceOnCartesianMap(start, end), AngleToXAxis(start, end));
        }

        public static double DistanceOnCartesianMap(Point start, Point end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>
        /// Calculate distance between 2 locations on Cartesian Map
        /// </summary>
        /// <returns>Distance in pixels</returns>
        public static double getDistance(double X1, double Y1, double X2, double Y2)
        {
            double dx = X2 - X1;
            double dy = Y2 - Y1;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>
        /// Convert angle from Degrees to Radiant
        /// </summary>
        /// <param name="AngleInDegrees">Angle in degrees </param>
        /// <returns> Angle in radians</returns>
        public static double DegToRad(double AngleInDegrees)
        {
            return AngleInDegrees * ToRadRatio;
        }

        /// <summary>
        /// Convert angle from Radiant to Degrees
        /// </summary>
        /// <param name="AngleInRadiant">Angle in Radiant </param>
        /// <returns> Angle in degrees</returns>
        public static double RadToDeg(double AngleInRadiant)
        {
            return AngleInRadiant * ToDegreeRatio;
        }

        
        /// <summary>
        /// returns angle between 0 and 360. if angle is negative , the 
        /// </summary>
        /// <param name="Viewer">Object that the location is related to</param>
        /// <param name="location2"> Coordinates of a location on a Cartesian map</param>
        /// <returns></returns>
        //internal static PolarLocation LocationFromObject(UniverseObject Viewer, Point location2)
        //{
        //    var polarPosition = PolarCoordinateSystem.CartesianToPolarCoordinates(Viewer.Location, location2);
        //    polarPosition.Angle += Viewer.FacingDirection;
        //    polarPosition.Angle = polarPosition.Angle % 360;
        //    if (polarPosition.Angle < 0)
        //        polarPosition.Angle += 360; // Convert negative angles to positive
        //    return polarPosition;
        //}
    }

    /// <summary>
    /// Location described by angle and distace on a polar coordinate sysem
    /// </summary>
    public class PolarLocation
    {
        public double Distance { get; set; }
        /// <summary>
        /// Angle in degrees 360
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// Angle in Radians
        /// </summary>
        public double AngleRad
        {
            get { return Angle * PolarCoordinateSystem.ToRadRatio; }
            set { Angle = value * PolarCoordinateSystem.ToDegreeRatio; }
        }

        public PolarLocation(double Distance, double AngleInDegrees)
        {
            this.Distance = Distance;
            this.Angle = AngleInDegrees;
        }
    }


    /// <summary>
    /// extention to Vector class to calulate location based on angle and distance, 
    /// Or also meaning to rotate vector
    /// </summary>
    public static class VectorExt
    {
        private const double DegToRad = Math.PI / 180;

        /// <summary>
        /// Rotate in degrees. This is custom extention   
        /// </summary>
        /// <param name="v"></param>
        /// <param name="degrees">Angle in degrees</param>
        /// <returns>Rotated vector</returns>
        public static Vector Rotate(this Vector v, double degrees)
        {
            return v.RotateRadians(degrees * DegToRad);
        }

        public static Vector RotateRadians(this Vector v, double radians)
        {
            var ca = Math.Cos(radians);
            var sa = Math.Sin(radians);
            return new Vector(ca * v.X - sa * v.Y, sa * v.X + ca * v.Y);
        }
    }
}
