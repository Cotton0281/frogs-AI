using AI_Evlo_Test.Objects;
using AI_Evlo_Test.ConfigLib;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;

namespace AI_Evlo_WPF.UnitTests.Objects;

[STATestClass]
public class PopulationMutationTests
{
    [TestMethod]
    public void MutationRate_DefaultsToOneAndClampsToDesignerRange()
    {
        var population = new Population();

        Assert.AreEqual(1, population.MutationRate);
        population.MutationRate = 0;
        Assert.AreEqual(1, population.MutationRate);
        population.MutationRate = 21;
        Assert.AreEqual(20, population.MutationRate);
    }

    [TestMethod]
    public void MutateSpecialAgent_AfterAddingLayer_ChangesExactRateOnlyInNewUnlockedLayer()
    {
        INeuralNetwork network = NeuralNetworkFactory.GetInstance().Create(2, 1, 1, 8);
        var member = new SmartObject(network) { ID = "survivor", Cycles = 100 };
        var population = new Population
        {
            Name = "Frogs",
            NeuroNetTemplate = new NeuroNetStructure(2, 1, 1, 8),
            Members = new List<ISmartObject> { member },
            LayerLocks = new List<bool> { true, true },
            MutationRate = 20
        };

        PopulationNetworkChangeResult growth = PopulationNeuralNetworkEvolution.AddResidualLayer(population);
        Assert.IsTrue(growth.Succeeded, growth.Message);
        CollectionAssert.AreEqual(new[] { true, false, true }, population.LayerLocks);
        NeuralNetworkGene before = Utils.CloneGene(member.NNetwork.GetGenes());

        PopulationNetworkChangeResult mutation = PopulationNeuralNetworkEvolution.MutateSpecialAgent(
            population,
            SpecialAgentRole.LongestLivedAlive,
            new EvolutionChember(12345));

        Assert.IsTrue(mutation.Succeeded, mutation.Message);
        NeuralNetworkGene after = member.NNetwork.GetGenes();
        Assert.AreEqual(0, ChangedCount(before, after, 0), "Locked H1 must not change.");
        Assert.AreEqual(20, ChangedCount(before, after, 1), "The unlocked residual layer must receive the configured mutation count.");
        Assert.AreEqual(0, ChangedCount(before, after, 2), "Locked outputs must not change.");
    }

    private static int ChangedCount(
        NeuralNetworkGene before,
        NeuralNetworkGene after,
        int destinationLayerIndex)
    {
        List<double> beforeValues = ParametersForDestination(before, destinationLayerIndex);
        List<double> afterValues = ParametersForDestination(after, destinationLayerIndex);
        return beforeValues.Zip(afterValues, (left, right) => left != right).Count(changed => changed);
    }

    private static List<double> ParametersForDestination(
        NeuralNetworkGene gene,
        int destinationLayerIndex)
    {
        int hiddenCount = gene.HiddenGenes.Count;
        LayerGene source = destinationLayerIndex == 0
            ? gene.InputGene
            : gene.HiddenGenes[destinationLayerIndex - 1];
        LayerGene destination = destinationLayerIndex < hiddenCount
            ? gene.HiddenGenes[destinationLayerIndex]
            : gene.OutputGene;

        return source.Neurons.SelectMany(neuron => neuron.Axon.Weights)
            .Concat(destination.Neurons.Select(neuron => neuron.Soma.Bias))
            .ToList();
    }
}
