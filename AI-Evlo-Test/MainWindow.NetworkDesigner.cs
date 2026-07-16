using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace AI_Evlo_Test
{
    public partial class MainWindow : IPopulationNetworkWorkspace
    {
        private VisualizeNetwork _networkDesigner;

        private void ShowPopulationNetworkDesigner(Population population)
        {
            if (population == null)
                return;

            if (_networkDesigner == null)
            {
                _networkDesigner = new VisualizeNetwork(this) { Owner = this };
                _networkDesigner.Closed += (sender, args) => _networkDesigner = null;
            }

            if (!_networkDesigner.IsVisible)
                _networkDesigner.Show();
            _networkDesigner.SelectPopulation(population.ID);
            _networkDesigner.Activate();
        }

        IReadOnlyList<PopulationOption> IPopulationNetworkWorkspace.GetPopulations()
        {
            lock (simLock)
            {
                return lsPopulations
                    .Select(population => new PopulationOption { Id = population.ID, Name = population.Name })
                    .ToList();
            }
        }

        PopulationNetworkSnapshot IPopulationNetworkWorkspace.Capture(
            string populationId,
            SpecialAgentRole preferredRole)
        {
            lock (simLock)
            {
                Population population = lsPopulations.FirstOrDefault(candidate => candidate.ID == populationId);
                if (population == null)
                    return null;

                List<SpecialAgentOption> options = BuildSpecialAgentOptions(population);
                if (options.Count == 0)
                {
                    return new PopulationNetworkSnapshot
                    {
                        PopulationId = population.ID,
                        PopulationName = population.Name,
                        LayerLocks = PopulationNeuralNetworkEvolution.NormalizeLocks(
                            population.LayerLocks,
                            (population.NeuroNetTemplate?.HiddenLayers ?? 0) + 1),
                        AvailableAgents = options,
                        AutoGrowEnabled = population.AutoGrowNeuralNetwork,
                        NextAutoGrowSurvivalCycles = population.NextAutoGrowSurvivalCycles,
                        MutationRate = population.MutationRate
                    };
                }

                SpecialAgentOption selected = options.FirstOrDefault(option => option.Role == preferredRole)
                    ?? options[0];
                INeuralNetwork network = ResolveSpecialAgentNetwork(population, selected.Role);
                return new PopulationNetworkSnapshot
                {
                    PopulationId = population.ID,
                    PopulationName = population.Name,
                    SelectedRole = selected.Role,
                    AgentDetail = selected.Detail,
                    Network = network,
                    LayerLocks = PopulationNeuralNetworkEvolution.NormalizeLocks(
                        population.LayerLocks,
                        (network?.HiddenLayers.Count ?? population.NeuroNetTemplate?.HiddenLayers ?? 0) + 1),
                    AvailableAgents = options,
                    AutoGrowEnabled = population.AutoGrowNeuralNetwork,
                    NextAutoGrowSurvivalCycles = population.NextAutoGrowSurvivalCycles,
                    MutationRate = population.MutationRate
                };
            }
        }

        PopulationNetworkChangeResult IPopulationNetworkWorkspace.SetLayerLock(
            string populationId,
            int destinationLayerIndex,
            bool locked)
        {
            lock (simLock)
            {
                Population population = lsPopulations.FirstOrDefault(candidate => candidate.ID == populationId);
                if (population == null)
                    return PopulationNetworkChangeResult.Failure("Population is no longer available.");

                int layerCount = (population.NeuroNetTemplate?.HiddenLayers ?? 0) + 1;
                population.LayerLocks = PopulationNeuralNetworkEvolution.NormalizeLocks(
                    population.LayerLocks,
                    layerCount);
                if (destinationLayerIndex < 0 || destinationLayerIndex >= layerCount)
                    return PopulationNetworkChangeResult.Failure("Layer is no longer available.");

                population.LayerLocks[destinationLayerIndex] = locked;
                SaveSession();
                string layerName = destinationLayerIndex == layerCount - 1
                    ? "Outputs"
                    : $"H{destinationLayerIndex + 1}";
                string message = $"{layerName} is now {(locked ? "locked" : "unlocked")} for '{population.Name}'.";
                Log(message);
                return PopulationNetworkChangeResult.Success(message);
            }
        }

        PopulationNetworkChangeResult IPopulationNetworkWorkspace.AddLayer(string populationId)
        {
            lock (simLock)
            {
                Population population = lsPopulations.FirstOrDefault(candidate => candidate.ID == populationId);
                if (population == null)
                    return PopulationNetworkChangeResult.Failure("Population is no longer available.");

                PopulationNetworkChangeResult result = PopulationNeuralNetworkEvolution.AddResidualLayer(population);
                if (result.Succeeded)
                {
                    SaveSession();
                    Log(result.Message);
                    SelectedPopulation = population;
                }
                return result;
            }
        }

        PopulationNetworkChangeResult IPopulationNetworkWorkspace.BlankUnlockedLayers(string populationId)
        {
            return UpdatePopulationNetworkParameters(
                populationId,
                PopulationNeuralNetworkEvolution.BlankUnlockedLayers);
        }

        PopulationNetworkChangeResult IPopulationNetworkWorkspace.RandomizeUnlockedLayers(string populationId)
        {
            return UpdatePopulationNetworkParameters(
                populationId,
                population => PopulationNeuralNetworkEvolution.RandomizeUnlockedLayers(population));
        }

        PopulationNetworkChangeResult IPopulationNetworkWorkspace.SetMutationRate(
            string populationId,
            int mutationRate)
        {
            lock (simLock)
            {
                Population population = lsPopulations.FirstOrDefault(candidate => candidate.ID == populationId);
                if (population == null)
                    return PopulationNetworkChangeResult.Failure("Population is no longer available.");

                population.MutationRate = mutationRate;
                SaveSession();
                string message = $"Mutation rate for '{population.Name}' is now {population.MutationRate} parameters per event.";
                Log(message);
                return PopulationNetworkChangeResult.Success(message);
            }
        }

        PopulationNetworkChangeResult IPopulationNetworkWorkspace.MutateAgent(
            string populationId,
            SpecialAgentRole role)
        {
            lock (simLock)
            {
                Population population = lsPopulations.FirstOrDefault(candidate => candidate.ID == populationId);
                if (population == null)
                    return PopulationNetworkChangeResult.Failure("Population is no longer available.");

                PopulationNetworkChangeResult result = PopulationNeuralNetworkEvolution.MutateSpecialAgent(
                    population,
                    role,
                    evoChember);
                if (result.Succeeded)
                {
                    SaveSession();
                    Log(result.Message);
                }
                return result;
            }
        }

        private PopulationNetworkChangeResult UpdatePopulationNetworkParameters(
            string populationId,
            Func<Population, PopulationNetworkChangeResult> update)
        {
            lock (simLock)
            {
                Population population = lsPopulations.FirstOrDefault(candidate => candidate.ID == populationId);
                if (population == null)
                    return PopulationNetworkChangeResult.Failure("Population is no longer available.");

                PopulationNetworkChangeResult result = update(population);
                if (result.Succeeded)
                {
                    SaveSession();
                    Log(result.Message);
                }
                return result;
            }
        }

        private static List<SpecialAgentOption> BuildSpecialAgentOptions(Population population)
        {
            var options = new List<SpecialAgentOption>();
            if (population.GoldenAgent?.NNetwork != null || population.GoldenAgentGene != null)
            {
                options.Add(new SpecialAgentOption
                {
                    Role = SpecialAgentRole.Golden,
                    DisplayName = "Golden Agent",
                    Detail = $"Averaged brain · {population.GoldenAveragedNetworkCount:N0} contributions"
                });
            }

            ISmartObject longest = population.Members?
                .Where(member => member?.NNetwork != null)
                .OrderByDescending(member => member.Cycles)
                .ThenBy(member => member.ID, StringComparer.Ordinal)
                .FirstOrDefault();
            if (longest != null)
            {
                options.Add(new SpecialAgentOption
                {
                    Role = SpecialAgentRole.LongestLivedAlive,
                    DisplayName = "Longest-Lived Alive",
                    Detail = $"{longest.ID} · {longest.Cycles:N0} cycles"
                });
            }

            ISmartObject fittest = population.Members?
                .Where(member => member?.NNetwork != null)
                .OrderByDescending(member => member.Fitness)
                .ThenBy(member => member.ID, StringComparer.Ordinal)
                .FirstOrDefault();
            if (fittest != null)
            {
                options.Add(new SpecialAgentOption
                {
                    Role = SpecialAgentRole.HighestFitnessAlive,
                    DisplayName = "Highest-Fitness Alive",
                    Detail = $"{fittest.ID} · fitness {fittest.Fitness:0.##}"
                });
            }

            GenomeRecord archived = population.lsBestGenes?
                .Where(record => record?.Gene != null)
                .OrderByDescending(record => record.Fitness)
                .ThenBy(record => record.ID, StringComparer.Ordinal)
                .FirstOrDefault();
            if (archived != null)
            {
                options.Add(new SpecialAgentOption
                {
                    Role = SpecialAgentRole.BestArchived,
                    DisplayName = "Best Archived Genome",
                    Detail = $"{archived.ID} · fitness {archived.Fitness:0.##}"
                });
            }
            return options;
        }

        private static INeuralNetwork ResolveSpecialAgentNetwork(
            Population population,
            SpecialAgentRole role)
        {
            switch (role)
            {
                case SpecialAgentRole.Golden:
                    if (population.GoldenAgent?.NNetwork != null)
                        return Utils.CloneNeuroNet(population.GoldenAgent.NNetwork);
                    return population.GoldenAgentGene == null
                        ? null
                        : NeuralNetworkFactory.GetInstance().Create(Utils.CloneGene(population.GoldenAgentGene));

                case SpecialAgentRole.LongestLivedAlive:
                    return Utils.CloneNeuroNet((population.Members ?? new List<ISmartObject>())
                        .Where(member => member?.NNetwork != null)
                        .OrderByDescending(member => member.Cycles)
                        .ThenBy(member => member.ID, StringComparer.Ordinal)
                        .FirstOrDefault()?.NNetwork);

                case SpecialAgentRole.HighestFitnessAlive:
                    return Utils.CloneNeuroNet((population.Members ?? new List<ISmartObject>())
                        .Where(member => member?.NNetwork != null)
                        .OrderByDescending(member => member.Fitness)
                        .ThenBy(member => member.ID, StringComparer.Ordinal)
                        .FirstOrDefault()?.NNetwork);

                default:
                    GenomeRecord archived = (population.lsBestGenes ?? new List<GenomeRecord>())
                        .Where(record => record?.Gene != null)
                        .OrderByDescending(record => record.Fitness)
                        .ThenBy(record => record.ID, StringComparer.Ordinal)
                        .FirstOrDefault();
                    return archived?.Gene == null
                        ? null
                        : NeuralNetworkFactory.GetInstance().Create(Utils.CloneGene(archived.Gene));
            }
        }
    }
}
