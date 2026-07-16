using AI_Evlo_Test.ConfigLib;
using AI_Evlo_Test.Enumerators;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;

namespace AI_Evlo_WPF.UnitTests.Objects;

[STATestClass]
public class PopulationNeuralNetworkEvolutionTests
{
    [TestMethod]
    public void AddResidualLayer_GrowsEveryPopulationBrainAndPreservesOutputs()
    {
        var factory = NeuralNetworkFactory.GetInstance();
        INeuralNetwork liveNetwork = factory.Create(2, 1, 1, 2);
        INeuralNetwork goldenNetwork = factory.Create(Utils.CloneGene(liveNetwork.GetGenes()));
        NeuralNetworkGene archiveGene = Utils.CloneGene(liveNetwork.GetGenes());
        NeuralNetworkGene goldenGene = Utils.CloneGene(liveNetwork.GetGenes());
        NeuralNetworkGene initialGene = Utils.CloneGene(liveNetwork.GetGenes());
        var live = new SmartObject(liveNetwork);
        var golden = new SmartObject(goldenNetwork) { IsGoldenAgent = true };
        var population = new Population
        {
            NeuroNetTemplate = new NeuroNetStructure(2, 1, 1, 2),
            Members = new List<ISmartObject> { live },
            GoldenAgent = golden,
            GoldenAgentGene = goldenGene,
            GoldenInitialGene = initialGene,
            LayerLocks = new List<bool> { true, true },
            lsBestGenes = new List<GenomeRecord>
            {
                new GenomeRecord { ID = "archive", Gene = archiveGene, Fitness = 10 }
            }
        };
        double[] inputs = { 0.3, -0.8 };
        live.NNetwork.SetInputs(inputs);
        live.NNetwork.Process();
        double before = live.NNetwork.GetOutputs()[0];

        PopulationNetworkChangeResult result = PopulationNeuralNetworkEvolution.AddResidualLayer(population);

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.HasCount(2, live.NNetwork.HiddenLayers);
        Assert.AreEqual(NeuralLayerKind.Residual, live.NNetwork.GetGenes().HiddenGenes[1].Kind);
        Assert.HasCount(2, golden.NNetwork.HiddenLayers);
        Assert.HasCount(2, population.GoldenAgentGene.HiddenGenes);
        Assert.HasCount(2, population.GoldenInitialGene.HiddenGenes);
        Assert.HasCount(2, population.lsBestGenes[0].Gene.HiddenGenes);
        CollectionAssert.AreEqual(new[] { true, false, true }, population.LayerLocks);
        Assert.HasCount(2, population.NeuroNetTemplate.LayerDefinitions);
        Assert.AreEqual(NeuralLayerKind.Residual, population.NeuroNetTemplate.LayerDefinitions[1].Kind);

        live.NNetwork.SetInputs(inputs);
        live.NNetwork.Process();
        Assert.AreEqual(before, live.NNetwork.GetOutputs()[0], 1e-12);
    }

    [TestMethod]
    public void AddResidualLayer_WhenOneBrainHasDifferentTopology_DoesNotPartiallyChangePopulation()
    {
        var factory = NeuralNetworkFactory.GetInstance();
        var live = new SmartObject(factory.Create(2, 1, 1, 2));
        var population = new Population
        {
            NeuroNetTemplate = new NeuroNetStructure(2, 1, 1, 2),
            Members = new List<ISmartObject> { live },
            LayerLocks = new List<bool> { false, false },
            lsBestGenes = new List<GenomeRecord>
            {
                new GenomeRecord { ID = "incompatible", Gene = factory.Create(3, 1, 1, 2).GetGenes() }
            }
        };

        PopulationNetworkChangeResult result = PopulationNeuralNetworkEvolution.AddResidualLayer(population);

        Assert.IsFalse(result.Succeeded);
        Assert.HasCount(1, live.NNetwork.HiddenLayers);
        Assert.HasCount(1, population.lsBestGenes[0].Gene.HiddenGenes);
        CollectionAssert.AreEqual(new[] { false, false }, population.LayerLocks);
    }

