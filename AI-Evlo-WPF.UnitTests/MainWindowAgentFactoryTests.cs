using AI_Evlo_Test;
using AI_Evlo_Test.ConfigLib;
using AI_Evlo_Test.Enumerators;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork.Factories;
using System.Reflection;
using System.Windows;

namespace AI_Evlo_WPF.UnitTests;

[STATestClass]
public class MainWindowAgentFactoryTests
{
    [TestMethod]
    public void CreateFromAliveParent_InheritsParentsCurrentHp()
    {
        var window = new MainWindow();
        try
        {
            var population = CreatePopulation();
            var parent = new Frog(NeuralNetworkFactory.GetInstance().Create(2, 1, 1, 8))
            {
                ID = "parent",
                HP = 42
            };
            parent.SetLocation(25, 30);

            MethodInfo? create = typeof(MainWindow).GetMethod(
                "CreateFromAliveParent",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Population), typeof(ISmartObject), typeof(bool), typeof(bool) },
                modifiers: null);
            Assert.IsNotNull(create);

            var child = (ISmartObject)create.Invoke(window, new object[] { population, parent, false, false })!;

            Assert.AreEqual(parent.HP, child.HP);
            Assert.AreEqual(parent.Location.X + 1, child.Location.X);
            Assert.AreEqual(parent.Location.Y + 1, child.Location.Y);
        }
        finally
        {
            window.Close();
        }
    }

    [TestMethod]
    public void CreatePopulationMember_WithRaft_SpawnsCloseToRaft()
    {
        var window = new MainWindow();
        try
        {
            var raft = new TargetObj { Size = 200 };
            raft.SetLocation(100, 100);
            List<TargetObj> targets = GetTargets(window);
            targets.Clear();
            targets.Add(raft);

            MethodInfo? create = typeof(MainWindow).GetMethod(
                "CreatePopulationMember",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(create);

            var member = (ISmartObject)create.Invoke(window, new object[] { CreatePopulation() })!;

            double distance = Point.Subtract(member.Location, raft.Location).Length;
            Assert.IsLessThanOrEqualTo(150, distance);
        }
        finally
        {
            window.Close();
        }
    }

    [TestMethod]
    public void TryRegrowPopulation_ImmediateRandomSpawn_DoesNotMoveNewbornToLongestLivedAgent()
    {
        var window = new MainWindow();
        try
        {
            var raft = new TargetObj { Size = 200 };
            raft.SetLocation(800, 500);
            List<TargetObj> targets = GetTargets(window);
            targets.Clear();
            targets.Add(raft);

            Population population = CreatePopulation();
            population.SizeLimit = 2;
            population.SpawnDelay = false;
            population.RegrowModeIndex = 4;
            var survivor = new Frog(NeuralNetworkFactory.GetInstance().Create(2, 1, 1, 8))
            {
                ID = "corner-survivor",
                Cycles = 100
            };
            survivor.SetLocation(-300, -300);
            population.Members.Add(survivor);

            bool spawned = window.TryRegrowPopulation(population, currentCycle: 10);

            Assert.IsTrue(spawned);
            Assert.HasCount(2, population.Members);
            ISmartObject newborn = population.Members.Single(member => !ReferenceEquals(member, survivor));
            double distance = Point.Subtract(newborn.Location, raft.Location).Length;
            Assert.IsLessThanOrEqualTo(150, distance);
        }
        finally
        {
            window.Close();
        }
    }

    [TestMethod]
    public void TryRegrowPopulation_ParentlessModeClonesBestArchivedGeneWhenAvailable()
    {
        var window = new MainWindow();
        try
        {
            var raft = new TargetObj { Size = 200 };
            raft.SetLocation(500, 300);
            List<TargetObj> targets = GetTargets(window);
            targets.Clear();
            targets.Add(raft);

            Population population = CreatePopulation();
            population.SizeLimit = 1;
            population.SpawnDelay = false;
            population.RegrowModeIndex = 4;
            var factory = NeuralNetworkFactory.GetInstance();
            var weaker = factory.Create(2, 1, 1, 8).GetGenes();
            var best = factory.Create(2, 1, 1, 8).GetGenes();
            weaker.InputGene.Neurons[0].Axon.Weights[0] = 0.11;
            best.InputGene.Neurons[0].Axon.Weights[0] = 0.88;
            population.lsBestGenes = new List<GenomeRecord>
            {
                new GenomeRecord { ID = "weaker", Fitness = 5, Gene = weaker },
                new GenomeRecord { ID = "best", Fitness = 10, Gene = best }
            };

            bool spawned = window.TryRegrowPopulation(population, currentCycle: 10);

            Assert.IsTrue(spawned);
            Assert.HasCount(1, population.Members);
            Assert.AreEqual("best", population.Members[0].ParentId);
            Assert.AreEqual(
                0.88,
                population.Members[0].NNetwork.GetGenes().InputGene.Neurons[0].Axon.Weights[0],
                0.000001);
        }
        finally
        {
            window.Close();
        }
    }

    private static Population CreatePopulation()
    {
        return new Population
        {
            Name = "Frogs",
            Being = PopulationBeing.Frog,
            NeuroNetTemplate = new NeuroNetStructure(2, 1, 1, 8)
        };
    }

    private static List<TargetObj> GetTargets(MainWindow window)
    {
        FieldInfo? field = typeof(MainWindow).GetField(
            "Targets",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return (List<TargetObj>)field.GetValue(window)!;
    }
}
