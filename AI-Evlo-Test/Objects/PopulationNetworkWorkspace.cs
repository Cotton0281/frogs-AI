using ArtificialNeuralNetwork;
using System.Collections.Generic;

namespace AI_Evlo_Test.Objects
{
    public enum SpecialAgentRole
    {
        Golden,
        LongestLivedAlive,
        HighestFitnessAlive,
        BestArchived
    }

    public sealed class PopulationOption
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name ?? "Population";
    }

    public sealed class SpecialAgentOption
    {
        public SpecialAgentRole Role { get; set; }
        public string DisplayName { get; set; }
        public string Detail { get; set; }
        public override string ToString() => DisplayName ?? Role.ToString();
    }

    public sealed class PopulationNetworkSnapshot
    {
        public string PopulationId { get; set; }
        public string PopulationName { get; set; }
        public SpecialAgentRole SelectedRole { get; set; }
        public string AgentDetail { get; set; }
        public INeuralNetwork Network { get; set; }
        public IReadOnlyList<bool> LayerLocks { get; set; }
        public IReadOnlyList<SpecialAgentOption> AvailableAgents { get; set; }
        public bool AutoGrowEnabled { get; set; }
        public int NextAutoGrowSurvivalCycles { get; set; }
        public int MutationRate { get; set; }
    }

    public interface IPopulationNetworkWorkspace
    {
        IReadOnlyList<PopulationOption> GetPopulations();
        PopulationNetworkSnapshot Capture(string populationId, SpecialAgentRole preferredRole);
        PopulationNetworkChangeResult SetLayerLock(string populationId, int destinationLayerIndex, bool locked);
        PopulationNetworkChangeResult AddLayer(string populationId);
        PopulationNetworkChangeResult BlankUnlockedLayers(string populationId);
        PopulationNetworkChangeResult RandomizeUnlockedLayers(string populationId);
        PopulationNetworkChangeResult SetMutationRate(string populationId, int mutationRate);
        PopulationNetworkChangeResult MutateAgent(string populationId, SpecialAgentRole role);
    }
}