    [TestMethod]
    public void AutoGrow_WhenSurvivalReachesMilestone_LocksOldLayersAndGrowsOnlyOnce()
    {
        var factory = NeuralNetworkFactory.GetInstance();
        var population = new Population
        {
            GoldenAgentEnabled = false,
            AutoGrowNeuralNetwork = true,
            NextAutoGrowSurvivalCycles = 100,
            NeuroNetTemplate = new NeuroNetStructure(2, 1, 1, 2),
            Members = new List<ISmartObject> { new SmartObject(factory.Create(2, 1, 1, 2)) },
            LayerLocks = new List<bool> { false, false }
        };

        bool grew = PopulationAutoGrowthPolicy.TryGrow(population, observedSurvivalCycles: 250, out PopulationNetworkChangeResult result);

        Assert.IsTrue(grew, result?.Message);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(250, population.SurvivalRecordCycles);
        Assert.AreEqual(400, population.NextAutoGrowSurvivalCycles);
        CollectionAssert.AreEqual(new[] { true, false, true }, population.LayerLocks);
        Assert.HasCount(2, population.Members[0].NNetwork.HiddenLayers);

        Assert.IsFalse(PopulationAutoGrowthPolicy.TryGrow(population, 250, out _));
        Assert.HasCount(2, population.Members[0].NNetwork.HiddenLayers);
    }

    [TestMethod]
    public void SetEnabled_SchedulesTheNextDoublingStrictlyAboveTheCurrentRecord()
    {
        int firstMilestone = PopulationRegrowthPolicy.NaturalSurvivalTicksFor(PopulationBeing.Frog) * 2;
        var population = new Population
        {
            Being = PopulationBeing.Frog,
            SurvivalRecordCycles = firstMilestone * 2
        };

        PopulationAutoGrowthPolicy.SetEnabled(population, true);

        Assert.IsTrue(population.AutoGrowNeuralNetwork);
        Assert.AreEqual(firstMilestone * 4, population.NextAutoGrowSurvivalCycles);
    }

    [TestMethod]
    public void BlankUnlockedLayers_ChangesEveryStoredBrainAndPreservesLockedDestinations()
    {
        Population population = CreateParameterEditingPopulation();
        NeuralNetworkGene before = Utils.CloneGene(population.Members[0].NNetwork.GetGenes());
        NeuralNetworkGene baselineBefore = Utils.CloneGene(population.GoldenInitialGene);
        population.lsBestGenes.Add(new GenomeRecord
        {
            ID = "older-archive",
            Gene = Utils.CloneGene(before),
            Fitness = 5
        });

        PopulationNetworkChangeResult result = PopulationNeuralNetworkEvolution.BlankUnlockedLayers(population);

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.HasCount(1, population.lsBestGenes);
        Assert.AreEqual("Parameters::ManualSeed", population.lsBestGenes[0].ID);
        foreach (NeuralNetworkGene after in GetPopulationGenes(population))
        {
            CollectionAssert.AreEqual(ParametersForDestination(before, 0), ParametersForDestination(after, 0));
            Assert.IsTrue(ParametersForDestination(after, 1).All(value => value == 0));
            CollectionAssert.AreEqual(ParametersForDestination(before, 2), ParametersForDestination(after, 2));
        }
        AssertSameParameters(population.GoldenAgentGene, population.GoldenAgent.NNetwork.GetGenes());
        AssertSameParameters(baselineBefore, population.GoldenInitialGene);
    }

    [TestMethod]
    public void BlankUnlockedLayers_WhenArchiveIsEmpty_CreatesZeroedArchivedSeed()
    {
        var factory = NeuralNetworkFactory.GetInstance();
        INeuralNetwork network = factory.Create(2, 1, 2, 2);
        var population = new Population
        {
            Name = "EmptyArchive",
            NeuroNetTemplate = NeuroNetStructure.FromGene(network.GetGenes()),
            Members = new List<ISmartObject> { new SmartObject(network) },
            LayerLocks = new List<bool> { false, false, false },
            lsBestGenes = new List<GenomeRecord>()
        };

        PopulationNetworkChangeResult result = PopulationNeuralNetworkEvolution.BlankUnlockedLayers(population);

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.HasCount(1, population.lsBestGenes);
        for (int destination = 0; destination <= population.lsBestGenes[0].Gene.HiddenGenes.Count; destination++)
            Assert.IsTrue(ParametersForDestination(population.lsBestGenes[0].Gene, destination).All(value => value == 0));
    }

