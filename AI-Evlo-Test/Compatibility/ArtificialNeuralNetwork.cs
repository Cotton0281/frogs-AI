using System;
using System.Collections.Generic;
using System.Linq;
using ArtificialNeuralNetwork.ActivationFunctions;
using ArtificialNeuralNetwork.Genes;
using ArtificialNeuralNetwork.WeightInitializer;

namespace ArtificialNeuralNetwork
{
    public interface INeuralNetwork
    {
        ILayer InputLayer { get; set; }
        IList<ILayer> HiddenLayers { get; set; }
        ILayer OutputLayer { get; set; }
        IList<Synapse> Inputs { get; set; }
        IList<Synapse> Outputs { get; set; }
        double[] GetOutputs();
        void Process();
        void SetInputs(double[] inputs);
        NeuralNetworkGene GetGenes();
    }

    public interface ILayer
    {
        IList<INeuron> NeuronsInLayer { get; set; }
        void Process();
        LayerGene GetGenes();
    }

    public interface INeuron
    {
        ISoma Soma { get; set; }
        IAxon Axon { get; set; }
        void Process();
        NeuronGene GetGenes();
    }

    public interface IAxon
    {
        IList<Synapse> Terminals { get; set; }
        IActivationFunction ActivationFunction { get; set; }
        double Value { get; }
        void ProcessSignal(double signal);
        void SetValue(double value);
        AxonGene GetGenes();
    }

    public interface ISoma
    {
        IList<Synapse> Dendrites { get; set; }
        ISummationFunction SummationFunction { get; set; }
        double Bias { get; set; }
        double Value { get; }
        double CalculateSummation();
        SomaGene GetGenes();
    }

    public interface ISummationFunction
    {
        double CalculateSummation(IList<Synapse> dendrites, double bias);
    }

    public class Synapse
    {
        public virtual IAxon Axon { get; set; }
        public virtual double Weight { get; set; }
    }

    public class SimpleSummation : ISummationFunction
    {
        public double CalculateSummation(IList<Synapse> dendrites, double bias)
        {
            double sum = bias;
            if (dendrites == null)
                return sum;

            foreach (Synapse dendrite in dendrites)
                sum += (dendrite.Axon?.Value ?? 0) * dendrite.Weight;

            return sum;
        }
    }

    public class Axon : IAxon
    {
        public IList<Synapse> Terminals { get; set; } = new List<Synapse>();
        public IActivationFunction ActivationFunction { get; set; } = new TanhActivationFunction();
        public virtual double Value { get; private set; }

        public void ProcessSignal(double signal)
        {
            Value = ActivationFunction?.CalculateActivation(signal) ?? signal;
        }

        public void SetValue(double value)
        {
            Value = value;
        }

        public AxonGene GetGenes()
        {
            return new AxonGene
            {
                ActivationFunction = ActivationFunction?.GetType() ?? typeof(TanhActivationFunction),
                Weights = Terminals?.Select(t => t.Weight).ToList() ?? new List<double>()
            };
        }
    }

    public class Soma : ISoma
    {
        public IList<Synapse> Dendrites { get; set; } = new List<Synapse>();
        public ISummationFunction SummationFunction { get; set; } = new SimpleSummation();
        public double Bias { get; set; }
        public virtual double Value { get; private set; }

        public double CalculateSummation()
        {
            Value = SummationFunction?.CalculateSummation(Dendrites, Bias) ?? Bias;
            return Value;
        }

        public SomaGene GetGenes()
        {
            return new SomaGene
            {
                Bias = Bias,
                SummationFunction = SummationFunction?.GetType() ?? typeof(SimpleSummation)
            };
        }
    }

    public class Neuron : INeuron
    {
        public ISoma Soma { get; set; }
        public IAxon Axon { get; set; }

        public void Process()
        {
            Axon.ProcessSignal(Soma.CalculateSummation());
        }

