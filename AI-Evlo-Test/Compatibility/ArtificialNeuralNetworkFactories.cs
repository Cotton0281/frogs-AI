using System;
using System.Collections.Generic;
using System.Linq;
using ArtificialNeuralNetwork.ActivationFunctions;
using ArtificialNeuralNetwork.Genes;
using ArtificialNeuralNetwork.WeightInitializer;

namespace ArtificialNeuralNetwork.Factories
{
    public interface ISomaFactory
    {
        ISoma Create(IList<Synapse> dendrites, double bias);
        ISoma Create(IList<Synapse> dendrites, double bias, Type summationFunction);
    }

    public interface IAxonFactory
    {
        IAxon Create(IList<Synapse> terminals);
        IAxon Create();
        IAxon Create(IList<Synapse> terminals, Type activationFunction);
    }

    public interface ISynapseFactory
    {
        Synapse Create();
        Synapse Create(double weight);
    }

    public interface INeuronFactory
    {
        INeuron Create(ISoma soma, IAxon axon);
    }

    public interface INeuralNetworkFactory
    {
        INeuralNetwork Create(int inputs, int outputs, int hiddenLayers, int neuronsInHiddenLayer);
        INeuralNetwork Create(int inputs, int outputs, IList<int> hiddenLayerSizes);
        INeuralNetwork Create(
            int inputs,
            int outputs,
            IList<int> hiddenLayerSizes,
            IList<NeuralLayerKind> hiddenLayerKinds);
        INeuralNetwork Create(NeuralNetworkGene gene);
    }

    public class SomaFactory : ISomaFactory
    {
        private readonly ISummationFunction summationFunction;

        private SomaFactory(ISummationFunction summationFunction)
        {
            this.summationFunction = summationFunction ?? new SimpleSummation();
        }

        public static ISomaFactory GetInstance(ISummationFunction summationFunction)
        {
            return new SomaFactory(summationFunction);
        }

        public ISoma Create(IList<Synapse> dendrites, double bias)
        {
            return new Soma
            {
                Dendrites = dendrites ?? new List<Synapse>(),
                Bias = bias,
                SummationFunction = summationFunction
            };
        }

        public ISoma Create(IList<Synapse> dendrites, double bias, Type summationFunction)
        {
            return new Soma
            {
                Dendrites = dendrites ?? new List<Synapse>(),
                Bias = bias,
                SummationFunction = CreateSummationFunction(summationFunction)
            };
        }

        internal static ISummationFunction CreateSummationFunction(Type type)
        {
            if (type == null || !typeof(ISummationFunction).IsAssignableFrom(type))
                return new SimpleSummation();

            return (ISummationFunction)Activator.CreateInstance(type);
        }
    }

    public class AxonFactory : IAxonFactory
    {
        private readonly IActivationFunction activationFunction;

        private AxonFactory(IActivationFunction activationFunction)
        {
            this.activationFunction = activationFunction ?? new TanhActivationFunction();
        }

        public static IAxonFactory GetInstance(IActivationFunction activationFunction)
        {
            return new AxonFactory(activationFunction);
        }

        public IAxon Create()
        {
            return Create(new List<Synapse>());
        }

        public IAxon Create(IList<Synapse> terminals)
        {
            return new Axon
            {
                Terminals = terminals ?? new List<Synapse>(),
                ActivationFunction = activationFunction
            };
        }

        public IAxon Create(IList<Synapse> terminals, Type activationFunction)
        {
            return new Axon
            {
                Terminals = terminals ?? new List<Synapse>(),
                ActivationFunction = CreateActivationFunction(activationFunction)
            };
        }

        internal static IActivationFunction CreateActivationFunction(Type type)
        {
            if (type == null || !typeof(IActivationFunction).IsAssignableFrom(type))
                return new TanhActivationFunction();

            return (IActivationFunction)Activator.CreateInstance(type);
        }
    }

    public class SynapseFactory : ISynapseFactory
    {
        private readonly IWeightInitializer weightInitializer;
        private readonly IAxonFactory axonFactory;

