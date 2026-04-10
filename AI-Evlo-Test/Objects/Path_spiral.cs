using CoordinatesUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Describes the movement of object
    /// </summary>
    public class trajectory
    {
        public virtual Point GetNextLocation(Point LastLocation)
        {
            return LastLocation;
        }
    }
    // Calculate trajectory of an object in a spiral
    public class Path_spiral : trajectory
    {
        public Point SpiralCenter = new Point(100, 100);
        public bool ClockwiseDirection = true;
        public double Speed = 1;
        public double SpiralingAngle = 1;
        /// <summary>
        /// Max distance from center before spiral stops expanding
        /// </summary>
        public double MaxSize = 300;
        public bool goToCenterFirst = true;
        private bool Expanding = true;
        /// <summary>
        /// Get the coordinates of the next location on the spiral path
        /// </summary>
        /// <param name="LastLocation"></param>
        /// <returns></returns>
        public override Point GetNextLocation(Point LastLocation)
        {
            Point newLocation = new Point();
            Vector vectorToCenter = Point.Subtract(SpiralCenter, LastLocation);

            if (!goToCenterFirst)
            {
                if (vectorToCenter.Length < 10 && !Expanding)
                {
                    ClockwiseDirection = !ClockwiseDirection;
                    Expanding = true;
                }
                else if (vectorToCenter.Length > MaxSize)
                {
                    Expanding = false;
                }

                if(Expanding)
                    SpiralingAngle= Math.Abs(SpiralingAngle);
                else
                    SpiralingAngle = Math.Abs(SpiralingAngle)*-1;

                double rotationAngle = 90 + SpiralingAngle;
                if (ClockwiseDirection)
                    rotationAngle = Math.Abs(rotationAngle);
                else
                    rotationAngle = Math.Abs(rotationAngle) * -1;
                Vector SpyralDirection = vectorToCenter.Rotate(rotationAngle);

                SpyralDirection.Normalize();
                SpyralDirection = Vector.Multiply(SpyralDirection, Speed);
                newLocation = Point.Add(LastLocation, SpyralDirection);
            }
            else
            {
                if (vectorToCenter.Length < 10)
                    goToCenterFirst = false;
                // move one pixel close to the  center
                vectorToCenter.Normalize(); // make it 1 pixel long
                vectorToCenter = Vector.Multiply(vectorToCenter, Speed); // apply selected speed 
                newLocation = Point.Add(LastLocation, vectorToCenter);
            }

            return newLocation;
        }
    }
}