        public NeuronGene GetGenes()
        {
            return new NeuronGene
            {
                Soma = Soma.GetGenes(),
                Axon = Axon.GetGenes()
            };
        }
    }

    public class Layer : ILayer
    {
        public IList<INeuron> NeuronsInLayer { get; set; } = new List<INeuron>();

        public void Process()
        {
            foreach (INeuron neuron in NeuronsInLayer)
                neuron.Process();
        }

        public LayerGene GetGenes()
        {
            return new LayerGene
            {
                Kind = NeuralLayerKind.Dense,
                Neurons = NeuronsInLayer.Select(n => n.GetGenes()).ToList()
            };
        }
    }

    /// <summary>
    /// A same-width residual layer. Its neurons evaluate the learned branch while the matching
    /// neuron in <see cref="SkipLayer"/> is added unchanged. With zero branch weights and biases,
    /// the layer is therefore an exact identity transform.
    /// </summary>
    public class ResidualLayer : ILayer
    {
        public IList<INeuron> NeuronsInLayer { get; set; } = new List<INeuron>();
        public ILayer SkipLayer { get; set; }

        public void Process()
        {
            if (SkipLayer?.NeuronsInLayer == null || SkipLayer.NeuronsInLayer.Count != NeuronsInLayer.Count)
                throw new InvalidOperationException("A residual layer must have the same width as its skip source.");

            for (int i = 0; i < NeuronsInLayer.Count; i++)
            {
                INeuron neuron = NeuronsInLayer[i];
                double branchInput = neuron.Soma.CalculateSummation();
                double branchValue = neuron.Axon.ActivationFunction?.CalculateActivation(branchInput) ?? branchInput;
                neuron.Axon.SetValue(SkipLayer.NeuronsInLayer[i].Axon.Value + branchValue);
            }
        }

        public LayerGene GetGenes()
        {
            return new LayerGene
            {
                Kind = NeuralLayerKind.Residual,
                Neurons = NeuronsInLayer.Select(n => n.GetGenes()).ToList()
            };
        }
    }

    public class NeuralNetwork : INeuralNetwork
    {
        public ILayer InputLayer { get; set; }
        public IList<ILayer> HiddenLayers { get; set; } = new List<ILayer>();
        public ILayer OutputLayer { get; set; }
        public IList<Synapse> Inputs { get; set; } = new List<Synapse>();
        public IList<Synapse> Outputs { get; set; } = new List<Synapse>();

        public double[] GetOutputs()
        {
            return OutputLayer?.NeuronsInLayer.Select(n => n.Axon.Value).ToArray()
                ?? Array.Empty<double>();
        }

        public void Process()
        {
            foreach (ILayer hiddenLayer in HiddenLayers)
                hiddenLayer.Process();

            OutputLayer?.Process();
        }

        public void SetInputs(double[] inputs)
        {
            if (inputs == null)
                inputs = Array.Empty<double>();

            for (int i = 0; i < Inputs.Count; i++)
                Inputs[i].Axon?.ProcessSignal(i < inputs.Length ? inputs[i] : 0);
        }

        public NeuralNetworkGene GetGenes()
        {
            return new NeuralNetworkGene
            {
                InputGene = InputLayer?.GetGenes() ?? new LayerGene(),
                HiddenGenes = HiddenLayers.Select(l => l.GetGenes()).ToList(),
                OutputGene = OutputLayer?.GetGenes() ?? new LayerGene()
            };
        }
    }
}

namespace ArtificialNeuralNetwork.ActivationFunctions
{
    public interface IActivationFunction
    {
        double CalculateActivation(double signal);
    }

    public class TanhActivationFunction : IActivationFunction
    {
        public double CalculateActivation(double signal) => Math.Tanh(signal);
    }

    public class IdentityActivationFunction : IActivationFunction
    {
        public double CalculateActivation(double signal) => signal;
    }

    public class SigmoidActivationFunction : IActivationFunction
    {
        public double CalculateActivation(double signal) => 1.0 / (1.0 + Math.Exp(-signal));
    }