        private SynapseFactory(IWeightInitializer weightInitializer, IAxonFactory axonFactory)
        {
            this.weightInitializer = weightInitializer ?? new ConstantWeightInitializer(0);
            this.axonFactory = axonFactory ?? AxonFactory.GetInstance(new TanhActivationFunction());
        }

        public static ISynapseFactory GetInstance(IWeightInitializer weightInitializer, IAxonFactory axonFactory)
        {
            return new SynapseFactory(weightInitializer, axonFactory);
        }

        public Synapse Create()
        {
            return Create(weightInitializer.InitializeWeight());
        }

        public Synapse Create(double weight)
        {
            return new Synapse
            {
                Axon = axonFactory.Create(),
                Weight = weight
            };
        }
    }

    public class NeuronFactory : INeuronFactory
    {
        public static INeuronFactory GetInstance()
        {
            return new NeuronFactory();
        }

        public INeuron Create(ISoma soma, IAxon axon)
        {
            return new Neuron
            {
                Soma = soma,
                Axon = axon
            };
        }
    }

    public class NeuralNetworkFactory : INeuralNetworkFactory
    {
        private readonly ISomaFactory somaFactory;
        private readonly IAxonFactory axonFactory;
        private readonly ISynapseFactory hiddenSynapseFactory;
        private readonly ISynapseFactory ioSynapseFactory;
        private readonly IWeightInitializer weightInitializer;
        private readonly INeuronFactory neuronFactory;

        private NeuralNetworkFactory(
            ISomaFactory somaFactory,
            IAxonFactory axonFactory,
            ISynapseFactory hiddenSynapseFactory,
            ISynapseFactory ioSynapseFactory,
            IWeightInitializer weightInitializer,
            INeuronFactory neuronFactory)
        {
            this.somaFactory = somaFactory;
            this.axonFactory = axonFactory;
            this.hiddenSynapseFactory = hiddenSynapseFactory;
            this.ioSynapseFactory = ioSynapseFactory;
            this.weightInitializer = weightInitializer;
            this.neuronFactory = neuronFactory;
        }

        public static NeuralNetworkFactory GetInstance(
            ISomaFactory somaFactory,
            IAxonFactory axonFactory,
            ISynapseFactory hiddenSynapseFactory,
            ISynapseFactory ioSynapseFactory,
            IWeightInitializer weightInitializer,
            INeuronFactory neuronFactory)
        {
            return new NeuralNetworkFactory(somaFactory, axonFactory, hiddenSynapseFactory, ioSynapseFactory, weightInitializer, neuronFactory);
        }

        public static NeuralNetworkFactory GetInstance()
        {
            var random = new RandomWeightInitializer(new Random());
            var axonFactory = AxonFactory.GetInstance(new TanhActivationFunction());

            return new NeuralNetworkFactory(
                SomaFactory.GetInstance(new SimpleSummation()),
                axonFactory,
                SynapseFactory.GetInstance(random, axonFactory),
                SynapseFactory.GetInstance(new ConstantWeightInitializer(1.0), axonFactory),
                random,
                NeuronFactory.GetInstance());
        }

        public INeuralNetwork Create(int inputs, int outputs, int hiddenLayers, int neuronsInHiddenLayer)
        {
            var hiddenLayerSizes = new List<int>();
            for (int i = 0; i < hiddenLayers; i++)
                hiddenLayerSizes.Add(neuronsInHiddenLayer);

            return Create(inputs, outputs, hiddenLayerSizes);
        }

        public INeuralNetwork Create(int inputs, int outputs, IList<int> hiddenLayerSizes)
        {
            IList<int> sizes = hiddenLayerSizes ?? new List<int>();
            return Create(
                inputs,
                outputs,
                sizes,
                Enumerable.Repeat(NeuralLayerKind.Dense, sizes.Count).ToList());
        }

