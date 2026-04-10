using AI_Evlo_Test.ConfigLib;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.ActivationFunctions;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.WeightInitializer;
using System;
using System.Text;
using System.Windows;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Object with Neural Network
    /// </summary>
    //public class SmartObject : BasicObject, ISmartObject
    //{
    //    public INeuralNetwork NNetwork { get; set; }
    //    public static  int MaxHp { get; set; } = 300;
    //    public int Cycles { get; set; } = 0;
    //    public int Generation { get; set; } = 0;
    //    public double Fitness { get { return Cycles; } }
    //    public double HP
    //    {
    //        get => _hP; set
    //        {
    //            if (value > MaxHp)
    //                _hP = MaxHp;
    //            else if (value < 0)
    //                _hP = 0;
    //            else
    //                _hP = value;
    //        }
    //    }
    //    public string ParentId { get; set; } = "0";
    //    public bool IsOnTarget { get; set; } = false;

    //    private double _hP;

    //    /// <summary>
    //    /// Give neuroNet new imputs, calculate outputs, trigger actions with the outputs.
    //    /// </summary>
    //    /// <param name="arrayInputs"></param>
    //    public double[] Act(double[] arrayInputs)
    //    {
    //        Cycles++;
    //        double[] dblOutputs ;
    //        if (NNetwork != null)
    //        {
    //            NNetwork.SetInputs(arrayInputs);
    //            NNetwork.Process();
    //            dblOutputs = NNetwork.GetOutputs();
    //            SetLocation(base.Location.X + dblOutputs[0], base.Location.Y + dblOutputs[1]);
    //            return  dblOutputs;
    //        }
    //        return new double[0];
    //    }

    //    public SmartObject()
    //    {
    //        HP = MaxHp;
    //    }
    //     public SmartObject(NeuroNetStructure nnStructure, ref RandomWeightInitializer randomInit)
    //   {
    //        //https://github.com/jobeland/NeuralNetwork


    //        var somaFactory = SomaFactory.GetInstance(new SimpleSummation());
    //        var axonFactory = AxonFactory.GetInstance(new ArtificialNeuralNetwork.ActivationFunctions.TanhActivationFunction());

    //        var hiddenSynapseFactory = SynapseFactory.GetInstance(randomInit, axonFactory);
    //        var ioSynapseFactory = SynapseFactory.GetInstance(new ConstantWeightInitializer(1.0), axonFactory);

    //        var neuronFactory = NeuronFactory.GetInstance();
    //        INeuralNetworkFactory nnFactory = NeuralNetworkFactory
    //            .GetInstance(somaFactory, axonFactory, hiddenSynapseFactory,
    //            ioSynapseFactory, randomInit, neuronFactory);

    //        NNetwork = nnFactory.Create(nnStructure.Inputs, nnStructure.Outputs,
    //            nnStructure.HiddenLayers, nnStructure.NeuronsInHiddenLayer);
    //        HP =MaxHp;
    //    }

    //    public SmartObject(INeuralNetwork NeuralNetwork)
    //    {
    //        HP = MaxHp;
    //        NNetwork = NeuralNetwork;
    //    }

    //    internal new void Dispose()
    //    {
    //        this.NNetwork = null;
    //        this.VisibleShape = null;
    //        base.Dispose();
    //    }

    //    void ISmartObject.Dispose()
    //    {
    //        Dispose();
    //    }
    //}


    /// <summary>
    /// Object with Neural Network and raycasting perception.
    /// </summary>
    public class SmartObject : BasicObject, ISmartObject, ISensable
    {
        public INeuralNetwork NNetwork { get; set; }
        public RayPerception Perception { get; set; }

        /// <summary>
        /// Set by the environment each tick based on population membership.
        /// </summary>
        public ObjectCategory Category { get; set; } = ObjectCategory.Frog;

        static public int MaxHp { get; set; } = 300;
        public static double MaxStamina { get; set; } = 200;
        public static double MaxSpeed { get; set; } = 1.5;

        /// <summary>Actual movement magnitude from the last tick.</summary>
        public double LastSpeed { get; set; } = 0;

        /// <summary>Stamina regenerated per tick.</summary>
        private const double StaminaRegenRate = 0.3;

        /// <summary>Stamina cost per unit of combined output magnitude.</summary>
        private const double StaminaCostPerUnit = 0.15;

        public int Cycles { get; set; } = 0;
        public int Generation { get; set; } = 0;
        public int Ofsprings { get; set; } = 0;
        public double Fitness { get { return Cycles - Ofsprings; } }
        public double Stamina { get; set; } = 200;

        /// <summary>Per-instance HP ceiling. Override in subclasses to raise the cap.</summary>
        protected virtual int EffectiveMaxHp => MaxHp;

        public double HP
        {
            get => _hP; set
            {
                int cap = EffectiveMaxHp;
                if (value > cap)
                    _hP = cap;
                else if (value < 0)
                    _hP = 0;
                else
                    _hP = value;
            }
        }
        public string ParentId { get; set; } = "0";

        public bool IsGettingHP { get; set; } = false;

        private double[] _cachedInputs;
        public double[] CachedInputs => _cachedInputs ?? (_cachedInputs = new double[Perception.Signals.Length + 2]);

        private double _hP;

        /// <summary>
        /// Give neuroNet new inputs, calculate outputs, trigger actions with the outputs.
        /// Movement is scaled by current stamina fraction so exhausted agents slow down.
        /// </summary>
        /// <param name="arrayInputs"></param>
        public double[] Act(double[] arrayInputs)
        {
            Cycles++;
            double[] dblOutputs;
            if (NNetwork == null)
                return new double[0];

            NNetwork.SetInputs(arrayInputs);
            NNetwork.Process();
            dblOutputs = NNetwork.GetOutputs();

            double rotationRequest = dblOutputs[0] * 3;
            double thrustRequest = dblOutputs[1] + 0.5;

            // Drain stamina proportional to requested effort
            double requestedCost = StaminaCostPerUnit * (Math.Abs(rotationRequest) + Math.Abs(thrustRequest));
            Stamina -= requestedCost;
            if (Stamina < 0) Stamina = 0;

            // Scale movement by remaining stamina — exhausted agents move less
            double staminaFraction = MaxStamina > 0 ? Stamina / MaxStamina : 0;
            double actualThrust = thrustRequest * staminaFraction;
            this.Rotate(rotationRequest * staminaFraction);
            this.PushForward(actualThrust);
            LastSpeed = Math.Abs(actualThrust);

            // Regenerate stamina slowly each tick
            Stamina = Math.Min(MaxStamina, Stamina + StaminaRegenRate);

            return dblOutputs;
        }

        public SmartObject()
        {
            HP = MaxHp;
            Perception = new RayPerception(centerRayMultiplier: 3.0);
        }
        public SmartObject(NeuroNetStructure nnStructure, ref RandomWeightInitializer randomInit)
        {
            //https://github.com/jobeland/NeuralNetwork


            var somaFactory = SomaFactory.GetInstance(new SimpleSummation());
            var axonFactory = AxonFactory.GetInstance(new ArtificialNeuralNetwork.ActivationFunctions.TanhActivationFunction());

            var hiddenSynapseFactory = SynapseFactory.GetInstance(randomInit, axonFactory);
            var ioSynapseFactory = SynapseFactory.GetInstance(new ConstantWeightInitializer(1.0), axonFactory);

            var neuronFactory = NeuronFactory.GetInstance();
            INeuralNetworkFactory nnFactory = NeuralNetworkFactory
                .GetInstance(somaFactory, axonFactory, hiddenSynapseFactory,
                ioSynapseFactory, randomInit, neuronFactory);

            NNetwork = nnFactory.Create(nnStructure.Inputs, nnStructure.Outputs,
                nnStructure.HiddenLayers, nnStructure.NeuronsInHiddenLayer);
            HP = MaxHp;
            Perception = new RayPerception(centerRayMultiplier: 3.0);
        }

        public SmartObject(INeuralNetwork NeuralNetwork)
        {
            HP = MaxHp;
            NNetwork = NeuralNetwork;
            Perception = new RayPerception(centerRayMultiplier: 3.0);
        }

        void ISmartObject.Dispose()
        {
            this.NNetwork = null;
            this.VisibleShape = null;
            this.Perception = null;
            base.Dispose();
        }
    }


    public interface ISmartObject :IBasicObject
    {
        string ID { get; set; }
        string ParentId { get; set; }
        double Fitness { get; }
        double HP { get; set; }
        //int MaxHp { get; set; }
        int Generation { get; set; }
        /// <summary>
        /// Count of offsprings based on this object
        /// </summary>
        int Ofsprings { get; set; }
        INeuralNetwork NNetwork { get; set; }
        /// <summary>
        /// Number of eviroment cycles have this object existed for
        /// </summary>
        int Cycles { get; set; }
        /// <summary>
        /// True if received HP from a target in this cycle
        /// </summary>
        bool IsGettingHP { get; set; }
        /// <summary>
        /// Current stamina level. Drains with movement, regenerates slowly.
        /// </summary>
        double Stamina { get; set; }
        double[] Act(double[] arrayInputs);
        void Dispose();
    }
}
