using AI_Evlo_Test.Enumerators;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace AI_Evlo_WPF.UnitTests.Objects
{
    [STATestClass]
    public class EvolutionChemberTests
    {
        [TestMethod]
        public void Constructor_WhenCalled_InitializesInstance()
        {
            // Arrange & Act
            var evolutionChember = new EvolutionChember();

            // Assert
            Assert.IsNotNull(evolutionChember);
        }

        [TestMethod]
        public void MutateGenom_WithSameSeed_ProducesSameMutation()
        {
            NeuralNetworkGene first = CreateTestGenesWithHiddenLayer();
            NeuralNetworkGene second = CreateTestGenesWithHiddenLayer();

            new EvolutionChember(12345).MutateGenom(first, 4, false);
            new EvolutionChember(12345).MutateGenom(second, 4, false);

            CollectionAssert.AreEqual(FlattenGeneValues(first).ToList(), FlattenGeneValues(second).ToList());
        }

        [TestMethod]
        public void MutateGenom_AtOneHundredPercent_DoesNotMutateInputBiases()
        {
            NeuralNetworkGene genes = CreateTestGenesWithHiddenLayer();
            double[] inputBiases = genes.InputGene.Neurons.Select(neuron => neuron.Soma.Bias).ToArray();

            new EvolutionChember(7).MutateGenom(genes, 100, true);

            CollectionAssert.AreEqual(inputBiases, genes.InputGene.Neurons.Select(neuron => neuron.Soma.Bias).ToArray());
        }

        [TestMethod]
        public void MutateGenom_WithLayerLocks_MutatesOnlyParametersOwnedByUnlockedDestinationLayers()
        {
            NeuralNetworkGene genes = CreateLayerOwnershipGene();

            new EvolutionChember(17).MutateGenom(
                genes,
                100,
                true,
                new[] { true, false, true });

            Assert.AreEqual(0.1, genes.InputGene.Neurons[0].Axon.Weights[0], 1e-12, "H1 incoming weight is locked.");
            Assert.AreEqual(0.2, genes.HiddenGenes[0].Neurons[0].Soma.Bias, 1e-12, "H1 bias is locked.");
            Assert.AreNotEqual(0.3, genes.HiddenGenes[0].Neurons[0].Axon.Weights[0], "H2 incoming weight is unlocked.");
            Assert.AreNotEqual(0.4, genes.HiddenGenes[1].Neurons[0].Soma.Bias, "H2 bias is unlocked.");
            Assert.AreEqual(0.5, genes.HiddenGenes[1].Neurons[0].Axon.Weights[0], 1e-12, "Output incoming weight is locked.");
            Assert.AreEqual(0.6, genes.OutputGene.Neurons[0].Soma.Bias, 1e-12, "Output bias is locked.");
        }

        [TestMethod]
        public void MutateGenom_WhenAllDestinationLayersAreLocked_LeavesGeneUnchanged()
        {
            NeuralNetworkGene genes = CreateLayerOwnershipGene();
            double[] before = FlattenGeneValues(genes).ToArray();

            new EvolutionChember(17).MutateGenom(
                genes,
                10,
                false,
                new[] { true, true, true });

            CollectionAssert.AreEqual(before, FlattenGeneValues(genes).ToArray());
        }

        [TestMethod]
        public void MutateNN_NullNetwork_ReturnsNull()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();

            // Act
            var result = evolutionChember.MutateNN(null, 50);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void MutateNN_ValidNetworkWithPercentRate_ReturnsMutatedNetwork()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var mockNetwork = new Mock<INeuralNetwork>();
            var genes = CreateTestGenes();
            mockNetwork.Setup(n => n.GetGenes()).Returns(genes);

            // Act
            var result = evolutionChember.MutateNN(mockNetwork.Object, 50, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateNN_ValidNetworkWithAbsoluteRate_ReturnsMutatedNetwork()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var mockNetwork = new Mock<INeuralNetwork>();
            var genes = CreateTestGenes();
            mockNetwork.Setup(n => n.GetGenes()).Returns(genes);

            // Act
            var result = evolutionChember.MutateNN(mockNetwork.Object, 2, false);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_PercentRateAbove100_ClampsTo100()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, 150, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_PercentRateBelowZero_ClampsToZero()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, -10, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_PercentRateZero_ReturnsOriginalGenes()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, 0, true);

            // Assert
            Assert.AreSame(genes, result);
        }

        [TestMethod]
        public void MutateGenom_SmallPercentRateAboveZero_MutatesAtLeastOne()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();
            var originalBias = genes.InputGene.Neurons[0].Soma.Bias;

            // Act
            var result = evolutionChember.MutateGenom(genes, 0.1, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_AbsoluteRateMode_UsesAbsoluteCount()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, 3, false);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_ValidGenes_MutatesInputLayerBias()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, 100, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_ValidGenes_MutatesInputLayerWeights()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, 100, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_ValidGenes_MutatesHiddenLayerBias()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenesWithHiddenLayer();

            // Act
            var result = evolutionChember.MutateGenom(genes, 100, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_ValidGenes_MutatesHiddenLayerWeights()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenesWithHiddenLayer();

            // Act
            var result = evolutionChember.MutateGenom(genes, 100, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_ValidGenes_MutatesOutputLayerBias()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, 100, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void NeuronsIndex_Constructor_SetsAllProperties()
        {
            // Arrange
            var layerType = ELeyerType.Hidden;
            var layerIndex = 2;
            var neuronIndex = 3;
            var weightIndex = 4;
            var valueType = EValueType.Weigth;

            // Act
            var neuronsIndex = new NeuronsIndex(layerType, layerIndex, neuronIndex, weightIndex, valueType);

            // Assert
            Assert.AreEqual(layerType, neuronsIndex.LayerType);
            Assert.AreEqual(layerIndex, neuronsIndex.LayerIndex);
            Assert.AreEqual(neuronIndex, neuronsIndex.NeuronIndex);
            Assert.AreEqual(weightIndex, neuronsIndex.WeigthIndex);
            Assert.AreEqual(valueType, neuronsIndex.ValueType);
        }

        [TestMethod]
        public void NeuronsIndex_ToString_InputLayerBias_ReturnsCorrectFormat()
        {
            // Arrange
            var neuronsIndex = new NeuronsIndex(ELeyerType.Input, 0, 5, -1, EValueType.Bios);

            // Act
            var result = neuronsIndex.ToString();

            // Assert
            Assert.AreEqual("I N[5] B", result);
        }

        [TestMethod]
        public void NeuronsIndex_ToString_InputLayerWeight_ReturnsCorrectFormat()
        {
            // Arrange
            var neuronsIndex = new NeuronsIndex(ELeyerType.Input, 0, 3, 7, EValueType.Weigth);

            // Act
            var result = neuronsIndex.ToString();

            // Assert
            Assert.AreEqual("I N[3] W[7]", result);
        }

        [TestMethod]
        public void NeuronsIndex_ToString_HiddenLayerBias_ReturnsCorrectFormat()
        {
            // Arrange
            var neuronsIndex = new NeuronsIndex(ELeyerType.Hidden, 2, 4, -1, EValueType.Bios);

            // Act
            var result = neuronsIndex.ToString();

            // Assert
            Assert.AreEqual("H[2] N[4] B", result);
        }

        [TestMethod]
        public void NeuronsIndex_ToString_HiddenLayerWeight_ReturnsCorrectFormat()
        {
            // Arrange
            var neuronsIndex = new NeuronsIndex(ELeyerType.Hidden, 1, 2, 3, EValueType.Weigth);

            // Act
            var result = neuronsIndex.ToString();

            // Assert
            Assert.AreEqual("H[1] N[2] W[3]", result);
        }

        [TestMethod]
        public void NeuronsIndex_ToString_OutputLayerBias_ReturnsCorrectFormat()
        {
            // Arrange
            var neuronsIndex = new NeuronsIndex(ELeyerType.Output, 0, 1, -1, EValueType.Bios);

            // Act
            var result = neuronsIndex.ToString();

            // Assert
            Assert.AreEqual("O N[1] B", result);
        }

        [TestMethod]
        public void NeuronsIndex_ToString_OutputLayerWeight_ReturnsCorrectFormat()
        {
            // Arrange
            var neuronsIndex = new NeuronsIndex(ELeyerType.Output, 0, 0, 5, EValueType.Weigth);

            // Act
            var result = neuronsIndex.ToString();

            // Assert
            Assert.AreEqual("O N[0] W[5]", result);
        }

        [TestMethod]
        public void MutateGenom_HalfPercentRate_MutatesApproximatelyHalf()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, 50, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_AbsoluteRateZero_ReturnsOriginalGenes()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, 0, false);

            // Assert
            Assert.AreSame(genes, result);
        }

        [TestMethod]
        public void MutateGenom_LargeAbsoluteRate_MutatesWithinBounds()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, 1000, false);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_MultipleHiddenLayers_MutatesCorrectly()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenesWithMultipleHiddenLayers();

            // Act
            var result = evolutionChember.MutateGenom(genes, 50, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void NeuronsIndex_Constructor_InputLayer_InitializesCorrectly()
        {
            // Arrange & Act
            var neuronsIndex = new NeuronsIndex(ELeyerType.Input, 0, 0, 0, EValueType.Bios);

            // Assert
            Assert.AreEqual(ELeyerType.Input, neuronsIndex.LayerType);
            Assert.AreEqual(0, neuronsIndex.LayerIndex);
            Assert.AreEqual(0, neuronsIndex.NeuronIndex);
            Assert.AreEqual(0, neuronsIndex.WeigthIndex);
            Assert.AreEqual(EValueType.Bios, neuronsIndex.ValueType);
        }

        [TestMethod]
        public void NeuronsIndex_Constructor_OutputLayer_InitializesCorrectly()
        {
            // Arrange & Act
            var neuronsIndex = new NeuronsIndex(ELeyerType.Output, 5, 10, 15, EValueType.Weigth);

            // Assert
            Assert.AreEqual(ELeyerType.Output, neuronsIndex.LayerType);
            Assert.AreEqual(5, neuronsIndex.LayerIndex);
            Assert.AreEqual(10, neuronsIndex.NeuronIndex);
            Assert.AreEqual(15, neuronsIndex.WeigthIndex);
            Assert.AreEqual(EValueType.Weigth, neuronsIndex.ValueType);
        }

        [TestMethod]
        public void MutateGenom_PerciselyOnePercent_MutatesAtLeastOne()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateLargeTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, 1, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateGenom_Precisely100Percent_MutatesAllValues()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var genes = CreateTestGenes();

            // Act
            var result = evolutionChember.MutateGenom(genes, 100, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateNN_ZeroMutationRate_ReturnsNewNetwork()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var mockNetwork = new Mock<INeuralNetwork>();
            var genes = CreateTestGenes();
            mockNetwork.Setup(n => n.GetGenes()).Returns(genes);

            // Act
            var result = evolutionChember.MutateNN(mockNetwork.Object, 0, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateNN_100PercentMutationRate_ReturnsNewNetwork()
        {
            // Arrange
            var evolutionChember = new EvolutionChember();
            var mockNetwork = new Mock<INeuralNetwork>();
            var genes = CreateTestGenes();
            mockNetwork.Setup(n => n.GetGenes()).Returns(genes);

            // Act
            var result = evolutionChember.MutateNN(mockNetwork.Object, 100, true);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void MutateNN_AbsoluteMutation_ChangesNetworkGenes()
        {
            var evolutionChember = new EvolutionChember();
            var factory = NeuralNetworkFactory.GetInstance();
            var network = factory.Create(CreateTestGenesWithHiddenLayer());
            List<double> originalValues = FlattenGeneValues(network.GetGenes()).ToList();

            INeuralNetwork mutated = evolutionChember.MutateNN(network, 1, false);
            List<double> mutatedValues = FlattenGeneValues(mutated.GetGenes()).ToList();

            Assert.HasCount(originalValues.Count, mutatedValues);
            Assert.IsTrue(originalValues.Zip(mutatedValues, (left, right) => left != right).Any(changed => changed));
        }

        private NeuralNetworkGene CreateLargeTestGenes()
        {
            var genes = new NeuralNetworkGene();

            // Create input layer with more neurons
            genes.InputGene = new LayerGene();
            genes.InputGene.Neurons = new List<NeuronGene>();

            for (int i = 0; i < 10; i++)
            {
                var neuron = new NeuronGene();
                neuron.Soma = new SomaGene { Bias = 0.5 };
                neuron.Axon = new AxonGene();
                neuron.Axon.Weights = new List<double> { 0.1, 0.2, 0.3, 0.4, 0.5 };
                genes.InputGene.Neurons.Add(neuron);
            }

            // Create hidden layers (empty)
            genes.HiddenGenes = new List<LayerGene>();

            // Create output layer
            genes.OutputGene = new LayerGene();
            genes.OutputGene.Neurons = new List<NeuronGene>();

            for (int i = 0; i < 5; i++)
            {
                var neuron = new NeuronGene();
                neuron.Soma = new SomaGene { Bias = 0.3 };
                neuron.Axon = new AxonGene();
                neuron.Axon.Weights = new List<double>();
                genes.OutputGene.Neurons.Add(neuron);
            }

            return genes;
        }

        private static NeuralNetworkGene CreateLayerOwnershipGene()
        {
            return new NeuralNetworkGene
            {
                InputGene = new LayerGene
                {
                    Neurons = new List<NeuronGene>
                    {
                        new NeuronGene { Soma = new SomaGene(), Axon = new AxonGene { Weights = new List<double> { 0.1 } } }
                    }
                },
                HiddenGenes = new List<LayerGene>
                {
                    new LayerGene
                    {
                        Neurons = new List<NeuronGene>
                        {
                            new NeuronGene { Soma = new SomaGene { Bias = 0.2 }, Axon = new AxonGene { Weights = new List<double> { 0.3 } } }
                        }
                    },
                    new LayerGene
                    {
                        Neurons = new List<NeuronGene>
                        {
                            new NeuronGene { Soma = new SomaGene { Bias = 0.4 }, Axon = new AxonGene { Weights = new List<double> { 0.5 } } }
                        }
                    }
                },
                OutputGene = new LayerGene
                {
                    Neurons = new List<NeuronGene>
                    {
                        new NeuronGene { Soma = new SomaGene { Bias = 0.6 }, Axon = new AxonGene() }
                    }
                }
            };
        }

        private NeuralNetworkGene CreateTestGenesWithMultipleHiddenLayers()
        {
            var genes = CreateTestGenes();

            // Add first hidden layer
            var hiddenLayer1 = new LayerGene();
            hiddenLayer1.Neurons = new List<NeuronGene>();

            for (int i = 0; i < 3; i++)
            {
                var neuron = new NeuronGene();
                neuron.Soma = new SomaGene { Bias = 0.4 };
                neuron.Axon = new AxonGene();
                neuron.Axon.Weights = new List<double> { 0.5, 0.6 };
                hiddenLayer1.Neurons.Add(neuron);
            }

            genes.HiddenGenes.Add(hiddenLayer1);

            // Add second hidden layer
            var hiddenLayer2 = new LayerGene();
            hiddenLayer2.Neurons = new List<NeuronGene>();

            for (int i = 0; i < 2; i++)
            {
                var neuron = new NeuronGene();
                neuron.Soma = new SomaGene { Bias = 0.6 };
                neuron.Axon = new AxonGene();
                neuron.Axon.Weights = new List<double> { 0.7 };
                hiddenLayer2.Neurons.Add(neuron);
            }

            genes.HiddenGenes.Add(hiddenLayer2);

            return genes;
        }

        private NeuralNetworkGene CreateTestGenes()
        {
            var genes = new NeuralNetworkGene();

            // Create input layer
            genes.InputGene = new LayerGene();
            genes.InputGene.Neurons = new List<NeuronGene>();

            for (int i = 0; i < 2; i++)
            {
                var neuron = new NeuronGene();
                neuron.Soma = new SomaGene { Bias = 0.5 };
                neuron.Axon = new AxonGene();
                neuron.Axon.Weights = new List<double> { 0.1, 0.2, 0.3 };
                genes.InputGene.Neurons.Add(neuron);
            }

            // Create hidden layers (empty)
            genes.HiddenGenes = new List<LayerGene>();

            // Create output layer
            genes.OutputGene = new LayerGene();
            genes.OutputGene.Neurons = new List<NeuronGene>();

            for (int i = 0; i < 2; i++)
            {
                var neuron = new NeuronGene();
                neuron.Soma = new SomaGene { Bias = 0.3 };
                neuron.Axon = new AxonGene();
                neuron.Axon.Weights = new List<double>();
                genes.OutputGene.Neurons.Add(neuron);
            }

            return genes;
        }

        private NeuralNetworkGene CreateTestGenesWithHiddenLayer()
        {
            var genes = CreateTestGenes();

            // Add hidden layer
            var hiddenLayer = new LayerGene();
            hiddenLayer.Neurons = new List<NeuronGene>();

            for (int i = 0; i < 3; i++)
            {
                var neuron = new NeuronGene();
                neuron.Soma = new SomaGene { Bias = 0.4 };
                neuron.Axon = new AxonGene();
                neuron.Axon.Weights = new List<double> { 0.5, 0.6 };
                hiddenLayer.Neurons.Add(neuron);
            }

            genes.HiddenGenes.Add(hiddenLayer);

            return genes;
        }

        private static IEnumerable<double> FlattenGeneValues(NeuralNetworkGene gene)
        {
            foreach (double value in FlattenLayerValues(gene.InputGene))
                yield return value;

            foreach (LayerGene hiddenGene in gene.HiddenGenes)
                foreach (double value in FlattenLayerValues(hiddenGene))
                    yield return value;

            foreach (double value in FlattenLayerValues(gene.OutputGene))
                yield return value;
        }

        private static IEnumerable<double> FlattenLayerValues(LayerGene layer)
        {
            foreach (NeuronGene neuron in layer.Neurons)
            {
                yield return neuron.Soma.Bias;
                foreach (double weight in neuron.Axon.Weights)
                    yield return weight;
            }
        }
    }
}
