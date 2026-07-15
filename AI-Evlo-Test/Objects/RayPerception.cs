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
        public const int DefaultRayCount = 12;
        public const int HitsPerRay = 2;
        public const int ValuesPerHit = 2;
        public const int DefaultInputCount = DefaultRayCount * HitsPerRay * ValuesPerHit;

        public int RayCount { get; }
        public double MaxDistance { get; }
        public double FieldOfView { get; }

        /// <summary>
        /// Per-ray results. Length = RayCount * 2.
        /// Layout: [ray0_hit0_distance, ray0_hit0_type, ray0_hit1_distance, ray0_hit1_type, ...]
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

        private readonly double[,] _hitDists;
        private readonly ObjectCategory[,] _hitTypes;
        private readonly bool[,] _hitSomethings;
        private readonly double[] _rayDirsX;
        private readonly double[] _rayDirsY;

        /// <summary>
        /// Per-ray hit info used by the visualizer. Set during Update().
        /// </summary>
        public RayHit[] Hits { get; }

        /// <summary>
        /// Per-ray, per-layer hit info. Layer 0 is the closest category; layer 1 is the
        /// nearest different category behind or beyond it.
        /// </summary>
        public RayHit[,] HitLayers { get; }

        public RayPerception(int rayCount = DefaultRayCount, double maxDistance = 250, double fieldOfView = 180, double centerRayMultiplier = 1.0)
        {
            RayCount = rayCount;
            MaxDistance = maxDistance;
            FieldOfView = fieldOfView;
            Signals = new double[rayCount * HitsPerRay * ValuesPerHit];
            RayAngles = new double[rayCount];
            MaxDistances = new double[rayCount];
            RayCos = new double[rayCount];
            RaySin = new double[rayCount];
            Hits = new RayHit[rayCount];
            HitLayers = new RayHit[rayCount, HitsPerRay];
            _hitDists = new double[rayCount, HitsPerRay];
            _hitTypes = new ObjectCategory[rayCount, HitsPerRay];
            _hitSomethings = new bool[rayCount, HitsPerRay];
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
            IReadOnlyList<SensableSnapshot> sensableObjects, string selfId = null,
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
                for (int h = 0; h < HitsPerRay; h++)
                {
                    _hitDists[r, h] = MaxDistances[r];
                    _hitTypes[r, h] = ObjectCategory.Food;
                    _hitSomethings[r, h] = false;
                }
            }

            for (int s = 0; s < sensableCount; s++)
            {
                SensableSnapshot obj = sensableObjects[s];

                // Skip self
                if (selfId != null && obj.Id == selfId)
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
                    double maxDist = MaxDistances[r];
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
                        AddDistinctCategoryHit(r, hitDist, category);
                }
            }

            for (int r = 0; r < RayCount; r++)
            {
                double rayMaxDist = MaxDistances[r];
                double rayDirX = _rayDirsX[r];
                double rayDirY = _rayDirsY[r];

                for (int h = 0; h < HitsPerRay; h++)
                {
                    int idx = ((r * HitsPerRay) + h) * ValuesPerHit;

                    if (_hitSomethings[r, h])
                    {
                        double hitDist = _hitDists[r, h];
                        ObjectCategory hitType = _hitTypes[r, h];

                        // 1.0 = touching, 0.0 = at max range
                        Signals[idx] = 1.0 - (hitDist / rayMaxDist);
                        Signals[idx + 1] = hitType.ToSignalValue();

                        HitLayers[r, h] = new RayHit
                        {
                            IsValid = true,
                            HitPoint = new Point(originX + (rayDirX * hitDist), originY + (rayDirY * hitDist)),
                            Category = hitType,
                            Distance = hitDist
                        };
                    }
                    else
                    {
                        // Both signals stay 0.0 (nothing detected)
                        HitLayers[r, h] = new RayHit
                        {
                            IsValid = true,
                            HitPoint = new Point(originX + (rayDirX * rayMaxDist), originY + (rayDirY * rayMaxDist)),
                            Category = null,
                            Distance = rayMaxDist
                        };
                    }
                }

                Hits[r] = HitLayers[r, 0];
            }
        }

        private void AddDistinctCategoryHit(int rayIndex, double distance, ObjectCategory category)
        {
            for (int h = 0; h < HitsPerRay; h++)
            {
                if (_hitSomethings[rayIndex, h] && _hitTypes[rayIndex, h] == category)
                {
                    if (distance < _hitDists[rayIndex, h])
                    {
                        _hitDists[rayIndex, h] = distance;
                        SortRayHits(rayIndex);
                    }
                    return;
                }
            }

            for (int h = 0; h < HitsPerRay; h++)
            {
                if (!_hitSomethings[rayIndex, h])
                {
                    _hitDists[rayIndex, h] = distance;
                    _hitTypes[rayIndex, h] = category;
                    _hitSomethings[rayIndex, h] = true;
                    SortRayHits(rayIndex);
                    return;
                }
            }

            int last = HitsPerRay - 1;
            if (distance < _hitDists[rayIndex, last])
            {
                _hitDists[rayIndex, last] = distance;
                _hitTypes[rayIndex, last] = category;
                _hitSomethings[rayIndex, last] = true;
                SortRayHits(rayIndex);
            }
        }

        private void SortRayHits(int rayIndex)
        {
            for (int i = 0; i < HitsPerRay - 1; i++)
            {
                for (int j = i + 1; j < HitsPerRay; j++)
                {
                    bool swap = _hitSomethings[rayIndex, j]
                        && (!_hitSomethings[rayIndex, i] || _hitDists[rayIndex, j] < _hitDists[rayIndex, i]);
                    if (!swap)
                        continue;

                    double dist = _hitDists[rayIndex, i];
                    ObjectCategory type = _hitTypes[rayIndex, i];
                    bool hit = _hitSomethings[rayIndex, i];

                    _hitDists[rayIndex, i] = _hitDists[rayIndex, j];
                    _hitTypes[rayIndex, i] = _hitTypes[rayIndex, j];
                    _hitSomethings[rayIndex, i] = _hitSomethings[rayIndex, j];

                    _hitDists[rayIndex, j] = dist;
                    _hitTypes[rayIndex, j] = type;
                    _hitSomethings[rayIndex, j] = hit;
                }
            }
        }

        /// <summary>
        /// Fills the provided buffer with the given extras prepended and then the ray signals.
        /// </summary>
        public void FillInputs(double[] buffer, double hpDeficit, double[] memory)
        {
            buffer[0] = hpDeficit;
            int offset = 1;
            Array.Clear(buffer, offset, SmartObject.MemorySize);
            if (memory != null && memory.Length > 0)
            {
                Array.Copy(memory, 0, buffer, offset, Math.Min(memory.Length, SmartObject.MemorySize));
            }
            offset += SmartObject.MemorySize;
            Array.Copy(Signals, 0, buffer, offset, Signals.Length);
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