        public INeuralNetwork Create(
            int inputs,
            int outputs,
            IList<int> hiddenLayerSizes,
            IList<NeuralLayerKind> hiddenLayerKinds)
        {
            IList<int> sizes = hiddenLayerSizes ?? new List<int>();
            IList<NeuralLayerKind> kinds = hiddenLayerKinds ?? new List<NeuralLayerKind>();
            if (sizes.Count != kinds.Count)
                throw new ArgumentException("Hidden layer sizes and kinds must have the same count.");

            var gene = CreateGene(inputs, outputs, sizes, kinds);
            return Create(gene);
        }

        public INeuralNetwork Create(NeuralNetworkGene gene)
        {
            if (gene == null)
                throw new ArgumentNullException(nameof(gene));

            return BuildNetwork(gene);
        }

        private NeuralNetworkGene CreateGene(
            int inputs,
            int outputs,
            IList<int> hiddenLayerSizes,
            IList<NeuralLayerKind> hiddenLayerKinds)
        {
            var gene = new NeuralNetworkGene
            {
                InputGene = new LayerGene(),
                HiddenGenes = new List<LayerGene>(),
                OutputGene = new LayerGene()
            };

            int nextLayerSize = hiddenLayerSizes.Count > 0 ? hiddenLayerSizes[0] : outputs;
            bool nextLayerIsResidual = hiddenLayerKinds.Count > 0 && hiddenLayerKinds[0] == NeuralLayerKind.Residual;
            for (int i = 0; i < inputs; i++)
                gene.InputGene.Neurons.Add(CreateNeuronGene(nextLayerSize, true, nextLayerIsResidual, false));

            for (int layerIndex = 0; layerIndex < hiddenLayerSizes.Count; layerIndex++)
            {
                int layerSize = hiddenLayerSizes[layerIndex];
                nextLayerSize = layerIndex + 1 < hiddenLayerSizes.Count ? hiddenLayerSizes[layerIndex + 1] : outputs;
                nextLayerIsResidual = layerIndex + 1 < hiddenLayerKinds.Count
                    && hiddenLayerKinds[layerIndex + 1] == NeuralLayerKind.Residual;
                var layer = new LayerGene { Kind = hiddenLayerKinds[layerIndex] };
                for (int neuronIndex = 0; neuronIndex < layerSize; neuronIndex++)
                    layer.Neurons.Add(CreateNeuronGene(
                        nextLayerSize,
                        false,
                        nextLayerIsResidual,
                        layer.Kind == NeuralLayerKind.Residual));
                gene.HiddenGenes.Add(layer);
            }

            for (int i = 0; i < outputs; i++)
                gene.OutputGene.Neurons.Add(CreateNeuronGene(0, false, false, false));

            return gene;
        }

        private NeuronGene CreateNeuronGene(
            int outgoingWeights,
            bool inputLayer,
            bool zeroOutgoingWeights,
            bool zeroBias)
        {
            var gene = new NeuronGene
            {
                Soma = new SomaGene
                {
                    Bias = inputLayer || zeroBias ? 0 : weightInitializer.InitializeWeight(),
                    SummationFunction = typeof(SimpleSummation)
                },
                Axon = new AxonGene
                {
                    ActivationFunction = typeof(TanhActivationFunction),
                    Weights = new List<double>()
                }
            };

            for (int i = 0; i < outgoingWeights; i++)
                gene.Axon.Weights.Add(zeroOutgoingWeights ? 0 : weightInitializer.InitializeWeight());

            return gene;
        }

