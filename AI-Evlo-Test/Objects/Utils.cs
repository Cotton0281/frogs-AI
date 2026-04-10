using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;
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
        static public NeuralNetworkGene CloneGene(NeuralNetworkGene Gene)
        {
            NeuralNetworkGene newGene = new NeuralNetworkGene();
            newGene.InputGene = new LayerGene()  ;
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
                LayerGene newLayer = new LayerGene();
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
            newGene.OutputGene = new LayerGene();
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
    }
}