    [TestMethod]
    public void RandomizeUnlockedLayers_ChangesOnlyUnlockedDestinationsAcrossPopulation()
    {
        Population population = CreateParameterEditingPopulation();
        NeuralNetworkGene before = Utils.CloneGene(population.Members[0].NNetwork.GetGenes());
        NeuralNetworkGene baselineBefore = Utils.CloneGene(population.GoldenInitialGene);
        population.lsBestGenes.Add(new GenomeRecord
        {
            ID = "older-archive",
            Gene = Utils.CloneGene(before),
            Fitness = 5
        });

        PopulationNetworkChangeResult result = PopulationNeuralNetworkEvolution.RandomizeUnlockedLayers(
            population,
            new Random(12345));

        Assert.IsTrue(result.Succeeded, result.Message);
        Assert.HasCount(1, population.lsBestGenes);
        Assert.AreEqual("Parameters::ManualSeed", population.lsBestGenes[0].ID);
        foreach (NeuralNetworkGene after in GetPopulationGenes(population))
        {
            CollectionAssert.AreEqual(ParametersForDestination(before, 0), ParametersForDestination(after, 0));
            List<double> randomized = ParametersForDestination(after, 1);
            Assert.IsTrue(randomized.All(value => value >= -1 && value <= 1));
            Assert.IsTrue(randomized.Where((value, index) => value != ParametersForDestination(before, 1)[index]).Any());
            CollectionAssert.AreEqual(ParametersForDestination(before, 2), ParametersForDestination(after, 2));
        }
        NeuralNetworkGene canonical = population.Members[0].NNetwork.GetGenes();
        foreach (NeuralNetworkGene gene in GetPopulationGenes(population))
            AssertSameParameters(canonical, gene);
        AssertSameParameters(population.GoldenAgentGene, population.GoldenAgent.NNetwork.GetGenes());
        AssertSameParameters(baselineBefore, population.GoldenInitialGene);
    }

    private static Population CreateParameterEditingPopulation()
    {
        var factory = NeuralNetworkFactory.GetInstance();
        INeuralNetwork network = factory.Create(2, 1, 2, 2);
        NeuralNetworkGene gene = network.GetGenes();
        var population = new Population
        {
            Name = "Parameters",
            NeuroNetTemplate = NeuroNetStructure.FromGene(gene),
            Members = new List<ISmartObject>
            {
                new SmartObject(network),
                new SmartObject(factory.Create(Utils.CloneGene(gene)))
            },
            GoldenAgentGene = Utils.CloneGene(gene),
            GoldenInitialGene = Utils.CloneGene(gene),
            LayerLocks = new List<bool> { true, false, true },
            lsBestGenes = new List<GenomeRecord>
            {
                new GenomeRecord { ID = "archive", Gene = Utils.CloneGene(gene) }
            }
        };
        population.GoldenAgent = new SmartObject(factory.Create(Utils.CloneGene(gene)))
        {
            IsGoldenAgent = true
        };
        return population;
    }

    private static IEnumerable<NeuralNetworkGene> GetPopulationGenes(Population population)
    {
        foreach (ISmartObject member in population.Members)
            yield return member.NNetwork.GetGenes();
        yield return population.GoldenAgent.NNetwork.GetGenes();
        yield return population.GoldenAgentGene;
        yield return population.lsBestGenes[0].Gene;
    }

    private static List<double> ParametersForDestination(NeuralNetworkGene gene, int destinationLayerIndex)
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

    private static void AssertSameParameters(NeuralNetworkGene expected, NeuralNetworkGene actual)
    {
        for (int destination = 0; destination <= expected.HiddenGenes.Count; destination++)
            CollectionAssert.AreEqual(
                ParametersForDestination(expected, destination),
                ParametersForDestination(actual, destination));
    }
}