        private NeuralNetwork BuildNetwork(NeuralNetworkGene gene)
        {
            ILayer inputLayer = BuildLayer(gene.InputGene);
            var hiddenLayers = new List<ILayer>();
            foreach (LayerGene hiddenGene in gene.HiddenGenes)
                hiddenLayers.Add(BuildLayer(hiddenGene));
            ILayer outputLayer = BuildLayer(gene.OutputGene);

            var allLayers = new List<ILayer> { inputLayer };
            allLayers.AddRange(hiddenLayers);
            allLayers.Add(outputLayer);

            for (int layerIndex = 0; layerIndex < allLayers.Count - 1; layerIndex++)
                ConnectLayers(allLayers[layerIndex], allLayers[layerIndex + 1]);

            for (int layerIndex = 1; layerIndex < allLayers.Count - 1; layerIndex++)
            {
                if (allLayers[layerIndex] is ResidualLayer residual)
                {
                    if (allLayers[layerIndex - 1].NeuronsInLayer.Count != residual.NeuronsInLayer.Count)
                        throw new ArgumentException("A residual hidden layer must match the preceding layer width.", nameof(gene));

                    residual.SkipLayer = allLayers[layerIndex - 1];
                }
            }

            return new NeuralNetwork
            {
                InputLayer = inputLayer,
                HiddenLayers = hiddenLayers,
                OutputLayer = outputLayer,
                Inputs = CreateInputSynapses(inputLayer),
                Outputs = CreateOutputSynapses(outputLayer)
            };
        }

        private ILayer BuildLayer(LayerGene layerGene)
        {
            ILayer layer = layerGene.Kind == NeuralLayerKind.Residual
                ? new ResidualLayer()
                : new Layer();
            foreach (NeuronGene neuronGene in layerGene.Neurons)
            {
                var soma = new Soma
                {
                    Bias = neuronGene.Soma?.Bias ?? 0,
                    SummationFunction = SomaFactory.CreateSummationFunction(neuronGene.Soma?.SummationFunction)
                };
                var axon = new Axon
                {
                    ActivationFunction = AxonFactory.CreateActivationFunction(neuronGene.Axon?.ActivationFunction),
                    Terminals = CreateWeightPlaceholders(neuronGene.Axon?.Weights)
                };

                layer.NeuronsInLayer.Add(neuronFactory.Create(soma, axon));
            }

            return layer;
        }

        private static void ConnectLayers(ILayer sourceLayer, ILayer targetLayer)
        {
            for (int sourceIndex = 0; sourceIndex < sourceLayer.NeuronsInLayer.Count; sourceIndex++)
            {
                INeuron sourceNeuron = sourceLayer.NeuronsInLayer[sourceIndex];
                List<double> outgoingWeights = sourceNeuron.Axon.Terminals.Select(t => t.Weight).ToList();
                sourceNeuron.Axon.Terminals.Clear();

                for (int targetIndex = 0; targetIndex < targetLayer.NeuronsInLayer.Count; targetIndex++)
                {
                    INeuron targetNeuron = targetLayer.NeuronsInLayer[targetIndex];
                    double weight = targetIndex < outgoingWeights.Count
                        ? outgoingWeights[targetIndex]
                        : 0;

                    var synapse = new Synapse
                    {
                        Axon = sourceNeuron.Axon,
                        Weight = weight
                    };
                    sourceNeuron.Axon.Terminals.Add(synapse);
                    targetNeuron.Soma.Dendrites.Add(synapse);
                }
            }
        }

        private static IList<Synapse> CreateWeightPlaceholders(IList<double> weights)
        {
            var placeholders = new List<Synapse>();
            if (weights == null)
                return placeholders;

            foreach (double weight in weights)
                placeholders.Add(new Synapse { Weight = weight });

            return placeholders;
        }

        private static IList<Synapse> CreateInputSynapses(ILayer inputLayer)
        {
            var inputs = new List<Synapse>();
            foreach (INeuron neuron in inputLayer.NeuronsInLayer)
            {
                var synapse = new Synapse
                {
                    Axon = neuron.Axon,
                    Weight = 1
                };
                inputs.Add(synapse);
            }
            return inputs;
        }

        private static IList<Synapse> CreateOutputSynapses(ILayer outputLayer)
        {
            var outputs = new List<Synapse>();
            foreach (INeuron neuron in outputLayer.NeuronsInLayer)
            {
                outputs.Add(new Synapse
                {
                    Axon = neuron.Axon,
                    Weight = 1
                });
            }
            return outputs;
        }
    }
}
