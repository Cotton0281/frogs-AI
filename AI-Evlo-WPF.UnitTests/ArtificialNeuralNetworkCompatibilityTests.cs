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

        [TestMethod]
        public void ResidualLayer_WithZeroBranch_PreservesTheNetworkFunction()
        {
            var factory = NeuralNetworkFactory.GetInstance();
            NeuralNetworkGene originalGene = CreateTwoLayerGene(includeResidualLayer: false);
            NeuralNetworkGene grownGene = CreateTwoLayerGene(includeResidualLayer: true);
            INeuralNetwork original = factory.Create(originalGene);
            INeuralNetwork grown = factory.Create(grownGene);

            foreach (double[] inputs in new[]
            {
                new[] { 0.25, -0.5 },
                new[] { -0.9, 0.7 },
                new[] { 0.0, 0.0 }
            })
            {
                original.SetInputs(inputs);
                original.Process();
                grown.SetInputs(inputs);
                grown.Process();

                Assert.AreEqual(original.GetOutputs()[0], grown.GetOutputs()[0], 1e-12);
            }

            Assert.AreEqual(NeuralLayerKind.Residual, grown.GetGenes().HiddenGenes[1].Kind);
        }

        [TestMethod]
        public void Factory_WithResidualLayerDefinition_ZeroInitializesTheResidualBranch()
        {
            INeuralNetworkFactory factory = NeuralNetworkFactory.GetInstance();

            INeuralNetwork network = factory.Create(
                2,
                1,
                new[] { 3, 3 },
                new[] { NeuralLayerKind.Dense, NeuralLayerKind.Residual });

            NeuralNetworkGene gene = network.GetGenes();
            Assert.AreEqual(NeuralLayerKind.Residual, gene.HiddenGenes[1].Kind);
            Assert.IsTrue(gene.HiddenGenes[0].Neurons
                .SelectMany(neuron => neuron.Axon.Weights)
                .All(weight => weight == 0));
            Assert.IsTrue(gene.HiddenGenes[1].Neurons.All(neuron => neuron.Soma.Bias == 0));
        }

        private static NeuralNetworkGene CreateTwoLayerGene(bool includeResidualLayer)
        {
            var firstHidden = new LayerGene
            {
                Neurons = new List<NeuronGene>
                {
                    CreateNeuronGene(0.1, includeResidualLayer ? new[] { 0.0, 0.0 } : new[] { 0.7 }),
                    CreateNeuronGene(-0.2, includeResidualLayer ? new[] { 0.0, 0.0 } : new[] { -0.2 })
                }
            };

            var hiddenLayers = new List<LayerGene> { firstHidden };
            if (includeResidualLayer)
            {
                hiddenLayers.Add(new LayerGene
                {
                    Kind = NeuralLayerKind.Residual,
                    Neurons = new List<NeuronGene>
                    {
                        CreateNeuronGene(0, 0.7),
                        CreateNeuronGene(0, -0.2)
                    }
                });
            }

            return new NeuralNetworkGene
            {
                InputGene = new LayerGene
                {
                    Neurons = new List<NeuronGene>
                    {
                        CreateNeuronGene(0, 0.4, -0.1),
                        CreateNeuronGene(0, -0.3, 0.8)
                    }
                },
                HiddenGenes = hiddenLayers,
                OutputGene = new LayerGene
                {
                    Neurons = new List<NeuronGene> { CreateNeuronGene(0.05) }
                }
            };
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
