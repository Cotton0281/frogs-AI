using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_Evlo_Test.Objects
{
    public class TargetObj : BasicObject, ISensable
    {
        public double HpCharge = 1;
        public int ObjectsOnTop = 0;
        private double underwater = 0;

        // --- Raft visual animation state (real-time based) ---
        /// <summary>Current sprite-sheet frame index for the raft float animation.</summary>
        public int SpriteFrameIndex;
        /// <summary>Wall-clock time at which the raft should advance to its next sprite frame.</summary>
        public DateTime NextSpriteChangeTime = DateTime.Now;
        /// <summary>Current raft rotation speed in degrees per second (kept within ±5).</summary>
        public double RotationDegPerSec;
        /// <summary>Accumulated raft rotation angle in degrees.</summary>
        public double RotationAngle;

        /// <summary>
        /// Set by the environment each tick (Food for OneTarget, Raft for TwoTargets).
        /// </summary>
        public ObjectCategory Category { get; set; } = ObjectCategory.Food;

        /// <summary>
        /// indicates how deep underwater is raft. Can't go below -10 and above 100
        /// </summary>
        public double Underwater
        {
            get => underwater;
            set
            {
                if (value < -10) underwater = -10;
                else if (value > 100) underwater = 100;
                else { underwater = value; }
            }
        }
    }
}
