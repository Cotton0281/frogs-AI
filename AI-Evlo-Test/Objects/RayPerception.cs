using System;
using System.Collections.Generic;
using System.Windows;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Ego-centric raycasting sensor inspired by NEAT whisker sensors
    /// and Unity ML-Agents RayPerceptionSensor.
    /// 
    /// Casts N rays from the agent at fixed angular offsets from FaceDirection.
    /// Each ray reports: (normalizedDistance, objectTypeSignal).
    /// Total NN inputs = RayCount * 2.
    /// Thread-safe — no UI calls.
    /// </summary>
    public class RayPerception
    {
        public int RayCount { get; }
        public double MaxDistance { get; }
        public double FieldOfView { get; }

        /// <summary>
        /// Per-ray results. Length = RayCount * 2.
        /// Layout: [ray0_distance, ray0_type, ray1_distance, ray1_type, ...]
        /// </summary>
        public double[] Signals { get; }

        /// <summary>Total NN inputs this sensor produces (RayCount * 2).</summary>
        public int InputCount => Signals.Length;

        /// <summary>Pre-computed angular offsets in degrees from FaceDirection.</summary>
        public double[] RayAngles { get; }

        /// <summary>Per-ray maximum detection distance. Center ray(s) may be longer.</summary>
        public double[] MaxDistances { get; }

        /// <summary>Pre-computed cosine for each ray angle.</summary>
        private double[] RayCos { get; }

        /// <summary>Pre-computed sine for each ray angle.</summary>
        private double[] RaySin { get; }

        private readonly double[] _closestDists;
        private readonly ObjectCategory[] _closestTypes;
        private readonly bool[] _hitSomethings;
        private readonly double[] _rayDirsX;
        private readonly double[] _rayDirsY;

        /// <summary>
        /// Per-ray hit info used by the visualizer. Set during Update().
        /// </summary>
        public RayHit[] Hits { get; }

        public RayPerception(int rayCount = 12, double maxDistance = 250, double fieldOfView = 180, double centerRayMultiplier = 1.0)
        {
            RayCount = rayCount;
            MaxDistance = maxDistance;
            FieldOfView = fieldOfView;
            Signals = new double[rayCount * 2];
            RayAngles = new double[rayCount];
            MaxDistances = new double[rayCount];
            RayCos = new double[rayCount];
            RaySin = new double[rayCount];
            Hits = new RayHit[rayCount];
            _closestDists = new double[rayCount];
            _closestTypes = new ObjectCategory[rayCount];
            _hitSomethings = new bool[rayCount];
            _rayDirsX = new double[rayCount];
            _rayDirsY = new double[rayCount];

            if (rayCount == 12 && Math.Abs(fieldOfView - 180.0) < 0.0001)
            {
                double startAngle = -fieldOfView / 2.0;
                double step = 8 > 1 ? fieldOfView / (8 - 1) : 0;

                for (int i = 0; i < 8; i++)
                    RayAngles[i] = startAngle + step * i;

                // Keep front/side beams unchanged. Spread only rear beams by 36° steps.
                // Side beams are at RayAngles[0] and RayAngles[7] (±90° with 180° FOV).
                RayAngles[8] = NormalizeAngle(RayAngles[0] - 36.0);
                RayAngles[9] = NormalizeAngle(RayAngles[8] - 36.0);
                RayAngles[10] = NormalizeAngle(RayAngles[9] - 36.0);
                RayAngles[11] = NormalizeAngle(RayAngles[10] - 36.0);
            }
            else
            {
                double startAngle = -fieldOfView / 2.0;
                double step = rayCount > 1 ? fieldOfView / (rayCount - 1) : 0;
                for (int i = 0; i < rayCount; i++)
                    RayAngles[i] = startAngle + step * i;
            }

            // Assign per-ray max distances; center ray(s) get the multiplier
            double minAbsAngle = double.MaxValue;
            for (int i = 0; i < rayCount; i++)
            {
                double absAngle = Math.Abs(RayAngles[i]);
                if (absAngle < minAbsAngle)
                    minAbsAngle = absAngle;
            }

            for (int i = 0; i < rayCount; i++)
            {
                bool isCenter = Math.Abs(Math.Abs(RayAngles[i]) - minAbsAngle) < 0.0001;
                MaxDistances[i] = isCenter ? maxDistance * centerRayMultiplier : maxDistance;

                double angleRad = RayAngles[i] * Math.PI / 180.0;
                RayCos[i] = Math.Cos(angleRad);
                RaySin[i] = Math.Sin(angleRad);
            }
        }

        private static double NormalizeAngle(double angle)
        {
            while (angle > 180.0)
                angle -= 360.0;

            while (angle < -180.0)
                angle += 360.0;

            return angle;
        }

        /// <summary>
        /// Cast all rays and populate Signals + Hits arrays.
        /// </summary>
        /// <param name="agentLocation">Agent world position.</param>
        /// <param name="agentFacing">Agent FaceDirection vector.</param>
        /// <param name="sensableObjects">All detectable objects in the world.</param>
        /// <param name="selfId">Agent's own ID so it skips itself.</param>
        /// <param name="ignoredCategories">Categories invisible to this agent's rays.</param>
        public void Update(Point agentLocation, Vector agentFacing,
            IList<ISensable> sensableObjects, string selfId = null,
            ObjectCategory[] ignoredCategories = null)
        {
            Array.Clear(Signals, 0, Signals.Length);

            Vector forward = agentFacing;
            forward.Normalize();

            double originX = agentLocation.X;
            double originY = agentLocation.Y;
            int sensableCount = sensableObjects.Count;

            for (int r = 0; r < RayCount; r++)
            {
                double cos = RayCos[r];
                double sin = RaySin[r];
                _rayDirsX[r] = forward.X * cos - forward.Y * sin;
                _rayDirsY[r] = forward.X * sin + forward.Y * cos;
                _closestDists[r] = MaxDistances[r];
                _closestTypes[r] = ObjectCategory.Food;
                _hitSomethings[r] = false;
            }

            for (int s = 0; s < sensableCount; s++)
            {
                ISensable obj = sensableObjects[s];

                // Skip self
                if (selfId != null && obj is ISmartObject smart && smart.ID == selfId)
                    continue;

                // Skip categories this agent cannot perceive
                if (ignoredCategories != null)
                {
                    bool skip = false;
                    for (int ic = 0; ic < ignoredCategories.Length; ic++)
                    {
                        if (obj.Category == ignoredCategories[ic])
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip)
                        continue;
                }

                double dx = obj.Location.X - originX;
                double dy = obj.Location.Y - originY;
                double ocLenSq = dx * dx + dy * dy;

                double radius = obj.Size / 2.0;
                double radiusSq = radius * radius;
                ObjectCategory category = obj.Category;

                for (int r = 0; r < RayCount; r++)
                {
                    double maxDist = _closestDists[r];
                    double maxRange = maxDist + radius;

                    if (ocLenSq > maxRange * maxRange)
                        continue;

                    // Project onto ray direction
                    double proj = dx * _rayDirsX[r] + dy * _rayDirsY[r];
                    if (proj < 0)
                        continue; // circle is behind the ray

                    double perpDistSq = ocLenSq - proj * proj;

                    if (perpDistSq > radiusSq)
                        continue; // ray misses the circle

                    double halfChord = Math.Sqrt(radiusSq - perpDistSq);
                    double hitDist = proj - halfChord;
                    if (hitDist < 0) hitDist = 0; // ray starts inside the circle

                    if (hitDist <= maxDist)
                    {
                        _closestDists[r] = hitDist;
                        _closestTypes[r] = category;
                        _hitSomethings[r] = true;
                    }
                }
            }

            for (int r = 0; r < RayCount; r++)
            {
                int idx = r * 2;
                double rayMaxDist = MaxDistances[r];
                double closestDist = _closestDists[r];
                double rayDirX = _rayDirsX[r];
                double rayDirY = _rayDirsY[r];

                if (_hitSomethings[r])
                {
                    // 1.0 = touching, 0.0 = at max range
                    Signals[idx] = 1.0 - (closestDist / rayMaxDist);
                    Signals[idx + 1] = _closestTypes[r].ToSignalValue();

                    Hits[r] = new RayHit
                    {
                        IsValid = true,
                        HitPoint = new Point(originX + (rayDirX * closestDist), originY + (rayDirY * closestDist)),
                        Category = _closestTypes[r],
                        Distance = closestDist
                    };
                }
                else
                {
                    // Both signals stay 0.0 (nothing detected)
                    Hits[r] = new RayHit
                    {
                        IsValid = true,
                        HitPoint = new Point(originX + (rayDirX * rayMaxDist), originY + (rayDirY * rayMaxDist)),
                        Category = null,
                        Distance = rayMaxDist
                    };
                }
            }
        }

        /// <summary>
        /// Returns flat input array for the NN: optional scalar extras prepended + ray signals.
        /// </summary>
        public double[] GetInputs(params double[] extraInputs)
        {
            if (extraInputs == null || extraInputs.Length == 0)
                return Signals;

            double[] combined = new double[extraInputs.Length + Signals.Length];
            Array.Copy(extraInputs, 0, combined, 0, extraInputs.Length);
            Array.Copy(Signals, 0, combined, extraInputs.Length, Signals.Length);
            return combined;
        }

        /// <summary>
        /// Fills the provided buffer with the given extras prepended and then the ray signals.
        /// </summary>
        public void FillInputs(double[] buffer, double hpDeficit, double staminaDeficit)
        {
            buffer[0] = hpDeficit;
            buffer[1] = staminaDeficit;
            Array.Copy(Signals, 0, buffer, 2, Signals.Length);
        }
    }

    /// <summary>
    /// Per-ray hit result used by the visualizer.
    /// </summary>
    public struct RayHit
    {
        /// <summary>Indicates whether the hit has been initialized during an Update call.</summary>
        public bool IsValid { get; set; }
        /// <summary>World-space point where the ray ends (hit or max distance).</summary>
        public Point HitPoint { get; set; }
        /// <summary>Null if ray reached max distance without hitting anything.</summary>
        public ObjectCategory? Category { get; set; }
        /// <summary>Distance from agent to the hit point.</summary>
        public double Distance { get; set; }
    }
}
