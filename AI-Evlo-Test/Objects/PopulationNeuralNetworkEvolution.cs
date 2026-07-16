using AI_Evlo_Test.ConfigLib;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.ActivationFunctions;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AI_Evlo_Test.Objects
{
    public sealed class PopulationNetworkChangeResult
    {
        public bool Succeeded { get; }
        public string Message { get; }

        private PopulationNetworkChangeResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public static PopulationNetworkChangeResult Success(string message)
            => new PopulationNetworkChangeResult(true, message);

        public static PopulationNetworkChangeResult Failure(string message)
            => new PopulationNetworkChangeResult(false, message);
    }

    /// <summary>
    /// Owns population-wide topology changes. All candidate genes and runtime networks are built
    /// before any population state is replaced, so callers observe an all-or-nothing change.
    /// </summary>
    public static class PopulationNeuralNetworkEvolution
    {
        public static PopulationNetworkChangeResult AddResidualLayer(Population population)
        {
            if (population == null)
                return PopulationNetworkChangeResult.Failure("Population is not available.");

            try
            {
                NeuralNetworkGene topology = FindRepresentativeGene(population);
                if (topology == null)
                    return PopulationNetworkChangeResult.Failure("Population has no neural-network topology.");
                if (topology.HiddenGenes == null || topology.HiddenGenes.Count == 0)
                    return PopulationNetworkChangeResult.Failure("A residual layer requires at least one hidden layer.");

                List<INeuralNetwork> liveNetworks = CollectLiveNetworks(population);
                List<NeuralNetworkGene> genes = CollectStoredGenes(population);
                foreach (INeuralNetwork network in liveNetworks)
                {
                    if (network == null || !Utils.HasSameTopology(topology, network.GetGenes()))
                        return PopulationNetworkChangeResult.Failure("A live population brain has an incompatible topology.");
                }
                foreach (NeuralNetworkGene gene in genes)
                {
                    if (!Utils.HasSameTopology(topology, gene))
                        return PopulationNetworkChangeResult.Failure("A saved population brain has an incompatible topology.");
                }
                var factory = NeuralNetworkFactory.GetInstance();
                List<INeuralNetwork> grownLiveNetworks = liveNetworks
                    .Select(network => factory.Create(AddResidualLayer(Utils.CloneGene(network.GetGenes()))))
                    .Cast<INeuralNetwork>()
                    .ToList();
                List<NeuralNetworkGene> grownGenes = genes
                    .Select(gene => AddResidualLayer(Utils.CloneGene(gene)))
                    .ToList();
                NeuralNetworkGene grownTopology = grownLiveNetworks.Count > 0
                    ? grownLiveNetworks[0].GetGenes()
                    : grownGenes.FirstOrDefault() ?? AddResidualLayer(Utils.CloneGene(topology));

                CommitPopulationBrains(
                    population,
                    grownLiveNetworks,
                    grownGenes,
                    includeGoldenInitial: true);
                int oldHiddenCount = topology.HiddenGenes.Count;
                List<bool> oldLocks = NormalizeLocks(population.LayerLocks, oldHiddenCount + 1);
                oldLocks.Insert(oldHiddenCount, false);
                population.LayerLocks = oldLocks;
                population.NeuroNetTemplate = NeuroNetStructure.FromGene(grownTopology);

                return PopulationNetworkChangeResult.Success(
                    $"Added residual layer H{oldHiddenCount + 1} to '{population.Name}'.");
            }
            catch (Exception ex)
            {
                return PopulationNetworkChangeResult.Failure("Could not add residual layer: " + ex.Message);
            }
        }

        public static PopulationNetworkChangeResult BlankUnlockedLayers(Population population)
        {
            return ApplyUnlockedParameters(
                population,
                () => 0,
                "Zeroed unlocked layers");
        }

        public static PopulationNetworkChangeResult RandomizeUnlockedLayers(
            Population population,
            Random random = null)
        {
            return ApplyCanonicalRandomization(population, random ?? Random.Shared);
        }

        public static PopulationNetworkChangeResult MutateSpecialAgent(
            Population population,
            SpecialAgentRole role,
            EvolutionChember evolutionChember = null)
        {
            if (population == null)
                return PopulationNetworkChangeResult.Failure("Population is not available.");

            try
            {
                NeuralNetworkGene topology = FindRepresentativeGene(population);
                if (topology == null)
                    return PopulationNetworkChangeResult.Failure("Population has no neural-network topology.");

                int destinationCount = (topology.HiddenGenes?.Count ?? 0) + 1;
                List<bool> locks = NormalizeLocks(population.LayerLocks, destinationCount);
                if (locks.All(locked => locked))
                    return PopulationNetworkChangeResult.Failure("Every layer is locked; no parameters were mutated.");

                EvolutionChember chamber = evolutionChember ?? new EvolutionChember();
                int mutationCount = Math.Min(
                    population.MutationRate,
                    CountUnlockedParameters(topology, locks));
                if (mutationCount <= 0)
                    return PopulationNetworkChangeResult.Failure("The unlocked layers have no mutable parameters.");
                var factory = NeuralNetworkFactory.GetInstance();

                INeuralNetwork MutateGene(NeuralNetworkGene source)
                {
                    if (source == null)
                        return null;
                    INeuralNetwork copy = factory.Create(Utils.CloneGene(source));
                    return chamber.MutateNN(copy, mutationCount, false, locks);
                }

                string roleName;
                switch (role)
                {
                    case SpecialAgentRole.Golden:
                    {
                        NeuralNetworkGene source = population.GoldenAgent?.NNetwork?.GetGenes()
                            ?? population.GoldenAgentGene;
                        INeuralNetwork mutated = MutateGene(source);
                        if (mutated == null)
                            return PopulationNetworkChangeResult.Failure("The Golden Agent brain is not available.");
                        if (population.GoldenAgent != null)
                            population.GoldenAgent.NNetwork = mutated;
                        population.GoldenAgentGene = Utils.CloneGene(mutated.GetGenes());
                        roleName = "Golden Agent";
                        break;
                    }
                    case SpecialAgentRole.LongestLivedAlive:
                    case SpecialAgentRole.HighestFitnessAlive:
                    {
                        ISmartObject member = role == SpecialAgentRole.LongestLivedAlive
                            ? (population.Members ?? new List<ISmartObject>())
                                .Where(candidate => candidate?.NNetwork != null)
                                .OrderByDescending(candidate => candidate.Cycles)
                                .ThenBy(candidate => candidate.ID, StringComparer.Ordinal)
                                .FirstOrDefault()
                            : (population.Members ?? new List<ISmartObject>())
                                .Where(candidate => candidate?.NNetwork != null)
                                .OrderByDescending(candidate => candidate.Fitness)
                                .ThenBy(candidate => candidate.ID, StringComparer.Ordinal)
                                .FirstOrDefault();
                        if (member == null)
                            return PopulationNetworkChangeResult.Failure("The displayed live agent is no longer available.");
                        member.NNetwork = MutateGene(member.NNetwork.GetGenes());
                        roleName = role == SpecialAgentRole.LongestLivedAlive
                            ? "Longest-Lived Alive"
                            : "Highest-Fitness Alive";
                        break;
                    }
                    default:
                    {
                        GenomeRecord archive = (population.lsBestGenes ?? new List<GenomeRecord>())
                            .Where(record => record?.Gene != null)
                            .OrderByDescending(record => record.Fitness)
                            .ThenBy(record => record.ID, StringComparer.Ordinal)
                            .FirstOrDefault();
                        if (archive == null)
                            return PopulationNetworkChangeResult.Failure("The displayed archived genome is no longer available.");
                        INeuralNetwork mutated = MutateGene(archive.Gene);
                        archive.Gene = Utils.CloneGene(mutated.GetGenes());
                        roleName = "Best Archived Genome";
                        break;
                    }
                }

                population.LayerLocks = locks;
                return PopulationNetworkChangeResult.Success(
                    $"Mutated {mutationCount} unlocked weights or biases in {roleName} for '{population.Name}'.");
            }
            catch (Exception ex)
            {
                return PopulationNetworkChangeResult.Failure("Could not mutate the displayed brain: " + ex.Message);
            }
        }

        public static NeuralNetworkGene AddResidualLayer(NeuralNetworkGene gene)
        {
            if (gene?.HiddenGenes == null || gene.HiddenGenes.Count == 0)
                throw new ArgumentException("A residual layer requires at least one hidden layer.", nameof(gene));

            LayerGene previous = gene.HiddenGenes[gene.HiddenGenes.Count - 1];
            if (previous?.Neurons == null || previous.Neurons.Count == 0)
                throw new ArgumentException("The preceding hidden layer is empty.", nameof(gene));

            var outgoing = previous.Neurons
                .Select(neuron => neuron.Axon.Weights.ToList())
                .ToList();
            int width = previous.Neurons.Count;
            for (int source = 0; source < width; source++)
                previous.Neurons[source].Axon.Weights = Enumerable.Repeat(0.0, width).ToList();

            var residual = new LayerGene { Kind = NeuralLayerKind.Residual };
            for (int neuronIndex = 0; neuronIndex < width; neuronIndex++)
            {
                residual.Neurons.Add(new NeuronGene
                {
                    Soma = new SomaGene
                    {
                        Bias = 0,
                        SummationFunction = typeof(SimpleSummation)
                    },
                    Axon = new AxonGene
                    {
                        ActivationFunction = typeof(TanhActivationFunction),
                        Weights = outgoing[neuronIndex]
                    }
                });
            }
            gene.HiddenGenes.Add(residual);
            return gene;
        }

        public static List<bool> NormalizeLocks(IEnumerable<bool> locks, int expectedCount)
        {
            var normalized = locks?.Take(expectedCount).ToList() ?? new List<bool>();
            while (normalized.Count < expectedCount)
                normalized.Add(false);
            return normalized;
        }

        private static PopulationNetworkChangeResult ApplyCanonicalRandomization(
            Population population,
            Random random)
        {
            if (population == null)
                return PopulationNetworkChangeResult.Failure("Population is not available.");

            try
            {
                NeuralNetworkGene topology = FindRepresentativeGene(population);
                if (topology == null)
                    return PopulationNetworkChangeResult.Failure("Population has no neural-network topology.");

                int destinationCount = (topology.HiddenGenes?.Count ?? 0) + 1;
                List<bool> locks = NormalizeLocks(population.LayerLocks, destinationCount);
                if (locks.All(locked => locked))
                    return PopulationNetworkChangeResult.Failure("Every layer is locked; no parameters were changed.");

                ValidatePopulationTopologies(
                    population,
                    topology,
                    includeGoldenInitial: false,
                    includeArchives: false);

                ISmartObject bestAlive = (population.Members ?? new List<ISmartObject>())
                    .Where(member => member?.NNetwork != null)
                    .OrderByDescending(member => member.Fitness)
                    .ThenBy(member => member.ID, StringComparer.Ordinal)
                    .FirstOrDefault();
                GenomeRecord bestArchived = PopulationRegrowthPolicy.BestArchived(population);
                NeuralNetworkGene canonical = Utils.CloneGene(
                    bestAlive?.NNetwork?.GetGenes()
                    ?? bestArchived?.Gene
                    ?? population.GoldenAgent?.NNetwork?.GetGenes()
                    ?? population.GoldenAgentGene
                    ?? topology);
                SetUnlockedParameters(canonical, locks, () => random.NextDouble() * 2 - 1);

                var factory = NeuralNetworkFactory.GetInstance();
                factory.Create(Utils.CloneGene(canonical));
                foreach (ISmartObject member in population.Members ?? new List<ISmartObject>())
                    if (member?.NNetwork != null)
                        member.NNetwork = factory.Create(Utils.CloneGene(canonical));

                if (population.GoldenAgent != null)
                    population.GoldenAgent.NNetwork = factory.Create(Utils.CloneGene(canonical));
                if (population.GoldenAgent != null || population.GoldenAgentGene != null)
                    population.GoldenAgentGene = Utils.CloneGene(canonical);

                ReplaceArchiveWithSeed(population, canonical);
                population.LayerLocks = locks;
                return PopulationNetworkChangeResult.Success(
                    $"Randomized one canonical brain and copied it to all current brains and archived spawn sources for '{population.Name}'.");
            }
            catch (Exception ex)
            {
                return PopulationNetworkChangeResult.Failure("Could not randomize network parameters: " + ex.Message);
            }
        }

        private static PopulationNetworkChangeResult ApplyUnlockedParameters(
            Population population,
            Func<double> nextValue,
            string completedAction)
        {
            if (population == null)
                return PopulationNetworkChangeResult.Failure("Population is not available.");

            try
            {
                NeuralNetworkGene topology = FindRepresentativeGene(population);
                if (topology == null)
                    return PopulationNetworkChangeResult.Failure("Population has no neural-network topology.");

                int destinationCount = (topology.HiddenGenes?.Count ?? 0) + 1;
                List<bool> locks = NormalizeLocks(population.LayerLocks, destinationCount);
                if (locks.All(locked => locked))
                    return PopulationNetworkChangeResult.Failure("Every layer is locked; no parameters were changed.");

                List<INeuralNetwork> liveNetworks = CollectLiveNetworks(population);
                List<NeuralNetworkGene> storedGenes = CollectStoredGenes(
                    population,
                    includeGoldenInitial: false,
                    includeArchives: false);
                foreach (INeuralNetwork network in liveNetworks)
                {
                    if (network == null || !Utils.HasSameTopology(topology, network.GetGenes()))
                        return PopulationNetworkChangeResult.Failure("A live population brain has an incompatible topology.");
                }
                foreach (NeuralNetworkGene gene in storedGenes)
                {
                    if (!Utils.HasSameTopology(topology, gene))
                        return PopulationNetworkChangeResult.Failure("A saved population brain has an incompatible topology.");
                }
                var factory = NeuralNetworkFactory.GetInstance();
                List<INeuralNetwork> updatedLiveNetworks = liveNetworks
                    .Select(network =>
                    {
                        NeuralNetworkGene gene = Utils.CloneGene(network.GetGenes());
                        SetUnlockedParameters(gene, locks, nextValue);
                        return factory.Create(gene);
                    })
                    .Cast<INeuralNetwork>()
                    .ToList();
                List<NeuralNetworkGene> updatedStoredGenes = storedGenes
                    .Select(gene =>
                    {
                        NeuralNetworkGene updated = Utils.CloneGene(gene);
                        SetUnlockedParameters(updated, locks, nextValue);
                        factory.Create(Utils.CloneGene(updated));
                        return updated;
                    })
                    .ToList();
                CommitPopulationBrains(
                    population,
                    updatedLiveNetworks,
                    updatedStoredGenes,
                    includeGoldenInitial: false,
                    includeArchives: false);
                if (population.GoldenAgent?.NNetwork != null && population.GoldenAgentGene != null)
                {
                    population.GoldenAgent.NNetwork = factory.Create(
                        Utils.CloneGene(population.GoldenAgentGene));
                }
                NeuralNetworkGene archivedSeed = population.Members?
                    .FirstOrDefault(member => member?.NNetwork != null)?
                    .NNetwork.GetGenes()
                    ?? population.GoldenAgentGene
                    ?? population.GoldenAgent?.NNetwork?.GetGenes();
                if (archivedSeed == null)
                {
                    archivedSeed = Utils.CloneGene(topology);
                    SetUnlockedParameters(archivedSeed, locks, nextValue);
                    factory.Create(Utils.CloneGene(archivedSeed));
                }
                ReplaceArchiveWithSeed(population, archivedSeed);
                population.LayerLocks = locks;
                return PopulationNetworkChangeResult.Success(
                    $"{completedAction} across all current brains and spawn sources for '{population.Name}'.");
            }
            catch (Exception ex)
            {
                return PopulationNetworkChangeResult.Failure("Could not update network parameters: " + ex.Message);
            }
        }

        private static void ValidatePopulationTopologies(
            Population population,
            NeuralNetworkGene topology,
            bool includeGoldenInitial,
            bool includeArchives)
        {
            foreach (INeuralNetwork network in CollectLiveNetworks(population))
            {
                if (network == null || !Utils.HasSameTopology(topology, network.GetGenes()))
                    throw new InvalidOperationException("A live population brain has an incompatible topology.");
            }

            foreach (NeuralNetworkGene gene in CollectStoredGenes(
                population,
                includeGoldenInitial,
                includeArchives))
            {
                if (!Utils.HasSameTopology(topology, gene))
                    throw new InvalidOperationException("A saved population brain has an incompatible topology.");
            }
        }

        private static void ReplaceArchiveWithSeed(
            Population population,
            NeuralNetworkGene canonical)
        {
            population.lsBestGenes ??= new List<GenomeRecord>();
            population.lsBestGenes.Clear();
            population.lsBestGenes.Add(new GenomeRecord
            {
                ID = (population.Name ?? "Population") + "::ManualSeed",
                Gene = Utils.CloneGene(canonical),
                Fitness = 0,
                Generation = 0
            });
        }

        private static void SetUnlockedParameters(
            NeuralNetworkGene gene,
            IReadOnlyList<bool> locks,
            Func<double> nextValue)
        {
            int hiddenCount = gene.HiddenGenes?.Count ?? 0;
            for (int destinationIndex = 0; destinationIndex <= hiddenCount; destinationIndex++)
            {
                if (destinationIndex < locks.Count && locks[destinationIndex])
                    continue;

                LayerGene source = destinationIndex == 0
                    ? gene.InputGene
                    : gene.HiddenGenes[destinationIndex - 1];
                LayerGene destination = destinationIndex < hiddenCount
                    ? gene.HiddenGenes[destinationIndex]
                    : gene.OutputGene;

                foreach (NeuronGene neuron in source?.Neurons ?? new List<NeuronGene>())
                {
                    if (neuron?.Axon?.Weights == null)
                        continue;
                    for (int weightIndex = 0; weightIndex < neuron.Axon.Weights.Count; weightIndex++)
                        neuron.Axon.Weights[weightIndex] = nextValue();
                }

                foreach (NeuronGene neuron in destination?.Neurons ?? new List<NeuronGene>())
                    if (neuron?.Soma != null)
                        neuron.Soma.Bias = nextValue();
            }
        }

        private static int CountUnlockedParameters(
            NeuralNetworkGene gene,
            IReadOnlyList<bool> locks)
        {
            int count = 0;
            int hiddenCount = gene.HiddenGenes?.Count ?? 0;
            for (int destinationIndex = 0; destinationIndex <= hiddenCount; destinationIndex++)
            {
                if (destinationIndex < locks.Count && locks[destinationIndex])
                    continue;

                LayerGene source = destinationIndex == 0
                    ? gene.InputGene
                    : gene.HiddenGenes[destinationIndex - 1];
                LayerGene destination = destinationIndex < hiddenCount
                    ? gene.HiddenGenes[destinationIndex]
                    : gene.OutputGene;
                count += (source?.Neurons ?? new List<NeuronGene>())
                    .Sum(neuron => neuron?.Axon?.Weights?.Count ?? 0);
                count += (destination?.Neurons ?? new List<NeuronGene>())
                    .Count(neuron => neuron?.Soma != null);
            }
            return count;
        }

        private static NeuralNetworkGene FindRepresentativeGene(Population population)
        {
            INeuralNetwork live = CollectLiveNetworks(population).FirstOrDefault();
            if (live != null)
                return live.GetGenes();
            NeuralNetworkGene stored = CollectStoredGenes(population).FirstOrDefault();
            if (stored != null)
                return stored;
            NeuroNetStructure structure = population.NeuroNetTemplate;
            if (structure == null || structure.HiddenLayers <= 0)
                return null;
            IReadOnlyList<NeuralLayerDefinition> definitions = structure.GetLayerDefinitions();
            return NeuralNetworkFactory.GetInstance()
                .Create(
                    structure.Inputs,
                    structure.Outputs,
                    definitions.Select(layer => layer.NeuronCount).ToList(),
                    definitions.Select(layer => layer.Kind).ToList())
                .GetGenes();
        }

        private static List<INeuralNetwork> CollectLiveNetworks(Population population)
        {
            var networks = (population.Members ?? new List<ISmartObject>())
                .Where(member => member?.NNetwork != null)
                .Select(member => member.NNetwork)
                .ToList();
            if (population.GoldenAgent?.NNetwork != null
                && !(population.Members?.Contains(population.GoldenAgent) ?? false))
                networks.Add(population.GoldenAgent.NNetwork);
            return networks;
        }

        private static List<NeuralNetworkGene> CollectStoredGenes(
            Population population,
            bool includeGoldenInitial = true,
            bool includeArchives = true)
        {
            var genes = new List<NeuralNetworkGene>();
            if (population.GoldenAgentGene != null)
                genes.Add(population.GoldenAgentGene);
            if (includeGoldenInitial && population.GoldenInitialGene != null)
                genes.Add(population.GoldenInitialGene);
            if (includeArchives)
            {
                genes.AddRange((population.lsBestGenes ?? new List<GenomeRecord>())
                    .Where(record => record?.Gene != null)
                    .Select(record => record.Gene));
            }
            return genes;
        }

        private static void CommitPopulationBrains(
            Population population,
            IReadOnlyList<INeuralNetwork> liveNetworks,
            IReadOnlyList<NeuralNetworkGene> storedGenes,
            bool includeGoldenInitial,
            bool includeArchives = true)
        {
            int liveIndex = 0;
            foreach (ISmartObject member in population.Members ?? new List<ISmartObject>())
                if (member?.NNetwork != null)
                    member.NNetwork = liveNetworks[liveIndex++];
            if (population.GoldenAgent?.NNetwork != null
                && !(population.Members?.Contains(population.GoldenAgent) ?? false))
                population.GoldenAgent.NNetwork = liveNetworks[liveIndex++];

            int geneIndex = 0;
            if (population.GoldenAgentGene != null)
                population.GoldenAgentGene = storedGenes[geneIndex++];
            if (includeGoldenInitial && population.GoldenInitialGene != null)
                population.GoldenInitialGene = storedGenes[geneIndex++];
            if (includeArchives)
            {
                foreach (GenomeRecord record in population.lsBestGenes ?? new List<GenomeRecord>())
                    if (record?.Gene != null)
                        record.Gene = storedGenes[geneIndex++];
            }
        }
    }

    public static class PopulationAutoGrowthPolicy
    {
        public static void SetEnabled(Population population, bool enabled)
        {
            if (population == null)
                return;

            population.AutoGrowNeuralNetwork = enabled;
            if (!enabled)
            {
                population.NextAutoGrowSurvivalCycles = 0;
                return;
            }

            int firstMilestone = Math.Max(
                1,
                PopulationRegrowthPolicy.NaturalSurvivalTicksFor(population.Being) * 2);
            population.NextAutoGrowSurvivalCycles = NextDoublingAbove(
                firstMilestone,
                population.SurvivalRecordCycles);
        }

        public static bool TryGrow(
            Population population,
            int observedSurvivalCycles,
            out PopulationNetworkChangeResult result)
        {
            result = null;
            if (population == null)
                return false;

            if (observedSurvivalCycles > population.SurvivalRecordCycles)
                population.SurvivalRecordCycles = observedSurvivalCycles;

            if (!population.AutoGrowNeuralNetwork)
                return false;

            if (population.NextAutoGrowSurvivalCycles <= 0)
                SetEnabled(population, true);

            if (population.SurvivalRecordCycles < population.NextAutoGrowSurvivalCycles)
                return false;

            int hiddenCount = population.NeuroNetTemplate?.HiddenLayers
                ?? population.Members?.FirstOrDefault(member => member?.NNetwork != null)?.NNetwork.HiddenLayers.Count
                ?? 0;
            List<bool> originalLocks = population.LayerLocks?.ToList() ?? new List<bool>();
            population.LayerLocks = Enumerable.Repeat(true, hiddenCount + 1).ToList();

            result = PopulationNeuralNetworkEvolution.AddResidualLayer(population);
            if (!result.Succeeded)
            {
                population.LayerLocks = originalLocks;
                return false;
            }

            int doubled = population.NextAutoGrowSurvivalCycles > int.MaxValue / 2
                ? int.MaxValue
                : population.NextAutoGrowSurvivalCycles * 2;
            population.NextAutoGrowSurvivalCycles = NextDoublingAbove(
                doubled,
                population.SurvivalRecordCycles);
            return true;
        }

        private static int NextDoublingAbove(int startingMilestone, int currentRecord)
        {
            int milestone = Math.Max(1, startingMilestone);
            while (milestone <= currentRecord && milestone < int.MaxValue)
            {
                if (milestone > int.MaxValue / 2)
                    return int.MaxValue;
                milestone *= 2;
            }
            return milestone;
        }
    }
}
