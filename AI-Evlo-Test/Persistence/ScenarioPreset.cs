using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AI_Evlo_Test.ConfigLib;
using AI_Evlo_Test.Enumerators;
using AI_Evlo_Test.Objects;
using Newtonsoft.Json;

namespace AI_Evlo_Test.Persistence
{
    internal sealed class ScenarioPreset
    {
        public int Version { get; set; } = 1;
        public string Name { get; set; } = "Scenario";
        public EEnvironmentType Environment { get; set; } = EEnvironmentType.TwoTargets;
        public MovementSettings Movement { get; set; } = new();
        public List<Population> Populations { get; set; } = new();

        internal static ScenarioPreset Capture(string name, EEnvironmentType environment,
            MovementSettings movement, IEnumerable<Population> populations)
        {
            var preset = new ScenarioPreset { Name = name, Environment = environment, Movement = movement.Clone() };
            foreach (var p in populations)
                preset.Populations.Add(new Population
                {
                    Name = p.Name, SizeLimit = p.SizeLimit, Being = p.Being,
                    PopulationColor = p.PopulationColor, NeuroNetTemplate = p.NeuroNetTemplate,
                    LayerLocks = new List<bool>(p.LayerLocks ?? new List<bool>()), MutationRate = p.MutationRate,
                    SpawnDelay = p.SpawnDelay, PauseMutation = p.PauseMutation,
                    GoldenAgentEnabled = p.GoldenAgentEnabled, AutoGrowNeuralNetwork = p.AutoGrowNeuralNetwork
                });
            // Deep copy settings so saving never serializes a live topology.
            return JsonConvert.DeserializeObject<ScenarioPreset>(JsonConvert.SerializeObject(preset));
        }

        internal void Validate()
        {
            if (Version != 1 || !Enum.IsDefined(Environment) || Movement == null || Populations == null || Populations.Count == 0)
                throw new InvalidDataException("This is not a supported starting scenario.");
            Movement.Normalize();
            if (Populations.Any(p => p == null || p.SizeLimit < 1 || p.SizeLimit > 10000
                || !Enum.IsDefined(p.Being) || p.NeuroNetTemplate == null))
                throw new InvalidDataException("Each population needs a valid species, brain and size between 1 and 10,000.");
            if (Populations.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Populations.Count
                || Populations.Any(p => string.IsNullOrWhiteSpace(p.Name)))
                throw new InvalidDataException("Population names must be nonempty and unique.");
            foreach (var p in Populations)
            {
                var brain = p.NeuroNetTemplate;
                brain.EnsureLayerDefinitions();
                if (brain.Inputs != SmartObject.InputCount || brain.Outputs != SmartObject.OutputCount
                    || brain.LayerDefinitions.Count == 0 || brain.LayerDefinitions.Any(l => l == null || l.NeuronCount < 1 || !Enum.IsDefined(l.Kind)))
                    throw new InvalidDataException("A scenario contains an incompatible neural network.");
            }
        }

        internal static ScenarioPreset Load(string path)
        {
            var preset = JsonConvert.DeserializeObject<ScenarioPreset>(File.ReadAllText(path))
                ?? throw new InvalidDataException("The scenario is empty.");
            preset.Validate();
            return Capture(preset.Name, preset.Environment, preset.Movement, preset.Populations);
        }

        internal void Save(string path)
        {
            Validate();
            SessionStore.AtomicWrite(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
