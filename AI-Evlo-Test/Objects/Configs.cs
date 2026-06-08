using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArtificialNeuralNetwork.Genes;
using AI_Evlo_Test.Objects;

namespace AI_Evlo_Test.ConfigLib
{
    
   public class NeuroNetStructure
    {
        public int Inputs = InputsDefault; // HP deficit + memory + 12 rays x 2 hits x 2 values
        public int Outputs = OutputsDefault; // rotation + thrust + memory writes
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

        public static NeuroNetStructure FromGene(NeuralNetworkGene gene)
        {
            if (gene == null || gene.HiddenGenes == null || gene.HiddenGenes.Count == 0)
                return null;

            int hiddenLayers = gene.HiddenGenes.Count;
            int neuronsInHiddenLayer = gene.HiddenGenes[0]?.Neurons?.Count ?? 0;

            if (hiddenLayers == 1 && neuronsInHiddenLayer == 18)
                return Small_1Lx9N();
            if (hiddenLayers == 3 && neuronsInHiddenLayer == 13)
                return Mid_3Lx10N();
            if (hiddenLayers == 5 && neuronsInHiddenLayer == 20)
                return Big_5Lx20N();

            return new NeuroNetStructure(
                gene.InputGene?.Neurons?.Count ?? InputsDefault,
                gene.OutputGene?.Neurons?.Count ?? OutputsDefault,
                hiddenLayers,
                neuronsInHiddenLayer)
            {
                Id = $"Custom {hiddenLayers}Lx{neuronsInHiddenLayer}N"
            };
        }

        public const int InputsDefault = SmartObject.InputCount;
        public const int OutputsDefault = SmartObject.OutputCount;
    }

}
