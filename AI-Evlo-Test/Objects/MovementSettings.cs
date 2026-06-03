using System;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Runtime-tunable costs for neural-network movement outputs.
    /// </summary>
    public sealed class MovementSettings
    {
        public const double DefaultRotationHpCost = 0.01;
        public const double DefaultThrustHpCost = 0.04;
        public const double DefaultLandedBirdSpeedMultiplier = 0.1;
        public const int DefaultBiteHpAmount = 100;
        public const int DefaultBiteCooldownTicks = 5;
        public const double DefaultPredatorBiteHpThreshold = 0.8;

        public double RotationHpCost { get; set; } = DefaultRotationHpCost;
        public double ThrustHpCost { get; set; } = DefaultThrustHpCost;
        public double LandedBirdSpeedMultiplier { get; set; } = DefaultLandedBirdSpeedMultiplier;
        public int BiteHpAmount { get; set; } = DefaultBiteHpAmount;
        public int BiteCooldownTicks { get; set; } = DefaultBiteCooldownTicks;
        public double PredatorBiteHpThreshold { get; set; } = DefaultPredatorBiteHpThreshold;

        public MovementSettings Clone()
        {
            return new MovementSettings
            {
                RotationHpCost = RotationHpCost,
                ThrustHpCost = ThrustHpCost,
                LandedBirdSpeedMultiplier = LandedBirdSpeedMultiplier,
                BiteHpAmount = BiteHpAmount,
                BiteCooldownTicks = BiteCooldownTicks,
                PredatorBiteHpThreshold = PredatorBiteHpThreshold
            };
        }

        public void Normalize()
        {
            if (double.IsNaN(RotationHpCost) || double.IsInfinity(RotationHpCost) || RotationHpCost < 0)
                RotationHpCost = 0;

            if (double.IsNaN(ThrustHpCost) || double.IsInfinity(ThrustHpCost) || ThrustHpCost < 0)
                ThrustHpCost = 0;

            if (double.IsNaN(LandedBirdSpeedMultiplier) ||
                double.IsInfinity(LandedBirdSpeedMultiplier) ||
                LandedBirdSpeedMultiplier <= 0)
            {
                LandedBirdSpeedMultiplier = DefaultLandedBirdSpeedMultiplier;
            }

            LandedBirdSpeedMultiplier = Math.Min(1.0, LandedBirdSpeedMultiplier);

            if (BiteHpAmount < 1)
                BiteHpAmount = 1;

            if (BiteCooldownTicks < 0)
                BiteCooldownTicks = 0;

            if (double.IsNaN(PredatorBiteHpThreshold) ||
                double.IsInfinity(PredatorBiteHpThreshold) ||
                PredatorBiteHpThreshold <= 0 ||
                PredatorBiteHpThreshold >= 1)
            {
                PredatorBiteHpThreshold = DefaultPredatorBiteHpThreshold;
            }
        }
    }
}
