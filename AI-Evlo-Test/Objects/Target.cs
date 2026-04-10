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
