using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.WeightInitializer;
//using NeuralNetwork.GeneticAlgorithm.Evolution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArtificialNeuralNetwork.Genes;
using AI_Evlo_Test.Enumerators;

namespace AI_Evlo_Test.Objects
{
    public class EvolutionChember
    {
        static Random Rnd = new Random();
        static readonly object RndLock = new object();
        RandomWeightInitializer randomInit = new RandomWeightInitializer(Rnd);
        public delegate void Message_Handler(string Message);
        public event Message_Handler NewMessage;

        private void NotifyMessage(string message)
        {
            NewMessage?.Invoke(message);
        }

        public EvolutionChember()
        {
            lock (RndLock)
                Rnd = new Random(DateTime.Now.DayOfYear * 1000 + DateTime.Now.Millisecond);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="MutatingNeuroNet"></param>
        /// <param name="MutationRate">0 to 100 Percent of neuron wights to mutate</param>
        /// <returns></returns>
        public INeuralNetwork MutateNN(INeuralNetwork MutatingNeuroNet, double MutationRate, bool MutRateInPercent = true)
        {
            //soma is input, Axon is output
            if (MutatingNeuroNet != null)
            {
                var originalGenes = MutatingNeuroNet.GetGenes();
                var mutGenes = MutateGenom(originalGenes, MutationRate, MutRateInPercent);

                var nnFak = NeuralNetworkFactory.GetInstance();
                MutatingNeuroNet = nnFak.Create(mutGenes);
            }
            return MutatingNeuroNet;
        }

        public NeuralNetworkGene MutateGenom(NeuralNetworkGene originalGenes, double Mutations, bool MutRateInPercent = true)
        {
            List<NeuronsIndex> lsIndex = IndexGene(originalGenes);
            int numMutations;

            if (MutRateInPercent)
            {
                if (Mutations > 100)
                    Mutations = 100;
                else if (Mutations < 0)
                    Mutations = 0;


                int ValuesCount = lsIndex.Count;
                numMutations = (int)((Mutations * ValuesCount) / 100);  // percent of neurons to mutate
            }

            else
            {
                numMutations = (int)Mutations;
            }
            if (numMutations == 0 && Mutations > 0)
                numMutations = 1;// make at least 1 change

            // Fisher-Yates partial shuffle: swap selected items to the end of the list — O(1) per pick
            int remaining = lsIndex.Count;
            for (int i = 0; i < numMutations; i++)
            {
                int pick = NextRandom(0, remaining);
                // Swap picked element to the back partition
                var temp = lsIndex[pick];
                lsIndex[pick] = lsIndex[remaining - 1];
                lsIndex[remaining - 1] = temp;
                remaining--;
            }

            // The last numMutations elements are the selected mutation targets
            for (int mi = lsIndex.Count - numMutations; mi < lsIndex.Count; mi++)
            {
                NeuronsIndex indx = lsIndex[mi];
                NeuronGene NeuronG = null;
                // Locate neurone with selected val for mutation
                switch (indx.LayerType)
                {
                    case ELeyerType.Input:
                        NeuronG = originalGenes.InputGene.Neurons[indx.NeuronIndex];
                        break;
                    case ELeyerType.Hidden:
                        NeuronG = originalGenes.HiddenGenes[indx.LayerIndex].Neurons[indx.NeuronIndex];
                        break;
                    case ELeyerType.Output:
                        NeuronG = originalGenes.OutputGene.Neurons[indx.NeuronIndex];
                        break;
                }

                
                // Mutate new vals here
                if (indx.ValueType.Equals(EValueType.Bios))
                {
                    NeuronG.Soma.Bias = NextRandomDouble() * 2 - 1;
                }
                else if (indx.ValueType.Equals(EValueType.Weigth))
                {
                    NeuronG.Axon.Weights[indx.WeigthIndex] = NextRandomDouble() * 2 - 1;
                }
            }
            return originalGenes;
        }

        private static int NextRandom(int minValue, int maxValue)
        {
            lock (RndLock)
                return Rnd.Next(minValue, maxValue);
        }

        private static double NextRandomDouble()
        {
            lock (RndLock)
                return Rnd.NextDouble();
        }

        /// <summary>
        /// Put all weights and bioses in a index
        /// </summary>
        /// <param name="originalGenes"></param>
        /// <returns></returns>
        private static List<NeuronsIndex> IndexGene(NeuralNetworkGene originalGenes)
        {
            // First Index all neurons
            List<NeuronsIndex> lsIndex = new List<NeuronsIndex>();
            // index Imput Layer
            for (int i = 0; i < originalGenes.InputGene.Neurons.Count; i++)
            {
                NeuronsIndex biosIndex = new NeuronsIndex(ELeyerType.Input, 0, i, -1, EValueType.Bios);
                lsIndex.Add(biosIndex);
                for (int iw = 0; iw < originalGenes.InputGene.Neurons[i].Axon.Weights.Count; iw++)
                {
                    NeuronsIndex WIndex = new NeuronsIndex(ELeyerType.Input, 0, i, iw, EValueType.Weigth);
                    lsIndex.Add(WIndex);
                }
            }

            // Index all hidden layers
            for (int iL = 0; iL < originalGenes.HiddenGenes.Count; iL++)
            {
                LayerGene gLayer = originalGenes.HiddenGenes[iL];
                for (int idxNeur = 0; idxNeur < gLayer.Neurons.Count; idxNeur++)
                {
                    NeuronsIndex biosIndex = new NeuronsIndex(ELeyerType.Hidden, iL, idxNeur, -1, EValueType.Bios);
                    lsIndex.Add(biosIndex);
                    for (int i = 0; i < gLayer.Neurons[idxNeur].Axon.Weights.Count; i++)
                    {
                        NeuronsIndex WIndex = new NeuronsIndex(ELeyerType.Hidden, iL, idxNeur, i, EValueType.Weigth);
                        lsIndex.Add(WIndex);
                    }
                }
            }

            //Index Ouput Layer
            for (int iO = 0; iO < originalGenes.OutputGene.Neurons.Count; iO++)
            {
                NeuronsIndex biosIndex = new NeuronsIndex(ELeyerType.Output, 0, iO, -1, EValueType.Bios);
                lsIndex.Add(biosIndex);
            }

            return lsIndex;
        }

        /// <summary>
        /// Create a Clone of NN and generate mutation
        /// </summary>
        /// <param name="MutatingNeuroNet"></param>
        /// <returns>Mutated clone of the Neural Network</returns>
    
    }

    /// <summary>
    /// Use for Mutation
    /// </summary>
    public struct NeuronsIndex
    {
        public ELeyerType LayerType;
        public int LayerIndex;
        public int NeuronIndex;
        public int WeigthIndex;
        public EValueType ValueType;
        public NeuronsIndex(ELeyerType eLayerType, int LayerIndex, int NeuronIndex, int WeigthIndex, EValueType ValueType)
        {
            LayerType = eLayerType;
            this.LayerIndex = LayerIndex;
            this.NeuronIndex = NeuronIndex;
            this.WeigthIndex = WeigthIndex;
            this.ValueType = ValueType;
        }
        public override string ToString()
        {
            string str = LayerType.ToString().Substring(0, 1);
            if (LayerType == ELeyerType.Hidden)
                str += $"[{LayerIndex}]";
            str += $" N[{NeuronIndex}] {ValueType.ToString().Substring(0, 1)}";
            if (ValueType == EValueType.Weigth)
                str += $"[{WeigthIndex}]";

            return str;
        }
    }
}
