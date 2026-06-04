using AI_Evlo_Test.ConfigLib;
using AI_Evlo_Test.Enumerators;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Genes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AI_Evlo_Test.Objects
{
    public interface IPopulation: IabstrPopulation<ISmartObject>
    {
        new List<ISmartObject> Members { get; set; }
        Type ObjectType { get; }

        new void Add(ISmartObject Member);
        string ToJson();
        new string ToString();
       // new NeuroNetStructure NeuroNetTemplate { get; set; }

    }

    public class Population : abstrPopulation<ISmartObject>, IabstrPopulation<ISmartObject>
    {
        private PopulationBeing being = PopulationBeing.Frog;
        private double goldenInitialThreshold;
        private double goldenThreshold;

        [Newtonsoft.Json.JsonIgnore]
        [System.Runtime.Serialization.IgnoreDataMember]
        public override List<ISmartObject> Members { get; set; } = new List<ISmartObject>();
        public PopulationBeing Being
        {
            get => being;
            set
            {
                if (being == value)
                    return;

                being = value;
                goldenInitialThreshold = 0;
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        [System.Runtime.Serialization.IgnoreDataMember]
        public int NextRegrowCycle { get; set; } = -1;

        [Newtonsoft.Json.JsonIgnore]
        [System.Runtime.Serialization.IgnoreDataMember]
        public int RegrowModeIndex { get; set; } = 0;

        public bool GoldenAgentEnabled { get; set; } = true;
        public NeuralNetworkGene GoldenAgentGene { get; set; }
        public int GoldenAveragedNetworkCount { get; set; }
        public int GoldenRecordSurvivorCycles { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        [System.Runtime.Serialization.IgnoreDataMember]
        public double GoldenInitialThreshold
        {
            get
            {
                if (goldenInitialThreshold <= 0)
                    goldenInitialThreshold = InitialGoldenThresholdFor(Being);

                return goldenInitialThreshold;
            }
        }

        public double GoldenThreshold
        {
            get
            {
                double initial = GoldenInitialThreshold;
                return goldenThreshold <= 0 ? initial : Math.Max(initial, goldenThreshold);
            }
            set => goldenThreshold = value;
        }

        [Newtonsoft.Json.JsonIgnore]
        [System.Runtime.Serialization.IgnoreDataMember]
        public ISmartObject GoldenAgent { get; set; }

        // Runtime-only: a System.Type does not round-trip through JSON cleanly.
        // It is rebuilt from Being after load.
        [Newtonsoft.Json.JsonIgnore]
        [System.Runtime.Serialization.IgnoreDataMember]
        public Type ObjectType { get; internal set; }

        /// <summary>
        /// Add member to population
        /// </summary>
        /// <param name="Member"></param>
        public override void Add(ISmartObject Member)
        {
            Members.Add(Member);
            TotalMembersCount++;
            if (Member.VisibleShape != null && (Member.VisibleShape is Shape))
                (Member.VisibleShape as Shape).Fill = PopulationColorBrush;
        }

        /// <summary>
        /// The name of the population
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        public string ToJson()
        {
            string jsonObj = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            return jsonObj;
        }

        public bool TryAverageGoldenBrain(ISmartObject survivor)
        {
            if (!ShouldCheckGoldenAverage(survivor))
                return false;

            SmartObject smart = (SmartObject)survivor;
            AdvanceGoldenAverageMilestone(smart);

            NeuralNetworkGene survivorGene = survivor.NNetwork.GetGenes();
            if (GoldenAgentGene == null || GoldenAveragedNetworkCount <= 0)
            {
                GoldenAgentGene = Utils.CloneGene(survivorGene);
                GoldenAveragedNetworkCount = 1;
                return true;
            }

            NeuralNetworkGene averaged = Utils.IncrementalAverageGene(
                GoldenAgentGene,
                survivorGene,
                GoldenAveragedNetworkCount);

            if (averaged == null)
                return false;

            GoldenAgentGene = averaged;
            GoldenAveragedNetworkCount++;
            return true;
        }

        public bool ShouldCheckGoldenAverage(ISmartObject survivor)
        {
            if (!GoldenAgentEnabled
                || survivor == null
                || ReferenceEquals(survivor, GoldenAgent)
                || survivor.NNetwork == null
                || !(survivor is SmartObject smart)
                || smart.IsGoldenAgent)
            {
                return false;
            }

            UpdateGoldenThresholdFromSurvivor(smart.Cycles);

            if (smart.NextGoldenAverageCycle > 0)
                return smart.Cycles >= smart.NextGoldenAverageCycle;

            return smart.Cycles >= GoldenThreshold;
        }

        public void ResetGoldenBrain()
        {
            GoldenAgentGene = null;
            GoldenAveragedNetworkCount = 0;
            GoldenRecordSurvivorCycles = 0;
            goldenInitialThreshold = 0;
            GoldenThreshold = 0;
        }

        public static double InitialGoldenThresholdFor(PopulationBeing being)
        {
            switch (being)
            {
                case PopulationBeing.Bird:
                    return Bird.BirdMaxHp / Bird.FlightHpDrain;
                case PopulationBeing.Shark:
                    return Shark.SharkMaxHp / Shark.SwimHpDrain;
                default:
                    return SmartObject.MaxHp / SmartObject.BaseHpDrain;
            }
        }

        private void UpdateGoldenThresholdFromSurvivor(int survivorCycles)
        {
            if (survivorCycles <= GoldenRecordSurvivorCycles)
                return;

            GoldenRecordSurvivorCycles = survivorCycles;
            double recordThreshold = survivorCycles / 2.0;
            if (recordThreshold > GoldenThreshold)
                GoldenThreshold = recordThreshold;
        }

        private void AdvanceGoldenAverageMilestone(SmartObject survivor)
        {
            if (survivor.NextGoldenAverageCycle <= 0)
            {
                survivor.GoldenAverageIntervalTicks = Math.Max(1, (int)Math.Ceiling(GoldenThreshold * 0.1));
                survivor.NextGoldenAverageCycle = survivor.Cycles + survivor.GoldenAverageIntervalTicks;
                return;
            }

            survivor.NextGoldenAverageCycle += Math.Max(1, survivor.GoldenAverageIntervalTicks);
        }

    }

    public interface IabstrPopulation<T>
    {
        int Count { get; }
        DateTime CreatedOn { get; }
        string ID { get; }
        int LifeCycles { get; set; }
        List<GenomeRecord> lsBestGenes { get; set; }
        NeuroNetStructure NeuroNetTemplate { get; set; }
        List<T> Members { get; set; }
        string Name { get; set; }
        Color PopulationColor { get; set; }
        int SizeLimit { get; set; }
        int StartingCycle { get; set; }
        double TopFitness { get; }
        int TotalMembersCount { get; set; }

        void Add(T Member);
        string GenerateMemberId(ISmartObject parent = null);
        string GenerateMemberId(GenomeRecord parent);
        string ToString();
    }

    /// <summary>
    /// Population is a grop of object that evolve in similar manner. Abstract class. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class abstrPopulation<T> : IabstrPopulation<T>
    {
        // [JsonProperty] makes Newtonsoft populate the private setter so the ID survives a
        // save→load round-trip; otherwise each load minted a new GUID, orphaning the saved file.
        [Newtonsoft.Json.JsonProperty]
        public string ID { get; private set; } = Guid.NewGuid().ToString();
        virtual public List<T> Members { get; set; }
        public List<GenomeRecord> lsBestGenes { get; set; } = new List<GenomeRecord>();
        public NeuroNetStructure NeuroNetTemplate { get; set; }
        public virtual void Add(T Member) { }

        /// <summary>
        /// Count of all members that were ever members of this population
        /// </summary>
        public int TotalMembersCount { get; set; }

        // Runtime-only WPF brush; only the Color value (below) is persisted.
        [Newtonsoft.Json.JsonIgnore]
        [System.Runtime.Serialization.IgnoreDataMember]
        public SolidColorBrush PopulationColorBrush = new SolidColorBrush() { Color = Colors.Yellow };
        public Color PopulationColor { get { return PopulationColorBrush.Color; } set { PopulationColorBrush.Color = value; } }
        /// <summary>
        /// Population size. Members count.
        /// </summary>
        public int Count { get => Members.Count; }
        public string Name { get; set; } = "PopulationX";
        /// <summary>
        /// Minimum size of population. It is not the actual members count. 
        /// If below this number population will start adding members. If above no members will be added
        /// </summary>
        public int SizeLimit { get; set; }
        public DateTime CreatedOn { get; } = DateTime.Now;
        public int LifeCycles { get; set; } = 0;
        public int StartingCycle { get; set; } = 0;
        public double TopFitness
        {
            get
            {
                if (Members.Count > 0)
                    return this.Members.Max(m => (m as ISmartObject).Fitness);
                else
                    return 0;
            }
        }

        public override string ToString()
        {
            return Name;
        }
        /// <summary>
        /// Generate ID for a Smart Opbect in this population
        /// </summary>
        /// <typeparam name="T"> this population</typeparam>
        /// <param name="population">this is "this" object</param>
        /// <param name="parent">Use parent Id as base if there is a parent</param>
        /// <returns></returns>
        public string GenerateMemberId(ISmartObject parent = null)
        {
            if (parent == null)
                return this.Name + "::" + this.TotalMembersCount;
            else
                return parent.ID + ":" + this.TotalMembersCount.ToString();
        }

        public string GenerateMemberId(GenomeRecord parent)
        {
            if (parent == null)
                return this.Name + "::" + this.TotalMembersCount;
            else
                return parent.ID + ":" + this.TotalMembersCount.ToString();
        }
    }

    public class GenomeRecord
    {
        private double fitness;

        /// <summary>
        /// Id of the original agent
        /// </summary>
        public string ID { get; set; }
        public NeuralNetworkGene Gene { get; set; }
        public double Fitness { 
            get => fitness - Ofsprings; 
            set => fitness = value; }
        /// <summary>
        /// counts generation of modifications
        /// </summary>
        public int Generation { get; set; }
        public int Ofsprings { get; set; }
    }

    public enum RegrowthBrainSourceKind
    {
        ArchivedBestExact,
        ArchivedBestMutated,
        AliveBestExact,
        AliveBestMutated,
        Random
    }

    public sealed class RegrowthBrainSource
    {
        public RegrowthBrainSourceKind Kind { get; }
        public ISmartObject AliveParent { get; }
        public GenomeRecord ArchivedParent { get; }

        private RegrowthBrainSource(RegrowthBrainSourceKind kind, ISmartObject aliveParent, GenomeRecord archivedParent)
        {
            Kind = kind;
            AliveParent = aliveParent;
            ArchivedParent = archivedParent;
        }

        public static RegrowthBrainSource Random()
        {
            return new RegrowthBrainSource(RegrowthBrainSourceKind.Random, null, null);
        }

        public static RegrowthBrainSource Alive(RegrowthBrainSourceKind kind, ISmartObject parent)
        {
            return parent == null
                ? Random()
                : new RegrowthBrainSource(kind, parent, null);
        }

        public static RegrowthBrainSource Archived(RegrowthBrainSourceKind kind, GenomeRecord parent)
        {
            return parent == null || parent.Gene == null
                ? Random()
                : new RegrowthBrainSource(kind, null, parent);
        }
    }

    public static class PopulationRegrowthPolicy
    {
        private const int ModeCount = 5;

        public static bool ShouldSpawn(Population population, int currentCycle)
        {
            return NeedsRegrowth(population)
                && population.NextRegrowCycle >= 0
                && currentCycle >= population.NextRegrowCycle;
        }

        public static bool NeedsRegrowth(Population population)
        {
            return population != null
                && population.Members != null
                && population.SizeLimit > population.Members.Count;
        }

        public static void ScheduleNextSpawn(Population population, int currentCycle)
        {
            population.NextRegrowCycle = currentCycle + NaturalSurvivalTicksFor(population?.Being ?? PopulationBeing.Frog);
        }

        public static void MarkSpawned(Population population, int currentCycle)
        {
            ScheduleNextSpawn(population, currentCycle);
            population.RegrowModeIndex = (population.RegrowModeIndex + 1) % ModeCount;
        }

        public static void ClearSchedule(Population population)
        {
            population.NextRegrowCycle = -1;
        }

        public static int NaturalSurvivalTicksFor(PopulationBeing being)
        {
            switch (being)
            {
                case PopulationBeing.Bird:
                    return (int)Math.Ceiling(Bird.BirdMaxHp / Bird.FlightHpDrain);
                case PopulationBeing.Shark:
                    return (int)Math.Ceiling(Shark.SharkMaxHp / Shark.SwimHpDrain);
                default:
                    return (int)Math.Ceiling(SmartObject.MaxHp / SmartObject.BaseHpDrain);
            }
        }

        public static RegrowthBrainSource SelectSource(Population population)
        {
            if (population == null)
                return RegrowthBrainSource.Random();

            int mode = population.RegrowModeIndex % ModeCount;
            if (mode < 0)
                mode += ModeCount;

            switch (mode)
            {
                case 0:
                    return BestOverall(population, mutate: false);
                case 1:
                    return BestOverall(population, mutate: true);
                case 2:
                    return RegrowthBrainSource.Alive(RegrowthBrainSourceKind.AliveBestExact, BestAlive(population));
                case 3:
                    return RegrowthBrainSource.Alive(RegrowthBrainSourceKind.AliveBestMutated, BestAlive(population));
                default:
                    return RegrowthBrainSource.Random();
            }
        }

        private static RegrowthBrainSource BestOverall(Population population, bool mutate)
        {
            ISmartObject alive = BestAlive(population);
            GenomeRecord archived = BestArchived(population);

            bool useAlive = alive != null && (archived == null || alive.Fitness >= archived.Fitness);
            if (useAlive)
                return RegrowthBrainSource.Alive(
                    mutate ? RegrowthBrainSourceKind.AliveBestMutated : RegrowthBrainSourceKind.AliveBestExact,
                    alive);

            return RegrowthBrainSource.Archived(
                mutate ? RegrowthBrainSourceKind.ArchivedBestMutated : RegrowthBrainSourceKind.ArchivedBestExact,
                archived);
        }

        private static ISmartObject BestAlive(Population population)
        {
            return population.Members?
                .Where(member => member != null && member.NNetwork != null)
                .OrderByDescending(member => member.Fitness)
                .FirstOrDefault();
        }

        private static GenomeRecord BestArchived(Population population)
        {
            return population.lsBestGenes?
                .Where(record => record != null && record.Gene != null)
                .OrderByDescending(record => record.Fitness)
                .FirstOrDefault();
        }
    }

    //public class ViewModelPopulation : INotifyPropertyChanged
    //{
    //    private readonly CollectionView _populations;
    //    private Population _selectedPopulation;

    //    public ViewModelPopulation(IList<Population> listPoplations)
    //    {
    //        listPoplations.Add(new Population() { Name= "<New Population>"});
    //        _populations = new CollectionView(listPoplations);
    //    }

    //    public CollectionView PhonebookEntries
    //    {
    //        get { return _populations; }
    //    }

    //    public Population SelectedPopulation
    //    {
    //        get { return _selectedPopulation; }
    //        set
    //        {
    //            if (_selectedPopulation == value) return;
    //            _selectedPopulation = value;
    //            OnPropertyChanged("PopulationEntry");
    //        }
    //    }

    //    private void OnPropertyChanged(string propertyName)
    //    {
    //        if (PropertyChanged != null)
    //            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    //    }
    //    public event PropertyChangedEventHandler PropertyChanged;
    //}
}
