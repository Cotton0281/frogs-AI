using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_Evlo_Test.ConfigLib
{
    
   public class NeuroNetStructure
    {
        public int Inputs = 26; // 1 HP deficit + 1 stamina deficit + 12 rays x 2 (distance + type)
        public int Outputs = 2;
        public int HiddenLayers ;
        public int NeuronsInHiddenLayer ;
        public string Id { get; internal set; }

        /// <summary>
        /// One hiden layer with 9 neurons
        /// </summary>
        /// <returns></returns>
        /// 

        private NeuroNetStructure() { }
        public NeuroNetStructure (int Inputs, int Outputs, int HiddenLayers, int NeuronsInHiddenLayer)
        {
            this.Inputs = Inputs;
            this.Outputs = Outputs;
            this.HiddenLayers = HiddenLayers;
            this.NeuronsInHiddenLayer = NeuronsInHiddenLayer;
            this.Id = Guid.NewGuid().ToString();
    }
        public static NeuroNetStructure Small_1Lx9N()
        {
            return new NeuroNetStructure()
            {
                HiddenLayers = 1,
                NeuronsInHiddenLayer = 18,
                Id = "Small"
            };
        }

        public static NeuroNetStructure Mid_3Lx10N()
        {
            return new NeuroNetStructure()
            {
                HiddenLayers = 3,
                Id = "Medium",
                NeuronsInHiddenLayer = 13,
            };
        }

        public static NeuroNetStructure Big_5Lx20N()
        {
            return new NeuroNetStructure()
            {
                Id = "Large",
                HiddenLayers = 5,
                NeuronsInHiddenLayer = 20
            };
        }
    }

}
