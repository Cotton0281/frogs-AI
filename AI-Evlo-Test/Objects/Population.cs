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
        [Newtonsoft.Json.JsonIgnore]
        [System.Runtime.Serialization.IgnoreDataMember]
        public override List<ISmartObject> Members { get; set; } = new List<ISmartObject>();
        public PopulationBeing Being { get; set; } = PopulationBeing.Frog;
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
        public string ID { get; private set; } = Guid.NewGuid().ToString();
        virtual public List<T> Members { get; set; }
        public List<GenomeRecord> lsBestGenes { get; set; } = new List<GenomeRecord>();
        public NeuroNetStructure NeuroNetTemplate { get; set; }
        public virtual void Add(T Member) { }

        /// <summary>
        /// Count of all members that were ever members of this population
        /// </summary>
        public int TotalMembersCount { get; set; }

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
