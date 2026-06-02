using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.ActivationFunctions;
using ArtificialNeuralNetwork.Genes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace AI_Evlo_WPF.UnitTests
{
    [STATestClass]
    public class ArtificialNeuralNetworkCompatibilityTests
    {
        [TestMethod]
        public void FactoryCreatedNetwork_ProcessesInputsAndRoundTripsGenes()
        {
            var factory = NeuralNetworkFactory.GetInstance();
            var network = factory.Create(2, 1, 1, 3);

            network.SetInputs(new[] { 0.25, -0.5 });
            network.Process();

            double[] outputs = network.GetOutputs();
            var genes = network.GetGenes();
            var rehydrated = factory.Create(genes);

            Assert.HasCount(1, outputs);
            Assert.HasCount(2, genes.InputGene.Neurons);
            Assert.HasCount(1, genes.HiddenGenes);
            Assert.HasCount(1, genes.OutputGene.Neurons);
            Assert.HasCount(1, rehydrated.OutputLayer.NeuronsInLayer);
        }

        [TestMethod]
        public void Process_UsesCurrentInputsToCalculateOutputs()
        {
            var factory = NeuralNetworkFactory.GetInstance();
            var network = factory.Create(new NeuralNetworkGene
            {
                InputGene = new LayerGene
                {
                    Neurons = new List<NeuronGene>
                    {
                        CreateNeuronGene(0, 1),
                        CreateNeuronGene(0, -1)
                    }
                },
                HiddenGenes = new List<LayerGene>(),
                OutputGene = new LayerGene
                {
                    Neurons = new List<NeuronGene>
                    {
                        CreateNeuronGene(0)
                    }
                }
            });

            network.SetInputs(new[] { 1.0, 0.0 });
            network.Process();
            double firstOutput = network.GetOutputs()[0];

            network.SetInputs(new[] { 0.0, 1.0 });
            network.Process();
            double secondOutput = network.GetOutputs()[0];

            Assert.IsGreaterThan(0.1, firstOutput);
            Assert.IsLessThan(-0.1, secondOutput);
        }

        private static NeuronGene CreateNeuronGene(double bias, params double[] weights)
        {
            return new NeuronGene
            {
                Soma = new SomaGene
                {
                    Bias = bias,
                    SummationFunction = typeof(SimpleSummation)
                },
                Axon = new AxonGene
                {
                    ActivationFunction = typeof(TanhActivationFunction),
                    Weights = new List<double>(weights)
                }
            };
        }
    }
}