    public class StepActivationFunction : IActivationFunction
    {
        public double CalculateActivation(double signal) => signal >= 0 ? 1 : 0;
    }

    public class RectifiedLinearActivationFunction : IActivationFunction
    {
        public double CalculateActivation(double signal) => Math.Max(0, signal);
    }

    public class LeakyRectifiedLinearActivationFunction : IActivationFunction
    {
        public double CalculateActivation(double signal) => signal >= 0 ? signal : signal * 0.01;
    }

    public class AbsoluteXActivationFunction : IActivationFunction
    {
        public double CalculateActivation(double signal) => Math.Abs(signal);
    }

    public class InverseActivationFunction : IActivationFunction
    {
        public double CalculateActivation(double signal) => -signal;
    }

    public class SechActivationFunction : IActivationFunction
    {
        public double CalculateActivation(double signal) => 1.0 / Math.Cosh(signal);
    }

    public class SinhActivationFunction : IActivationFunction
    {
        public double CalculateActivation(double signal) => Math.Sinh(signal);
    }
}

namespace ArtificialNeuralNetwork.SummationFunctions
{
    public class AverageSummation : ISummationFunction
    {
        public double CalculateSummation(IList<Synapse> dendrites, double bias)
        {
            if (dendrites == null || dendrites.Count == 0)
                return bias;

            return bias + dendrites.Average(d => (d.Axon?.Value ?? 0) * d.Weight);
        }
    }

    public class MaxSummation : ISummationFunction
    {
        public double CalculateSummation(IList<Synapse> dendrites, double bias)
        {
            if (dendrites == null || dendrites.Count == 0)
                return bias;

            return bias + dendrites.Max(d => (d.Axon?.Value ?? 0) * d.Weight);
        }
    }

    public class MinSummation : ISummationFunction
    {
        public double CalculateSummation(IList<Synapse> dendrites, double bias)
        {
            if (dendrites == null || dendrites.Count == 0)
                return bias;

            return bias + dendrites.Min(d => (d.Axon?.Value ?? 0) * d.Weight);
        }
    }
}

namespace ArtificialNeuralNetwork.WeightInitializer
{
    public interface IWeightInitializer
    {
        double InitializeWeight();
    }

    public class RandomWeightInitializer : IWeightInitializer
    {
        private readonly Random random;

        public RandomWeightInitializer(Random random)
        {
            this.random = random ?? new Random();
        }

        public double InitializeWeight()
        {
            return random.NextDouble() * 2 - 1;
        }
    }

    public class ConstantWeightInitializer : IWeightInitializer
    {
        private readonly double weight;

        public ConstantWeightInitializer(double weight)
        {
            this.weight = weight;
        }

        public double InitializeWeight() => weight;
    }
}

namespace ArtificialNeuralNetwork.Genes
{
    public enum NeuralLayerKind
    {
        Dense = 0,
        Residual = 1
    }

    public class NeuralNetworkGene
    {
        public LayerGene InputGene { get; set; } = new LayerGene();
        public IList<LayerGene> HiddenGenes { get; set; } = new List<LayerGene>();
        public LayerGene OutputGene { get; set; } = new LayerGene();
    }

    public class LayerGene
    {
        public NeuralLayerKind Kind { get; set; } = NeuralLayerKind.Dense;
        public IList<NeuronGene> Neurons { get; set; } = new List<NeuronGene>();
    }

    public class NeuronGene
    {
        public SomaGene Soma { get; set; } = new SomaGene();
        public AxonGene Axon { get; set; } = new AxonGene();
    }

    public class SomaGene
    {
        public double Bias { get; set; }
        public Type SummationFunction { get; set; } = typeof(SimpleSummation);
    }

    public class AxonGene
    {
        public Type ActivationFunction { get; set; } = typeof(TanhActivationFunction);
        public IList<double> Weights { get; set; } = new List<double>();
    }
}
