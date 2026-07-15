using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Genes;
using Moq;

namespace AI_Evlo_WPF.UnitTests.Objects;

[TestClass]
public class PopulationArchiveTests
{
    [TestMethod]
    public void Add_KeepsExactlyHalfOfEvenPopulationLimit()
    {
        var population = new Population { SizeLimit = 50 };

        for (int fitness = 0; fitness < 30; fitness++)
            PopulationArchive.Add(population, CreateMember(fitness));

        Assert.HasCount(25, population.lsBestGenes);
        Assert.AreEqual(29, population.lsBestGenes[0].Fitness);
        Assert.AreEqual(5, population.lsBestGenes[^1].Fitness);
    }

    [TestMethod]
    public void Add_AfterPopulationShrinks_TrimsExistingArchive()
    {
        var population = new Population { SizeLimit = 10 };
        for (int fitness = 0; fitness < 5; fitness++)
            PopulationArchive.Add(population, CreateMember(fitness));

        population.SizeLimit = 4;
        PopulationArchive.Add(population, CreateMember(-1));

        Assert.HasCount(2, population.lsBestGenes);
        Assert.AreEqual(4, population.lsBestGenes[0].Fitness);
        Assert.AreEqual(3, population.lsBestGenes[1].Fitness);
    }

    private static ISmartObject CreateMember(int fitness)
    {
        var network = new Mock<INeuralNetwork>();
        network.Setup(item => item.GetGenes()).Returns(new NeuralNetworkGene());
        return new SmartObject
        {
            ID = fitness.ToString(),
            Cycles = fitness,
            NNetwork = network.Object
        };
    }
}
