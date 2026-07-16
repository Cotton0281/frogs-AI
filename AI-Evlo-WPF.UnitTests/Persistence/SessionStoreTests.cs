using AI_Evlo_Test;
using AI_Evlo_Test.ConfigLib;
using AI_Evlo_Test.Objects;
using AI_Evlo_Test.Persistence;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AI_Evlo_WPF.UnitTests.Persistence
{
    [TestClass]
    public class SessionStoreTests
    {
        [TestMethod]
        public void SaveTwice_CreatesBackupAndLoadsLatestSession()
        {
            string directory = CreateDirectory();
            try
            {
                var store = new SessionStore(directory);
                store.Save(new[] { new Population { Name = "first", SizeLimit = 2 } });
                store.Save(new[] { new Population { Name = "second", SizeLimit = 3 } });

                List<Population> loaded = store.Load();

                Assert.AreEqual("second", loaded.Single().Name);
                Assert.IsTrue(File.Exists(store.BackupPath));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [TestMethod]
        public void Load_WhenPrimaryIsCorrupt_UsesBackup()
        {
            string directory = CreateDirectory();
            try
            {
                var store = new SessionStore(directory);
                store.Save(new[] { new Population { Name = "recoverable", SizeLimit = 2 } });
                store.Save(new[] { new Population { Name = "latest", SizeLimit = 3 } });
                File.WriteAllText(store.SessionPath, "not json");

                List<Population> loaded = store.Load();

                Assert.AreEqual("recoverable", loaded.Single().Name);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [TestMethod]
        public void LoadLegacyFiles_OnlyReadsGuidNamedJsonFiles()
        {
            string directory = CreateDirectory();
            try
            {
                var population = new Population { Name = "legacy", SizeLimit = 4 };
                MainWindow.WriteToJsonFile(Path.Combine(directory, Guid.NewGuid() + ".json"), population);
                File.WriteAllText(Path.Combine(directory, "movement-settings.json"), "{}");

                List<Population> loaded = new SessionStore(directory).LoadLegacyFiles();

                Assert.AreEqual("legacy", loaded.Single().Name);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [TestMethod]
        public void SaveAndLoad_PreservesCustomResidualTopologyLocksAndAutoGrowState()
        {
            string directory = CreateDirectory();
            try
            {
                var factory = NeuralNetworkFactory.GetInstance();
                NeuralNetworkGene gene = PopulationNeuralNetworkEvolution.AddResidualLayer(
                    factory.Create(2, 1, 1, 2).GetGenes());
                var population = new Population
                {
                    Name = "grown",
                    SizeLimit = 2,
                    NeuroNetTemplate = NeuroNetStructure.FromGene(gene),
                    LayerLocks = new List<bool> { true, false, true },
                    AutoGrowNeuralNetwork = true,
                    SurvivalRecordCycles = 250,
                    NextAutoGrowSurvivalCycles = 400,
                    lsBestGenes = new List<GenomeRecord>
                    {
                        new GenomeRecord { ID = "best", Gene = gene, Fitness = 12 }
                    }
                };
                var store = new SessionStore(directory);

                store.Save(new[] { population });
                Population loaded = store.Load().Single();

                Assert.IsTrue(loaded.AutoGrowNeuralNetwork);
                Assert.AreEqual(250, loaded.SurvivalRecordCycles);
                Assert.AreEqual(400, loaded.NextAutoGrowSurvivalCycles);
                CollectionAssert.AreEqual(new[] { true, false, true }, loaded.LayerLocks);
                Assert.AreEqual(NeuralLayerKind.Residual, loaded.NeuroNetTemplate.LayerDefinitions[1].Kind);
                Assert.AreEqual(NeuralLayerKind.Residual, loaded.lsBestGenes[0].Gene.HiddenGenes[1].Kind);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static string CreateDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
