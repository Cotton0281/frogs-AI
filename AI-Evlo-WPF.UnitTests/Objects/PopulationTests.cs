using AI_Evlo_Test.Enumerators;
using AI_Evlo_Test.Objects;
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
            ArtificialNeuralNetwork.Genes.NeuralNetworkGene largeGene = factory.Create(25, 2, 5, 20).GetGenes();
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
        public void PopulationRegrowthPolicy_ShouldSpawn_WhenBelowLimitAndTimerDue()
        {
            var now = new DateTime(2026, 6, 2, 12, 0, 0);
            var population = new Population { SizeLimit = 5, NextRegrowAt = now.AddSeconds(-1) };
            population.Members = new List<ISmartObject>
            {
                new SmartObject(),
                new SmartObject(),
                new SmartObject(),
                new SmartObject()
            };

            Assert.IsTrue(PopulationRegrowthPolicy.ShouldSpawn(population, now));
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_ShouldSpawn_WhenBelowLimitAndNotScheduled_ReturnsFalse()
        {
            var now = new DateTime(2026, 6, 2, 12, 0, 0);
            var population = new Population { SizeLimit = 2 };
            population.Members = new List<ISmartObject> { new SmartObject() };

            Assert.IsFalse(PopulationRegrowthPolicy.ShouldSpawn(population, now));
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_ShouldSpawn_WhenAtLimit_ReturnsFalse()
        {
            var now = new DateTime(2026, 6, 2, 12, 0, 0);
            var population = new Population { SizeLimit = 2, NextRegrowAt = now.AddSeconds(-1) };
            population.Members = new List<ISmartObject> { new SmartObject(), new SmartObject() };

            Assert.IsFalse(PopulationRegrowthPolicy.ShouldSpawn(population, now));
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_MarkSpawned_SchedulesNextSpawnOneSecondLaterAndAdvancesMode()
        {
            var now = new DateTime(2026, 6, 2, 12, 0, 0);
            var population = new Population { RegrowModeIndex = 4 };

            PopulationRegrowthPolicy.MarkSpawned(population, now);

            Assert.AreEqual(now.AddSeconds(1), population.NextRegrowAt);
            Assert.AreEqual(0, population.RegrowModeIndex);
        }

        [TestMethod]
        public void PopulationRegrowthPolicy_SelectSource_RotatesThroughAvailableSources()
        {
            var factory = NeuralNetworkFactory.GetInstance();
            NeuralNetworkGene gene = factory.Create(25, 2, 1, 18).GetGenes();
            var liveTop = new SmartObject
            {
                ID = "live-top",
                Cycles = 50,
                NNetwork = factory.Create(25, 2, 1, 18)
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
    }
}
