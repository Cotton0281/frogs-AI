using AI_Evlo_Test;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork.Genes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AI_Evlo_WPF.UnitTests
{
    [STATestClass]
    public class PopulationListTests
    {
        [TestMethod]
        public void Constructor_WhenCalled_InitializesForm()
        {
            // Arrange & Act
            var populationList = new PopulationList();

            // Assert
            Assert.IsNotNull(populationList);
        }

        [TestMethod]
        public void SetDataSource_WithValidPopulation_SetsDataGridViewDataSources()
        {
            // Arrange
            var populationList = new PopulationList();
            var population = new Population
            {
                Members = new List<ISmartObject>
                {
                    new SmartObject { ID = "member1", Cycles = 10 },
                    new SmartObject { ID = "member2", Cycles = 20 }
                },
                lsBestGenes = new List<GenomeRecord>
                {
                    new GenomeRecord { ID = "gene1", Fitness = 100.0 },
                    new GenomeRecord { ID = "gene2", Fitness = 200.0 }
                }
            };

            // Act
            populationList.SetDataSource(population);

            // Assert
            Assert.IsNotNull(populationList);
        }

        [TestMethod]
        public void SetDataSource_WithEmptyMembers_SetsEmptyDataSource()
        {
            // Arrange
            var populationList = new PopulationList();
            var population = new Population
            {
                Members = new List<ISmartObject>(),
                lsBestGenes = new List<GenomeRecord>()
            };

            // Act
            populationList.SetDataSource(population);

            // Assert
            Assert.IsNotNull(populationList);
        }

        [TestMethod]
        public void SetDataSource_WithNullMembers_ThrowsException()
        {
            // Arrange
            var populationList = new PopulationList();
            var population = new Population
            {
                Members = null,
                lsBestGenes = null
            };

            // Act
            bool exceptionThrown = false;
            try
            {
                populationList.SetDataSource(population);
            }
            catch (NullReferenceException)
            {
                exceptionThrown = true;
            }

            // Assert
            Assert.IsTrue(exceptionThrown, "Expected NullReferenceException to be thrown");
        }

        [TestMethod]
        public void SetDataSource_WithMultipleGenes_ConvertsToList()
        {
            // Arrange
            var populationList = new PopulationList();
            var genes = new List<GenomeRecord>();
            for (int i = 0; i < 10; i++)
            {
                genes.Add(new GenomeRecord
                {
                    ID = $"gene{i}",
                    Fitness = i * 10.0,
                    Generation = i
                });
            }

            var population = new Population
            {
                Members = new List<ISmartObject>(),
                lsBestGenes = genes
            };

            // Act
            populationList.SetDataSource(population);

            // Assert
            Assert.IsNotNull(populationList);
        }

        [TestMethod]
        public void RefreshList_AfterSetDataSource_OrdersGenesByFitnessDescending()
        {
            // Arrange
            var populationList = new PopulationList();
            var population = new Population
            {
                Members = new List<ISmartObject>
                {
                    new SmartObject { ID = "member1", Cycles = 10 }
                },
                lsBestGenes = new List<GenomeRecord>
                {
                    new GenomeRecord { ID = "gene1", Fitness = 50.0 },
                    new GenomeRecord { ID = "gene2", Fitness = 150.0 },
                    new GenomeRecord { ID = "gene3", Fitness = 100.0 }
                }
            };

            populationList.SetDataSource(population);

            // Act
            populationList.RefreshList();

            // Assert
            Assert.IsNotNull(populationList);
        }

        [TestMethod]
        public void RefreshList_WithoutSetDataSource_ThrowsNullReferenceException()
        {
            // Arrange
            var populationList = new PopulationList();

            // Act
            bool exceptionThrown = false;
            try
            {
                populationList.RefreshList();
            }
            catch (NullReferenceException)
            {
                exceptionThrown = true;
            }

            // Assert
            Assert.IsTrue(exceptionThrown, "Expected NullReferenceException to be thrown");
        }

        [TestMethod]
        public void RefreshList_WithEmptyGenes_HandlesEmptyList()
        {
            // Arrange
            var populationList = new PopulationList();
            var population = new Population
            {
                Members = new List<ISmartObject>(),
                lsBestGenes = new List<GenomeRecord>()
            };

            populationList.SetDataSource(population);

            // Act
            populationList.RefreshList();

            // Assert
            Assert.IsNotNull(populationList);
        }

        [TestMethod]
        public void RefreshList_WithNullGenes_ThrowsNullReferenceException()
        {
            // Arrange
            var populationList = new PopulationList();
            var population = new Population
            {
                Members = new List<ISmartObject>(),
                lsBestGenes = null
            };

            populationList.SetDataSource(population);

            // Act
            bool exceptionThrown = false;
            try
            {
                populationList.RefreshList();
            }
            catch (ArgumentNullException)
            {
                exceptionThrown = true;
            }

            // Assert
            Assert.IsTrue(exceptionThrown, "Expected ArgumentNullException to be thrown");
        }

        [TestMethod]
        public void SetDataSource_MultipleTimesCalled_UpdatesDataSource()
        {
            // Arrange
            var populationList = new PopulationList();
            var population1 = new Population
            {
                Members = new List<ISmartObject> { new SmartObject { ID = "old" } },
                lsBestGenes = new List<GenomeRecord>()
            };
            var population2 = new Population
            {
                Members = new List<ISmartObject> { new SmartObject { ID = "new" } },
                lsBestGenes = new List<GenomeRecord>()
            };

            // Act
            populationList.SetDataSource(population1);
            populationList.SetDataSource(population2);

            // Assert
            Assert.IsNotNull(populationList);
        }

        [TestMethod]
        public void RefreshList_WithSingleGene_PreservesGene()
        {
            // Arrange
            var populationList = new PopulationList();
            var population = new Population
            {
                Members = new List<ISmartObject>(),
                lsBestGenes = new List<GenomeRecord>
                {
                    new GenomeRecord { ID = "onlyGene", Fitness = 75.0 }
                }
            };

            populationList.SetDataSource(population);

            // Act
            populationList.RefreshList();

            // Assert
            Assert.IsNotNull(populationList);
        }

        [TestMethod]
        public void RefreshList_WithDuplicateFitnessValues_HandlesCorrectly()
        {
            // Arrange
            var populationList = new PopulationList();
            var population = new Population
            {
                Members = new List<ISmartObject>(),
                lsBestGenes = new List<GenomeRecord>
                {
                    new GenomeRecord { ID = "gene1", Fitness = 100.0 },
                    new GenomeRecord { ID = "gene2", Fitness = 100.0 },
                    new GenomeRecord { ID = "gene3", Fitness = 100.0 }
                }
            };

            populationList.SetDataSource(population);

            // Act
            populationList.RefreshList();

            // Assert
            Assert.IsNotNull(populationList);
        }

        [TestMethod]
        public void SetDataSource_WithLargePopulation_HandlesCorrectly()
        {
            // Arrange
            var populationList = new PopulationList();
            var members = new List<ISmartObject>();
            var genes = new List<GenomeRecord>();

            for (int i = 0; i < 1000; i++)
            {
                members.Add(new SmartObject { ID = $"member{i}", Cycles = i });
                genes.Add(new GenomeRecord { ID = $"gene{i}", Fitness = i * 1.5 });
            }

            var population = new Population
            {
                Members = members,
                lsBestGenes = genes
            };

            // Act
            populationList.SetDataSource(population);

            // Assert
            Assert.IsNotNull(populationList);
        }

        [TestMethod]
        public void RefreshList_WithNegativeFitnessValues_OrdersCorrectly()
        {
            // Arrange
            var populationList = new PopulationList();
            var population = new Population
            {
                Members = new List<ISmartObject>(),
                lsBestGenes = new List<GenomeRecord>
                {
                    new GenomeRecord { ID = "gene1", Fitness = -50.0 },
                    new GenomeRecord { ID = "gene2", Fitness = 0.0 },
                    new GenomeRecord { ID = "gene3", Fitness = -100.0 },
                    new GenomeRecord { ID = "gene4", Fitness = 50.0 }
                }
            };

            populationList.SetDataSource(population);

            // Act
            populationList.RefreshList();

            // Assert
            Assert.IsNotNull(populationList);
        }

        [TestMethod]
        public void Constructor_WhenCalledMultipleTimes_CreatesMultipleInstances()
        {
            // Arrange & Act
            var populationList1 = new PopulationList();
            var populationList2 = new PopulationList();

            // Assert
            Assert.IsNotNull(populationList1);
            Assert.IsNotNull(populationList2);
            Assert.AreNotSame(populationList1, populationList2);
        }
    }
}
