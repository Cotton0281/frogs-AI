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
        public List<NeuralLayerDefinition> LayerDefinitions { get; set; } = new List<NeuralLayerDefinition>();

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
            EnsureLayerDefinitions();
        }
        public static NeuroNetStructure Small_1Lx9N()
        {
            var structure = new NeuroNetStructure()
            {
                HiddenLayers = 1,
                NeuronsInHiddenLayer = 18,
                Id = "Small"
            };
            structure.EnsureLayerDefinitions();
            return structure;
        }

        public static NeuroNetStructure Mid_3Lx10N()
        {
            var structure = new NeuroNetStructure()
            {
                HiddenLayers = 3,
                Id = "Medium",
                NeuronsInHiddenLayer = 13,
            };
            structure.EnsureLayerDefinitions();
            return structure;
        }

        public static NeuroNetStructure Big_5Lx20N()
        {
            var structure = new NeuroNetStructure()
            {
                Id = "Large",
                HiddenLayers = 5,
                NeuronsInHiddenLayer = 20
            };
            structure.EnsureLayerDefinitions();
            return structure;
        }

        public static NeuroNetStructure FromGene(NeuralNetworkGene gene)
        {
            if (gene == null || gene.HiddenGenes == null || gene.HiddenGenes.Count == 0)
                return null;

            int hiddenLayers = gene.HiddenGenes.Count;
            int neuronsInHiddenLayer = gene.HiddenGenes[0]?.Neurons?.Count ?? 0;
            bool uniformDense = gene.HiddenGenes.All(layer =>
                layer != null
                && layer.Kind == NeuralLayerKind.Dense
                && layer.Neurons?.Count == neuronsInHiddenLayer);

            if (uniformDense && hiddenLayers == 1 && neuronsInHiddenLayer == 18)
                return Small_1Lx9N();
            if (uniformDense && hiddenLayers == 3 && neuronsInHiddenLayer == 13)
                return Mid_3Lx10N();
            if (uniformDense && hiddenLayers == 5 && neuronsInHiddenLayer == 20)
                return Big_5Lx20N();

            var structure = new NeuroNetStructure(
                gene.InputGene?.Neurons?.Count ?? InputsDefault,
                gene.OutputGene?.Neurons?.Count ?? OutputsDefault,
                hiddenLayers,
                neuronsInHiddenLayer)
            {
                Id = $"Custom ({hiddenLayers} layers)",
                LayerDefinitions = gene.HiddenGenes
                    .Select(layer => new NeuralLayerDefinition
                    {
                        NeuronCount = layer?.Neurons?.Count ?? 0,
                        Kind = layer?.Kind ?? NeuralLayerKind.Dense
                    })
                    .ToList()
            };
            return structure;
        }

        public IReadOnlyList<NeuralLayerDefinition> GetLayerDefinitions()
        {
            EnsureLayerDefinitions();
            return LayerDefinitions;
        }

        public void EnsureLayerDefinitions()
        {
            if (LayerDefinitions == null)
                LayerDefinitions = new List<NeuralLayerDefinition>();

            if (LayerDefinitions.Count == 0 && HiddenLayers > 0 && NeuronsInHiddenLayer > 0)
            {
                for (int i = 0; i < HiddenLayers; i++)
                {
                    LayerDefinitions.Add(new NeuralLayerDefinition
                    {
                        NeuronCount = NeuronsInHiddenLayer,
                        Kind = NeuralLayerKind.Dense
                    });
                }
            }

            HiddenLayers = LayerDefinitions.Count;
            if (LayerDefinitions.Count > 0)
                NeuronsInHiddenLayer = LayerDefinitions[0].NeuronCount;
        }

        public const int InputsDefault = SmartObject.InputCount;
        public const int OutputsDefault = SmartObject.OutputCount;
    }

    public class NeuralLayerDefinition
    {
        public int NeuronCount { get; set; }
        public NeuralLayerKind Kind { get; set; } = NeuralLayerKind.Dense;
    }

}
