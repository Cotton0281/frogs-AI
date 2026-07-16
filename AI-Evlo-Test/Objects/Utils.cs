using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;
using AI_Evlo_Test.ConfigLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_Evlo_Test.Objects
{
    public class Utils
    {
        /// <summary>
        /// Copare 2 neuronets and return the values of the differences
        /// </summary>
        /// <param name="Gene1"></param>
        /// <param name="Gene2"></param>
        /// <returns></returns>
        static public string GetDifferences(NeuralNetworkGene Gene1, NeuralNetworkGene Gene2)
        {
            StringBuilder msg = new StringBuilder();
            int numLayers = Gene1.HiddenGenes.Count;
            //soma is input, Axon is output


            string inputDiff = CompareLayers(Gene1.InputGene, Gene2.InputGene);
            if (!string.IsNullOrEmpty(inputDiff))
            {
                msg.AppendLine();
                msg.AppendLine("Input:");
                msg.Append(inputDiff);
            }

            for (int i = 0; i < numLayers; i++)
            {
                msg.Append("Layer ").Append(i).AppendLine();
                msg.Append(CompareLayers(Gene1.HiddenGenes[i], Gene2.HiddenGenes[i]));
            }

            msg.AppendLine();
            msg.AppendLine("Output:");
            msg.Append(CompareLayers(Gene1.OutputGene, Gene2.OutputGene));

            return msg.ToString();
        }

        private static string CompareLayers( LayerGene Layer1, LayerGene Layer2)
        {
            StringBuilder msg = new StringBuilder();
            int numNeurons = Layer1.Neurons.Count;

            for (int i1 = 0; i1 < numNeurons; i1++)
            {
                NeuronGene NeuronG = Layer1.Neurons[i1];
                if (NeuronG.Soma.Bias != Layer2.Neurons[i1].Soma.Bias)
                {
                    msg.Append($" N{i1} B{NeuronG.Soma.Bias} => {Layer2.Neurons[i1].Soma.Bias}");
                    msg.AppendLine();
                }

                for (int i2 = 0; i2 < NeuronG.Axon.Weights.Count; i2++)
                {
                    if (NeuronG.Axon.Weights[i2] != Layer2.Neurons[i1].Axon.Weights[i2])
                    {
                        msg.Append($" N{i1} W{i2} {NeuronG.Axon.Weights[i2]} => {Layer2.Neurons[i1].Axon.Weights[i2]}");
                        msg.AppendLine();
                    }
                }
            }
            return msg.ToString();
        }

        public static INeuralNetwork CloneNeuroNet(INeuralNetwork NeuroNet)
        {
            if (NeuroNet == null)
                return null;

            NeuralNetworkGene clonedGenes = CloneGene( NeuroNet.GetGenes());

            NeuralNetworkFactory NNetworkFactory = NeuralNetworkFactory.GetInstance();
            INeuralNetwork cloneNeuralNetwork = NNetworkFactory.Create(clonedGenes);
            return cloneNeuralNetwork;
        }

        public static bool HasSameTopology(NeuralNetworkGene gene1, NeuralNetworkGene gene2)
        {
            if (gene1 == null || gene2 == null)
                return false;

            return HasSameLayerTopology(gene1.InputGene, gene2.InputGene)
                && HasSameLayerSetTopology(gene1.HiddenGenes, gene2.HiddenGenes)
                && HasSameLayerTopology(gene1.OutputGene, gene2.OutputGene);
        }

        public static bool MatchesStructure(NeuralNetworkGene gene, NeuroNetStructure structure)
        {
            if (gene == null || structure == null)
                return false;

            if (gene.InputGene?.Neurons == null || gene.InputGene.Neurons.Count != structure.Inputs)
                return false;

            if (gene.OutputGene?.Neurons == null || gene.OutputGene.Neurons.Count != structure.Outputs)
                return false;

            if (gene.HiddenGenes == null || gene.HiddenGenes.Count != structure.HiddenLayers)
                return false;

            for (int i = 0; i < gene.HiddenGenes.Count; i++)
            {
                IReadOnlyList<NeuralLayerDefinition> definitions = structure.GetLayerDefinitions();
                if (i >= definitions.Count
                    || gene.HiddenGenes[i]?.Neurons == null
                    || gene.HiddenGenes[i].Neurons.Count != definitions[i].NeuronCount
                    || gene.HiddenGenes[i].Kind != definitions[i].Kind)
                    return false;
            }

            return true;
        }

        public static NeuralNetworkGene IncrementalAverageGene(NeuralNetworkGene currentAverage, NeuralNetworkGene newValue, int currentCount)
        {
            if (newValue == null)
                return null;

            if (currentAverage == null || currentCount <= 0)
                return CloneGene(newValue);

            if (!HasSameTopology(currentAverage, newValue))
                return null;

            NeuralNetworkGene averaged = CloneGene(currentAverage);
            AverageLayer(averaged.InputGene, newValue.InputGene, currentCount);
            for (int i = 0; i < averaged.HiddenGenes.Count; i++)
                AverageLayer(averaged.HiddenGenes[i], newValue.HiddenGenes[i], currentCount);
            AverageLayer(averaged.OutputGene, newValue.OutputGene, currentCount);
            return averaged;
        }

        static public NeuralNetworkGene CloneGene(NeuralNetworkGene Gene)
        {
            NeuralNetworkGene newGene = new NeuralNetworkGene();
            newGene.InputGene = new LayerGene { Kind = Gene.InputGene.Kind };
            newGene.InputGene.Neurons = new List<NeuronGene>();

            for (int in1 = 0; in1 < Gene.InputGene.Neurons.Count; in1++)
            {
                NeuronGene newNeuron = CloneNeuron(Gene.InputGene.Neurons[in1]);
                newGene.InputGene.Neurons.Add(newNeuron);
            }


            int numLayers = Gene.HiddenGenes.Count;
            newGene.HiddenGenes = new List<LayerGene>();
            for (int i = 0; i < numLayers; i++)
            {
                LayerGene newLayer = new LayerGene { Kind = Gene.HiddenGenes[i].Kind };
                newLayer.Neurons = new List<NeuronGene>();

                newGene.HiddenGenes.Add(newLayer);
                int numNeurons = Gene.HiddenGenes[i].Neurons.Count;

                for (int i1 = 0; i1 < numNeurons; i1++)
                {
                    NeuronGene newNeuron = CloneNeuron(Gene.HiddenGenes[i].Neurons[i1]);
                    newLayer.Neurons.Add(newNeuron);
                }
            }

            //Output layer
            newGene.OutputGene = new LayerGene { Kind = Gene.OutputGene.Kind };
            newGene.OutputGene.Neurons = new List<NeuronGene>();

            for (int in1 = 0; in1 < Gene.OutputGene.Neurons.Count; in1++)
            {
                NeuronGene newNeuron = CloneNeuron(Gene.OutputGene.Neurons[in1]);
                newGene.OutputGene.Neurons.Add(newNeuron);
            }
             

            return newGene;

        }

        private static NeuronGene CloneNeuron(NeuronGene OriginalNeuron)
        {
            NeuronGene newNeuron = new NeuronGene()
            {
                Soma = new SomaGene()
                {
                    Bias = OriginalNeuron.Soma.Bias,
                    SummationFunction = OriginalNeuron.Soma.SummationFunction
                },
                Axon = new AxonGene()
                {
                    ActivationFunction = OriginalNeuron.Axon.ActivationFunction
                }
            };
            newNeuron.Axon.Weights = new List<double>();

            for (int i2 = 0; i2 < OriginalNeuron.Axon.Weights.Count; i2++)
            {
                newNeuron.Axon.Weights.Add(OriginalNeuron.Axon.Weights[i2]);
            }
            return newNeuron;
        }

        private static bool HasSameLayerSetTopology(IList<LayerGene> layers1, IList<LayerGene> layers2)
        {
            if (layers1 == null || layers2 == null || layers1.Count != layers2.Count)
                return false;

            for (int i = 0; i < layers1.Count; i++)
                if (!HasSameLayerTopology(layers1[i], layers2[i]))
                    return false;

            return true;
        }

        private static bool HasSameLayerTopology(LayerGene layer1, LayerGene layer2)
        {
            if (layer1?.Neurons == null
                || layer2?.Neurons == null
                || layer1.Kind != layer2.Kind
                || layer1.Neurons.Count != layer2.Neurons.Count)
                return false;

            for (int i = 0; i < layer1.Neurons.Count; i++)
            {
                IList<double> weights1 = layer1.Neurons[i]?.Axon?.Weights;
                IList<double> weights2 = layer2.Neurons[i]?.Axon?.Weights;
                if (weights1 == null || weights2 == null || weights1.Count != weights2.Count)
                    return false;
            }

            return true;
        }

        private static void AverageLayer(LayerGene averageLayer, LayerGene newLayer, int currentCount)
        {
            for (int neuronIndex = 0; neuronIndex < averageLayer.Neurons.Count; neuronIndex++)
                AverageNeuron(averageLayer.Neurons[neuronIndex], newLayer.Neurons[neuronIndex], currentCount);
        }

        private static void AverageNeuron(NeuronGene averageNeuron, NeuronGene newNeuron, int currentCount)
        {
            averageNeuron.Soma.Bias = IncrementalAverage(averageNeuron.Soma.Bias, newNeuron.Soma.Bias, currentCount);

            for (int weightIndex = 0; weightIndex < averageNeuron.Axon.Weights.Count; weightIndex++)
            {
                averageNeuron.Axon.Weights[weightIndex] = IncrementalAverage(
                    averageNeuron.Axon.Weights[weightIndex],
                    newNeuron.Axon.Weights[weightIndex],
                    currentCount);
            }
        }

        private static double IncrementalAverage(double average, double newValue, int currentCount)
        {
            int cappedCount = Math.Min(currentCount, 100);
            return average + (newValue - average) / (cappedCount + 1);
        }
    }
}
