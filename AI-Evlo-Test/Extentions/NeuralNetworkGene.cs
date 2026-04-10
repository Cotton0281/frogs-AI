using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArtificialNeuralNetwork.Genes;


namespace AI_Evlo_Test.Extentions
{

    public static class NeuralNetworkGeneExtender
    {
        public static string ToStringValues(this NeuralNetworkGene neuralNetworkGene)
        {
            StringBuilder strValues = new StringBuilder();
            int numLayers = neuralNetworkGene.HiddenGenes.Count;
            //soma is input, Axon is output

            for (int i = 0; i < numLayers; i++)
            {
                LayerGene GayerG = neuralNetworkGene.HiddenGenes[i];
                int numNeurons = GayerG.Neurons.Count;
                strValues.AppendLine().Append("Layer").Append(i);
                for (int i1 = 0; i1 < numNeurons; i1++)
                {
                    NeuronGene NeuronG = GayerG.Neurons[i1];
                    strValues.AppendLine().Append($"Neuron{i1} Bias {NeuronG.Soma.Bias} ,");
                    for (int i2 = 0; i2 < NeuronG.Axon.Weights.Count; i2++)
                    {
                        strValues.Append($" {NeuronG.Axon.Weights[i2]}");
                    }
                }
            }


            return strValues.ToString();
        }
    }


}
