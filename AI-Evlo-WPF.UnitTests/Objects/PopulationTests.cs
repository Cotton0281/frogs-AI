using AI_Evlo_Test.Enumerators;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.ActivationFunctions;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AI_Evlo_WPF.UnitTests.Objects
{
    [STATestClass]
    public class PopulationTests
    {
        [TestMethod]
        public void Population_Add_AddsISmartObjectToMembers()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject = new Mock<ISmartObject>();

            // Act
            population.Add(mockSmartObject.Object);

            // Assert
            Assert.HasCount(1, population.Members);
            Assert.AreSame(mockSmartObject.Object, population.Members[0]);
        }

        [TestMethod]
        public void Population_Add_IncrementsTotalMembersCount()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject = new Mock<ISmartObject>();

            // Act
            population.Add(mockSmartObject.Object);

            // Assert
            Assert.AreEqual(1, population.TotalMembersCount);
        }

        [TestMethod]
        public void Population_Add_WhenVisibleShapeIsNull_DoesNotThrow()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject = new Mock<ISmartObject>();
            mockSmartObject.Setup(m => m.VisibleShape).Returns((System.Windows.FrameworkElement)null!);

            // Act
            population.Add(mockSmartObject.Object);

            // Assert
            Assert.HasCount(1, population.Members);
        }

        [TestMethod]
        public void Population_Add_WhenVisibleShapeIsShape_SetsShapeFillToPopulationColorBrush()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject = new Mock<ISmartObject>();
            var shape = new Rectangle();
            mockSmartObject.Setup(m => m.VisibleShape).Returns(shape);

            // Act
            population.Add(mockSmartObject.Object);

            // Assert
            Assert.AreSame(population.PopulationColorBrush, shape.Fill);
        }

        [TestMethod]
        public void Population_Add_WhenVisibleShapeIsNotShape_DoesNotSetFill()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject = new Mock<ISmartObject>();
            var mockFrameworkElement = new Mock<System.Windows.FrameworkElement>();
            mockSmartObject.Setup(m => m.VisibleShape).Returns(mockFrameworkElement.Object);

            // Act
            population.Add(mockSmartObject.Object);

            // Assert
            Assert.HasCount(1, population.Members);
        }

        [TestMethod]
        public void Population_Add_MultipleMembers_IncrementsCountCorrectly()
        {
            // Arrange
            var population = new Population();
            var mockSmartObject1 = new Mock<ISmartObject>();
            var mockSmartObject2 = new Mock<ISmartObject>();
            var mockSmartObject3 = new Mock<ISmartObject>();

            // Act
            population.Add(mockSmartObject1.Object);
            population.Add(mockSmartObject2.Object);
            population.Add(mockSmartObject3.Object);

            // Assert
            Assert.HasCount(3, population.Members);
            Assert.AreEqual(3, population.TotalMembersCount);
        }

        [TestMethod]
        public void Population_ToString_ReturnsName()
        {
            // Arrange
            var population = new Population
            {
                Name = "TestPopulation"
            };

            // Act
            var result = population.ToString();

            // Assert
            Assert.AreEqual("TestPopulation", result);
        }

        [TestMethod]
        public void Population_ToString_WhenNameIsDefault_ReturnsDefaultName()
        {
            // Arrange
            var population = new Population();

            // Act
            var result = population.ToString();

            // Assert
            Assert.AreEqual("PopulationX", result);
        }

        [TestMethod]
        public void Population_ToJson_ReturnsJsonString()
        {
            // Arrange
            var population = new Population
            {
                Name = "TestPopulation",
                TotalMembersCount = 5,
                SizeLimit = 10
            };

            // Act
            var result = population.ToJson();

            // Assert
            Assert.IsNotNull(result);
            Assert.Contains("\"Name\":\"TestPopulation\"", result);
        }

        [TestMethod]
        public void Population_ToJson_ContainsExpectedProperties()
        {
            // Arrange
            var population = new Population
            {
                Name = "TestPopulation",
                TotalMembersCount = 5
            };

            // Act
            var result = population.ToJson();

            // Assert
            Assert.Contains("Name", result);
            Assert.Contains("TotalMembersCount", result);
        }

        [TestMethod]
        public void Population_Add_WithMultipleShapes_SetsAllFillsCorrectly()
        {
            // Arrange
            var population = new Population();
            var customBrush = new SolidColorBrush(Colors.Red);
            population.PopulationColorBrush = customBrush;

            var mockSmartObject1 = new Mock<ISmartObject>();
            var shape1 = new Ellipse();
            mockSmartObject1.Setup(m => m.VisibleShape).Returns(shape1);

            var mockSmartObject2 = new Mock<ISmartObject>();
            var shape2 = new Rectangle();
            mockSmartObject2.Setup(m => m.VisibleShape).Returns(shape2);

            // Act
            population.Add(mockSmartObject1.Object);
            population.Add(mockSmartObject2.Object);

            // Assert
            Assert.AreSame(customBrush, shape1.Fill);
            Assert.AreSame(customBrush, shape2.Fill);
        }

        [TestMethod]
        public void Population_Being_DefaultsToFrog()
        {
            // Arrange
            var population = new Population();

            // Act
            var result = population.Being;

            // Assert
            Assert.AreEqual(PopulationBeing.Frog, result);
        }

        [TestMethod]
        public void Population_ToJson_ContainsBeing()
        {
            // Arrange
            var population = new Population
            {
                Name = "BirdPopulation",
                Being = PopulationBeing.Bird
            };

            // Act
            var result = population.ToJson();

            // Assert
            Assert.Contains("\"Being\":1", result);
        }

        [TestMethod]
        public void ResolveRestoredNeuroNetTemplate_WhenTemplateMissingAndBestGeneIsLarge_ReturnsLarge()
        {
            // Arrange
            var factory = ArtificialNeuralNetwork.Factories.NeuralNetworkFactory.GetInstance();
            ArtificialNeuralNetwork.Genes.NeuralNetworkGene largeGene = factory.Create(SmartObject.InputCount, SmartObject.OutputCount, 5, 20).GetGenes();
            var population = new Population
            {
                NeuroNetTemplate = null,
                lsBestGenes = new System.Collections.Generic.List<GenomeRecord>
                {
                    new GenomeRecord { Gene = largeGene, Fitness = 100 }
                }
            };

            // Act
            AI_Evlo_Test.ConfigLib.NeuroNetStructure template = AI_Evlo_Test.MainWindow.ResolveRestoredNeuroNetTemplate(population);

            // Assert
            Assert.AreEqual("Large", template.Id);
            Assert.AreEqual(5, template.HiddenLayers);
            Assert.AreEqual(20, template.NeuronsInHiddenLayer);
        }

        [TestMethod]
        public void ResolveRestoredNeuroNetTemplate_WhenTemplateAlreadyLarge_ReturnsLarge()
        {
            // Arrange
            var population = new Population
            {
                NeuroNetTemplate = AI_Evlo_Test.ConfigLib.NeuroNetStructure.Big_5Lx20N()
            };

            // Act
            AI_Evlo_Test.ConfigLib.NeuroNetStructure template = AI_Evlo_Test.MainWindow.ResolveRestoredNeuroNetTemplate(population);

            // Assert
            Assert.AreEqual("Large", template.Id);
            Assert.AreEqual(5, template.HiddenLayers);
            Assert.AreEqual(20, template.NeuronsInHiddenLayer);
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_ShouldSpawn_WhenBelowLimitAndCycleDue()
        {
            var population = new Population { SizeLimit = 5, NextRegrowCycle = 100 };
            population.Members = new List<ISmartObject>
            {
                new SmartObject(),
                new SmartObject(),
                new SmartObject(),
                new SmartObject()
            };

            Assert.IsTrue(PopulationRegrowthPolicy.ShouldSpawn(population, currentCycle: 100));
        }

        [TestMethod]
        public void Population_SpawnDelay_DefaultsToTrue()
        {
            var population = new Population();

            Assert.IsTrue(population.SpawnDelay);
        }

        [TestMethod]
        public void Population_PauseMutation_DefaultsToFalse()
        {
            var population = new Population();

            Assert.IsFalse(population.PauseMutation);
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_ShouldMutate_UsesPopulationPauseMutationSetting()
        {
            Assert.IsFalse(PopulationRegrowthPolicy.ShouldMutate(new Population(), mutationRequested: false));
            Assert.IsTrue(PopulationRegrowthPolicy.ShouldMutate(new Population(), mutationRequested: true));
            Assert.IsFalse(PopulationRegrowthPolicy.ShouldMutate(
                new Population { PauseMutation = true },
                mutationRequested: true));
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_ShouldSpawn_WhenSpawnDelayDisabledAndBelowLimit_ReturnsTrueEveryTick()
        {
            var population = new Population { SizeLimit = 5, SpawnDelay = false };
            population.Members = new List<ISmartObject>
            {
                new SmartObject(),
                new SmartObject()
            };

            Assert.IsTrue(PopulationRegrowthPolicy.ShouldSpawn(population, currentCycle: 1));
            Assert.IsTrue(PopulationRegrowthPolicy.ShouldSpawn(population, currentCycle: 2));
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_ShouldSpawn_WhenBelowLimitAndNotScheduled_ReturnsFalse()
        {
            var population = new Population { SizeLimit = 2 };
            population.Members = new List<ISmartObject> { new SmartObject() };

            Assert.IsFalse(PopulationRegrowthPolicy.ShouldSpawn(population, currentCycle: 100));
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_LongestLivedMember_ReturnsLiveMemberWithMostCycles()
        {
            var factory = NeuralNetworkFactory.GetInstance();
            var young = new SmartObject
            {
                ID = "young",
                Cycles = 10,
                NNetwork = factory.Create(SmartObject.InputCount, SmartObject.OutputCount, 1, 9)
            };
            var old = new SmartObject
            {
                ID = "old",
                Cycles = 30,
                NNetwork = factory.Create(SmartObject.InputCount, SmartObject.OutputCount, 1, 9)
            };
            var population = new Population
            {
                Members = new List<ISmartObject> { young, old, new SmartObject { Cycles = 100 } }
            };

            Assert.AreSame(old, PopulationRegrowthPolicy.LongestLivedMember(population));
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_ShouldSpawn_WhenAtLimit_ReturnsFalse()
        {
            var population = new Population { SizeLimit = 2, NextRegrowCycle = 100 };
            population.Members = new List<ISmartObject> { new SmartObject(), new SmartObject() };

            Assert.IsFalse(PopulationRegrowthPolicy.ShouldSpawn(population, currentCycle: 100));
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_MarkSpawned_SchedulesNextSpawnByNaturalSurvivalTicksAndAdvancesMode()
        {
            var frogPopulation = new Population { Being = PopulationBeing.Frog, RegrowModeIndex = 3 };
            var wrappingFrogPopulation = new Population { Being = PopulationBeing.Frog, RegrowModeIndex = 4 };
            var sharkPopulation = new Population { Being = PopulationBeing.Shark };
            var birdPopulation = new Population { Being = PopulationBeing.Bird };

            PopulationRegrowthPolicy.MarkSpawned(frogPopulation, currentCycle: 1000);
            PopulationRegrowthPolicy.MarkSpawned(wrappingFrogPopulation, currentCycle: 1000);
            PopulationRegrowthPolicy.MarkSpawned(sharkPopulation, currentCycle: 1000);
            PopulationRegrowthPolicy.MarkSpawned(birdPopulation, currentCycle: 1000);

            Assert.AreEqual(1000 + (int)Math.Ceiling(SmartObject.MaxHp / SmartObject.BaseHpDrain), frogPopulation.NextRegrowCycle);
            Assert.AreEqual(1000 + (int)Math.Ceiling(Shark.SharkMaxHp / Shark.SwimHpDrain), sharkPopulation.NextRegrowCycle);
            Assert.AreEqual(1000 + (int)Math.Ceiling(Bird.BirdMaxHp / Bird.FlightHpDrain), birdPopulation.NextRegrowCycle);
            Assert.AreEqual(4, frogPopulation.RegrowModeIndex);
            Assert.AreEqual(0, wrappingFrogPopulation.RegrowModeIndex);
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_MarkSpawned_WhenSpawnDelayDisabled_AdvancesModeWithoutScheduling()
        {
            var population = new Population { SpawnDelay = false, RegrowModeIndex = 4, NextRegrowCycle = 1000 };

            PopulationRegrowthPolicy.MarkSpawned(population, currentCycle: 1000);

            Assert.AreEqual(-1, population.NextRegrowCycle);
            Assert.AreEqual(0, population.RegrowModeIndex);
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_SelectSource_RotatesThroughAvailableSources()
        {
            var factory = NeuralNetworkFactory.GetInstance();
            NeuralNetworkGene gene = factory.Create(SmartObject.InputCount, SmartObject.OutputCount, 1, 18).GetGenes();
            var liveTop = new SmartObject
            {
                ID = "live-top",
                Cycles = 50,
                NNetwork = factory.Create(SmartObject.InputCount, SmartObject.OutputCount, 1, 18)
            };
            var population = new Population
            {
                Members = new List<ISmartObject> { liveTop },
                lsBestGenes = new List<GenomeRecord>
                {
                    new GenomeRecord { ID = "archive-top", Fitness = 100, Gene = gene, Generation = 4 }
                }
            };

            population.RegrowModeIndex = 0;
            Assert.AreEqual(RegrowthBrainSourceKind.ArchivedBestExact, PopulationRegrowthPolicy.SelectSource(population).Kind);
            population.RegrowModeIndex = 1;
            Assert.AreEqual(RegrowthBrainSourceKind.ArchivedBestMutated, PopulationRegrowthPolicy.SelectSource(population).Kind);
            population.RegrowModeIndex = 2;
            Assert.AreEqual(RegrowthBrainSourceKind.AliveBestExact, PopulationRegrowthPolicy.SelectSource(population).Kind);
            population.RegrowModeIndex = 3;
            Assert.AreEqual(RegrowthBrainSourceKind.AliveBestMutated, PopulationRegrowthPolicy.SelectSource(population).Kind);
            population.RegrowModeIndex = 4;
            Assert.AreEqual(RegrowthBrainSourceKind.Random, PopulationRegrowthPolicy.SelectSource(population).Kind);
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_SelectSource_WhenRequestedSourceMissing_FallsBackToRandom()
        {
            var population = new Population { Members = new List<ISmartObject>(), RegrowModeIndex = 2 };

            RegrowthBrainSource source = PopulationRegrowthPolicy.SelectSource(population);

            Assert.AreEqual(RegrowthBrainSourceKind.Random, source.Kind);
            Assert.IsNull(source.AliveParent);
            Assert.IsNull(source.ArchivedParent);
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_SelectSource_NeverUsesGoldenAgentAsParent()
        {
            var factory = NeuralNetworkFactory.GetInstance();
            var golden = new SmartObject
            {
                ID = "golden",
                NNetwork = factory.Create(SmartObject.InputCount, SmartObject.OutputCount, 1, 18)
            };
            var population = new Population
            {
                GoldenAgent = golden,
                Members = new List<ISmartObject>(),
                lsBestGenes = new List<GenomeRecord>()
            };

            for (int mode = 0; mode < 5; mode++)
            {
                population.RegrowModeIndex = mode;
                RegrowthBrainSource source = PopulationRegrowthPolicy.SelectSource(population);

                Assert.AreEqual(RegrowthBrainSourceKind.Random, source.Kind);
                Assert.AreNotSame(golden, source.AliveParent);
            }
        }

        [TestMethod]
        public void TryAverageGoldenBrain_FirstQualifiedSurvivorCopiesNetworkAndSetsCount()
        {
            NeuralNetworkGene survivorGene = CreateGoldenTestGene(0.2, 0.4, 0.6);
            var survivor = new SmartObject(NeuralNetworkFactory.GetInstance().Create(survivorGene))
            {
                Cycles = SmartObject.MaxHp * 4 + 1
            };
            var population = new Population();

            bool averaged = population.TryAverageGoldenBrain(survivor);

            Assert.IsTrue(averaged);
            Assert.AreEqual(1, population.GoldenAveragedNetworkCount);
            Assert.AreEqual(0.2, population.GoldenAgentGene.InputGene.Neurons[0].Axon.Weights[0], 0.000001);
            Assert.AreEqual(0.4, population.GoldenAgentGene.HiddenGenes[0].Neurons[0].Soma.Bias, 0.000001);
            Assert.AreEqual(0.6, population.GoldenAgentGene.HiddenGenes[0].Neurons[0].Axon.Weights[0], 0.000001);
        }

        [TestMethod]
        public void TryAverageGoldenBrain_SecondQualifiedSurvivorIncrementallyAveragesWeightsAndBiases()
        {
            var population = new Population
            {
                GoldenAgentGene = CreateGoldenTestGene(0.4, 0.6, 0.8),
                GoldenAveragedNetworkCount = 1
            };
            var survivor = new SmartObject(NeuralNetworkFactory.GetInstance().Create(CreateGoldenTestGene(0.2, 0.2, 0.4)))
            {
                Cycles = SmartObject.MaxHp * 4 + 1
            };

            bool averaged = population.TryAverageGoldenBrain(survivor);

            Assert.IsTrue(averaged);
            Assert.AreEqual(2, population.GoldenAveragedNetworkCount);
            Assert.AreEqual(0.3, population.GoldenAgentGene.InputGene.Neurons[0].Axon.Weights[0], 0.000001);
            Assert.AreEqual(0.4, population.GoldenAgentGene.HiddenGenes[0].Neurons[0].Soma.Bias, 0.000001);
            Assert.AreEqual(0.6, population.GoldenAgentGene.HiddenGenes[0].Neurons[0].Axon.Weights[0], 0.000001);
        }

        [TestMethod]
        public void TryAverageGoldenBrain_ThirdQualifiedSurvivorUsesExistingAverageCount()
        {
            var population = new Population
            {
                GoldenAgentGene = CreateGoldenTestGene(10, 20, 30),
                GoldenAveragedNetworkCount = 3
            };
            var survivor = new SmartObject(NeuralNetworkFactory.GetInstance().Create(CreateGoldenTestGene(18, 12, 10)))
            {
                Cycles = SmartObject.MaxHp * 4 + 1
            };

            bool averaged = population.TryAverageGoldenBrain(survivor);

            Assert.IsTrue(averaged);
            Assert.AreEqual(4, population.GoldenAveragedNetworkCount);
            Assert.AreEqual(12, population.GoldenAgentGene.InputGene.Neurons[0].Axon.Weights[0], 0.000001);
            Assert.AreEqual(18, population.GoldenAgentGene.HiddenGenes[0].Neurons[0].Soma.Bias, 0.000001);
            Assert.AreEqual(25, population.GoldenAgentGene.HiddenGenes[0].Neurons[0].Axon.Weights[0], 0.000001);
        }

        [TestMethod]
        public void TryAverageGoldenBrain_WhenAverageCountExceedsOneHundred_CapsBlendDenominator()
        {
            var population = new Population
            {
                GoldenAgentGene = CreateGoldenTestGene(10, 20, 30),
                GoldenAveragedNetworkCount = 150
            };
            var survivor = new SmartObject(NeuralNetworkFactory.GetInstance().Create(CreateGoldenTestGene(111, 121, 131)))
            {
                Cycles = SmartObject.MaxHp * 4 + 1
            };

            bool averaged = population.TryAverageGoldenBrain(survivor);

            Assert.IsTrue(averaged);
            Assert.AreEqual(151, population.GoldenAveragedNetworkCount);
            Assert.AreEqual(11, population.GoldenAgentGene.InputGene.Neurons[0].Axon.Weights[0], 0.000001);
            Assert.AreEqual(21, population.GoldenAgentGene.HiddenGenes[0].Neurons[0].Soma.Bias, 0.000001);
            Assert.AreEqual(31, population.GoldenAgentGene.HiddenGenes[0].Neurons[0].Axon.Weights[0], 0.000001);
        }

        [TestMethod]
        public void TryAverageGoldenBrain_WhenDisabled_DoesNotAverageQualifiedSurvivor()
        {
            var population = new Population { GoldenAgentEnabled = false };
            var survivor = new SmartObject(NeuralNetworkFactory.GetInstance().Create(CreateGoldenTestGene(1, 2, 3)))
            {
                Cycles = SmartObject.MaxHp * 4 + 1
            };

            bool averaged = population.TryAverageGoldenBrain(survivor);

            Assert.IsFalse(averaged);
            Assert.AreEqual(0, population.GoldenAveragedNetworkCount);
            Assert.IsNull(population.GoldenAgentGene);
        }

        [TestMethod]
        public void GoldenThreshold_WhenUnset_UsesPopulationMaxHpDividedByBaseHpDrain()
        {
            int originalMaxHp = SmartObject.MaxHp;
            try
            {
                SmartObject.MaxHp = 300;
                var sharkPopulation = new Population { Being = PopulationBeing.Shark };
                var birdPopulation = new Population { Being = PopulationBeing.Bird };
                var frogPopulation = new Population { Being = PopulationBeing.Frog };

                Assert.AreEqual(3750, sharkPopulation.GoldenThreshold, 0.000001);
                Assert.AreEqual(3333.333333, birdPopulation.GoldenThreshold, 0.000001);
                Assert.AreEqual(857.142857, frogPopulation.GoldenThreshold, 0.000001);
            }
            finally
            {
                SmartObject.MaxHp = originalMaxHp;
            }
        }

        [TestMethod]
        public void GoldenThreshold_WhenBeingChangesAfterFirstRead_RecomputesInitialThreshold()
        {
            var population = new Population { Being = PopulationBeing.Frog };

            _ = population.GoldenThreshold;
            population.Being = PopulationBeing.Shark;

            Assert.AreEqual(3750, population.GoldenThreshold, 0.000001);
        }

        [TestMethod]
        public void TryAverageGoldenBrain_WhenSurvivorIsBelowGoldenThreshold_DoesNotAverage()
        {
            var population = new Population { Being = PopulationBeing.Shark };
            var survivor = new SmartObject(NeuralNetworkFactory.GetInstance().Create(CreateGoldenTestGene(1, 2, 3)))
            {
                Cycles = (int)population.GoldenThreshold - 1
            };

            bool averaged = population.TryAverageGoldenBrain(survivor);

            Assert.IsFalse(averaged);
            Assert.AreEqual(0, population.GoldenAveragedNetworkCount);
            Assert.IsNull(population.GoldenAgentGene);
        }

        [TestMethod]
        public void ShouldCheckGoldenAverage_WhenSurvivorIsBelowGoldenThreshold_ReturnsFalse()
        {
            var population = new Population { Being = PopulationBeing.Shark };
            var survivor = new SmartObject(NeuralNetworkFactory.GetInstance().Create(CreateGoldenTestGene(1, 2, 3)))
            {
                Cycles = (int)population.GoldenThreshold - 1
            };

            bool shouldCheck = population.ShouldCheckGoldenAverage(survivor);

            Assert.IsFalse(shouldCheck);
            Assert.AreEqual(survivor.Cycles, population.GoldenRecordSurvivorCycles);
        }

        [TestMethod]
        public void ShouldCheckGoldenAverage_WhenSurvivorReachesNextGoldenMilestone_ReturnsTrue()
        {
            var population = new Population
            {
                GoldenThreshold = 1000
            };
            var survivor = new SmartObject(NeuralNetworkFactory.GetInstance().Create(CreateGoldenTestGene(1, 2, 3)))
            {
                Cycles = 1000
            };

            Assert.IsTrue(population.ShouldCheckGoldenAverage(survivor));
            Assert.IsTrue(population.TryAverageGoldenBrain(survivor));

            survivor.Cycles = 1099;
            Assert.IsFalse(population.ShouldCheckGoldenAverage(survivor));

            survivor.Cycles = 1100;
            Assert.IsTrue(population.ShouldCheckGoldenAverage(survivor));
        }

        [TestMethod]
        public void ShouldAttemptGoldenAverage_WhenQualifiedSurvivorHasZeroHp_ReturnsTrue()
        {
            var population = new Population
            {
                GoldenThreshold = 1000
            };
            var survivor = new SmartObject(NeuralNetworkFactory.GetInstance().Create(CreateGoldenTestGene(1, 2, 3)))
            {
                Cycles = 1000,
                HP = 0
            };

            bool shouldAttempt = AI_Evlo_Test.MainWindow.ShouldAttemptGoldenAverage(population, survivor);

            Assert.IsTrue(shouldAttempt);
        }

        [TestMethod]
        public void TryAverageGoldenBrain_WhenRecordSurvivorIsHigh_IncreasesGoldenThresholdToHalfRecord()
        {
            var population = new Population
            {
                Being = PopulationBeing.Shark
            };
            var survivor = new SmartObject(NeuralNetworkFactory.GetInstance().Create(CreateGoldenTestGene(1, 2, 3)))
            {
                Cycles = 20000
            };

            bool averaged = population.TryAverageGoldenBrain(survivor);

            Assert.IsTrue(averaged);
            Assert.AreEqual(20000, population.GoldenRecordSurvivorCycles);
            Assert.AreEqual(10000, population.GoldenThreshold, 0.000001);
        }

        [TestMethod]
        public void TryAverageGoldenBrain_SameSurvivorAveragesAgainAtTenPercentIntervals()
        {
            var population = new Population
            {
                GoldenThreshold = 1000
            };
            var survivor = new SmartObject(NeuralNetworkFactory.GetInstance().Create(CreateGoldenTestGene(1, 2, 3)))
            {
                Cycles = 1000
            };

            Assert.IsTrue(population.TryAverageGoldenBrain(survivor));
            Assert.AreEqual(1, population.GoldenAveragedNetworkCount);
            Assert.AreEqual(1100, survivor.NextGoldenAverageCycle);

            survivor.Cycles = 1099;
            Assert.IsFalse(population.TryAverageGoldenBrain(survivor));
            Assert.AreEqual(1, population.GoldenAveragedNetworkCount);

            survivor.Cycles = 1100;
            Assert.IsTrue(population.TryAverageGoldenBrain(survivor));
            Assert.AreEqual(2, population.GoldenAveragedNetworkCount);
            Assert.AreEqual(1200, survivor.NextGoldenAverageCycle);

            survivor.Cycles = 1200;
            Assert.IsTrue(population.TryAverageGoldenBrain(survivor));
            Assert.AreEqual(3, population.GoldenAveragedNetworkCount);
            Assert.AreEqual(1300, survivor.NextGoldenAverageCycle);
        }

        [TestMethod]
        public void TryAverageGoldenBrain_WhenSurvivorIsGoldenAgent_DoesNotAverageItself()
        {
            var golden = new SmartObject(NeuralNetworkFactory.GetInstance().Create(CreateGoldenTestGene(1, 2, 3)))
            {
                Cycles = 50000,
                IsGoldenAgent = true
            };
            var population = new Population
            {
                GoldenAgent = golden
            };

            bool averaged = population.TryAverageGoldenBrain(golden);

            Assert.IsFalse(averaged);
            Assert.AreEqual(0, population.GoldenAveragedNetworkCount);
            Assert.IsNull(population.GoldenAgentGene);
        }

        [TestMethod]
        public void TryAverageGoldenBrain_WhenTopologyDiffers_DoesNotChangeExistingGoldenAverage()
        {
            var population = new Population
            {
                GoldenAgentGene = CreateGoldenTestGene(0.4, 0.6, 0.8),
                GoldenAveragedNetworkCount = 1
            };
            INeuralNetwork differentTopology = NeuralNetworkFactory.GetInstance().Create(25, 2, 3, 13);
            var survivor = new SmartObject(differentTopology)
            {
                Cycles = SmartObject.MaxHp * 4 + 1
            };

            bool averaged = population.TryAverageGoldenBrain(survivor);

            Assert.IsFalse(averaged);
            Assert.AreEqual(1, population.GoldenAveragedNetworkCount);
            Assert.AreEqual(0.4, population.GoldenAgentGene.InputGene.Neurons[0].Axon.Weights[0], 0.000001);
        }

        private static NeuralNetworkGene CreateGoldenTestGene(double inputToHiddenWeight, double hiddenBias, double hiddenToOutputWeight)
        {
            return new NeuralNetworkGene
            {
                InputGene = new LayerGene
                {
                    Neurons = new List<NeuronGene>
                    {
                        CreateNeuronGene(0, inputToHiddenWeight)
                    }
                },
                HiddenGenes = new List<LayerGene>
                {
                    new LayerGene
                    {
                        Neurons = new List<NeuronGene>
                        {
                            CreateNeuronGene(hiddenBias, hiddenToOutputWeight)
                        }
                    }
                },
                OutputGene = new LayerGene
                {
                    Neurons = new List<NeuronGene>
                    {
                        CreateNeuronGene(0)
                    }
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
