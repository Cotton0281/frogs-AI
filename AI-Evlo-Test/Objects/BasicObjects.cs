using CoordinatesUtil;
using System;
using System.Collections.Generic;
using System.Windows;
using static AI_Evlo_Test.Objects.BasicObject;

namespace AI_Evlo_Test.Objects
{

    public class BasicObject : IBasicObject
    {
        public event LocationChanged_Handler OnLocationChanged;
        private Point location = new Point(0, 0);
        private FrameworkElement visibleShape = null;

        public string ID { get; set; } = "0";
        public Point Location { get => location; }
        public FrameworkElement VisibleShape { get => visibleShape; set => visibleShape = value; }
        public double Size {get; set; }
        public Vector Intertia = new Vector(0, 0);
        public Vector FaceDirection { get; set; } = new Vector(0, -1);
        public Trajectory Trajectory = new Trajectory();

        object objectLock = new Object(); // used to lock the event
        event LocationChanged_Handler IBasicObject.OnLocationChanged
        {
            add
            {
                lock (objectLock)
                {
                    OnLocationChanged += value;
                }
            }

            remove
            {
                lock(objectLock)
                {
                    OnLocationChanged -= value;
                }
            }
        }


        /// <summary>
        /// Changes location of object and triggers event 
        /// </summary>
        /// <param name="locationNew"></param>
        public void SetLocation(Point locationNew)
        {
            SetLocation(locationNew.X, locationNew.Y);
        }

        /// <summary>
        /// Changes location of object and triggers event 
        /// </summary>
        /// <param name="X">X coordinate</param>
        /// <param name="Y"> Y coordinate</param>
        public void SetLocation(double X, double Y)
        {
            if (X != location.X || Y != location.Y)
            {
                location.X = X;
                location.Y = Y;
                OnLocationChanged?.Invoke(this, Location);
            }
        }
        public void MoveTo(Vector vectorAddToLocatioon)
        {
            location = Point.Add(Location, vectorAddToLocatioon);
        }

        /// <summary>
        /// Move the location in the direction of FaceDirection
        /// </summary>
        /// <param name="Force">How much to move in pixels</param>
        public void PushForward(double Force)
        {
            Vector v = Vector.Multiply(FaceDirection, Force);
            this.MoveTo(v);
        }

        public void Rotate(double degrresRotation)
        {
            FaceDirection = FaceDirection.Rotate(degrresRotation);
            // Following moved outside of the paralel multithreads
            //double anglFromVertical = Vector.AngleBetween(new Vector(0, -1), FaceDirection);
            // VisibleShape.RenderTransform = new System.Windows.Media.RotateTransform(anglFromVertical,);


        }
        public void RotateTo(Point ptDirection)
        {
            FaceDirection = Point.Subtract(ptDirection, location);
            double anglFromVertical = Vector.AngleBetween(new Vector(0, -1), FaceDirection);
            if (VisibleShape != null)
                VisibleShape.RenderTransform = new System.Windows.Media.RotateTransform(anglFromVertical);
        }

        /// <summary>
        /// Returns true if this object's circular bounds overlap with another object.
        /// Uses Size as the diameter of each object's bounding circle.
        /// </summary>
        public bool IsCollidingWith(BasicObject other)
        {
            double radiusA = Size / 2.0;
            double radiusB = other.Size / 2.0;
            Vector delta = Point.Subtract(other.Location, Location);
            double distSq = delta.LengthSquared;
            double minDist = radiusA + radiusB;

            return distSq > 0 && distSq < minDist * minDist;
        }

        /// <summary>
        /// Resolves an elastic billiard-ball bounce between two objects.
        /// Both objects' Inertia vectors are updated and positions are separated
        /// so they no longer overlap. Assumes equal mass.
        /// </summary>
        public static void ResolveElasticBounce(BasicObject a, BasicObject b)
        {
            Vector delta = Point.Subtract(b.Location, a.Location);
            double dist = delta.Length;
            double radiusA = a.Size / 2.0;
            double radiusB = b.Size / 2.0;
            double minDist = radiusA + radiusB;

            if (dist >= minDist || dist == 0)
                return;

            // Unit normal from a toward b
            Vector normal = delta;
            normal.Normalize();

            // Separate objects so they no longer overlap
            double overlap = minDist - dist;
            Vector separation = Vector.Multiply(normal, overlap / 2.0);
            a.SetLocation(a.Location.X - separation.X, a.Location.Y - separation.Y);
            b.SetLocation(b.Location.X + separation.X, b.Location.Y + separation.Y);

            // Project velocities onto collision normal (dot product)
            double aSpeed = Vector.Multiply(a.Intertia, normal);
            double bSpeed = Vector.Multiply(b.Intertia, normal);

            // Objects already moving apart — no impulse needed
            if (aSpeed - bSpeed <= 0)
                return;

            // Equal-mass elastic collision: swap the normal-component of velocities
            a.Intertia += Vector.Multiply(normal, bSpeed - aSpeed);
            b.Intertia += Vector.Multiply(normal, aSpeed - bSpeed);
        }

        /// <summary>
        /// Resolves all pair-wise elastic bounces for a list of objects.
        /// Call once per tick after all objects have moved.
        /// </summary>
        public static void ResolveAllCollisions(List<BasicObject> objects)
        {
            for (int i = 0; i < objects.Count; i++)
                for (int j = i + 1; j < objects.Count; j++)
                    ResolveElasticBounce(objects[i], objects[j]);
        }

        internal void Dispose()
        {
            this.visibleShape = null;
            Trajectory = null;
        }
    }

    public interface IBasicObject
    {
        event LocationChanged_Handler OnLocationChanged;
        System.Windows.Point Location { get; }
        [Newtonsoft.Json.JsonIgnore]
        [System.Runtime.Serialization.IgnoreDataMember]
        System.Windows.FrameworkElement VisibleShape { get; set; }
        void SetLocation(double X, double Y);
        void SetLocation(System.Windows.Point locationNew);
        double Size { get; set; }
    }


    public delegate void LocationChanged_Handler(IBasicObject objSimpleObject, Point ObjecLocation);
}
