using AI_Evlo_Test.ConfigLib;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.ActivationFunctions;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.WeightInitializer;
using System;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        public static double MaxSpeed { get; set; } = 1.5;
        public static MovementSettings MovementSettings { get; set; } = new MovementSettings();
        public const double BaseHpDrain = 0.35;
        public const int MemorySize = 2;
        public const int MovementOutputCount = 2;
        public const int OutputCount = MovementOutputCount + MemorySize;
        public const int ExtraInputCount = 1 + MemorySize;
        public const int InputCount = ExtraInputCount + RayPerception.DefaultInputCount;

        /// <summary>Actual movement magnitude from the last tick.</summary>
        public double LastSpeed { get; set; } = 0;

        /// <summary>Actual rotation (degrees) applied last tick. Negative = left, positive = right.</summary>
        public double LastRotation { get; protected set; } = 0;

        public int Cycles { get; set; } = 0;
        public int Generation { get; set; } = 0;
        public int Ofsprings { get; set; } = 0;
        public bool IsGoldenAgent { get; set; } = false;
        public int NextGoldenAverageCycle { get; set; } = 0;
        public int GoldenAverageIntervalTicks { get; set; } = 0;
        public DateTime GoldenMergeFlashUntilUtc { get; private set; } = DateTime.MinValue;
        public double Fitness { get { return Cycles - Ofsprings; } }

        /// <summary>Per-instance HP ceiling. Override in subclasses to raise the cap.</summary>
        public virtual int EffectiveMaxHp => MaxHp;

        /// <summary>Returns true when this agent is hungry enough to bite.</summary>
        public virtual bool IsHungry =>
            HP < EffectiveMaxHp * (MovementSettings ?? new MovementSettings()).PredatorBiteHpThreshold;

        public int BiteCooldownTicksRemaining { get; set; }

        public bool IsGoldenMergeFlashActive => DateTime.UtcNow < GoldenMergeFlashUntilUtc;

        public void TriggerGoldenMergeFlash()
        {
            GoldenMergeFlashUntilUtc = DateTime.UtcNow.AddMilliseconds(200);
        }

        public void TickBiteCooldown()
        {
            if (BiteCooldownTicksRemaining > 0)
                BiteCooldownTicksRemaining--;
        }

        /// <summary>
        /// The category this agent broadcasts to other agents' perception rays.
        /// Overridden per species (and per state, e.g. landed birds).
        /// </summary>
        public virtual ObjectCategory SenseCategory => ObjectCategory.Frog;

        /// <summary>
        /// Categories this agent's own rays cannot perceive. Empty means "sees everything".
        /// </summary>
        public virtual ObjectCategory[] IgnoredCategories => NoIgnoredCategories;

        protected static readonly ObjectCategory[] NoIgnoredCategories = new ObjectCategory[0];

        /// <summary>
        /// Returns the sprite frame to display this tick, or null for shape-based agents.
        /// Overridden by species that animate (Frog, Bird, Shark).
        /// </summary>
        public virtual ImageSource GetSpriteFrame() => null;

        protected virtual double MovementSpeedMultiplier => 1.0;

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
        public double[] CachedInputs => _cachedInputs ?? (_cachedInputs = new double[Perception.Signals.Length + ExtraInputCount]);

        /// <summary>
        /// Last tick's recurrent scratchpad values, fed back as neural-network inputs.
        /// </summary>
        public double[] Memory { get; private set; } = new double[MemorySize];

        private double _hP;

        /// <summary>
        /// Give neuroNet new inputs, calculate outputs, trigger actions with the outputs.
        /// </summary>
        /// <param name="arrayInputs"></param>
        public virtual double[] Act(double[] arrayInputs)
        {
            Cycles++;
            double[] dblOutputs;
            if (NNetwork == null)
                return new double[0];

            NNetwork.SetInputs(arrayInputs);
            NNetwork.Process();
            dblOutputs = NNetwork.GetOutputs();

            double rotationRequest = dblOutputs.Length > 0 ? dblOutputs[0] * 3 : 0;
            double thrustRequest = dblOutputs.Length > 1 ? dblOutputs[1] + 0.5 : 0;
            double thrustApplied = ClampThrust(thrustRequest);

            this.Rotate(rotationRequest);
            this.PushForward(thrustApplied);
            LastSpeed = Math.Abs(thrustApplied);
            LastRotation = rotationRequest;
            ApplyMovementHpCost(rotationRequest, thrustApplied);
            WriteMemoryOutputs(dblOutputs);

            return dblOutputs;
        }

        public void ResetMemory()
        {
            Array.Clear(Memory, 0, Memory.Length);
        }

        private void WriteMemoryOutputs(double[] outputs)
        {
            for (int i = 0; i < MemorySize; i++)
            {
                int outputIndex = MovementOutputCount + i;
                Memory[i] = outputs != null && outputIndex < outputs.Length ? outputs[outputIndex] : 0;
            }
        }

        private double ClampThrust(double thrustRequest)
        {
            double maxSpeed = Math.Max(0, MaxSpeed * MovementSpeedMultiplier);
            if (maxSpeed <= 0)
                return 0;

            if (thrustRequest > maxSpeed)
                return maxSpeed;
            if (thrustRequest < -maxSpeed)
                return -maxSpeed;

            return thrustRequest;
        }

        private void ApplyMovementHpCost(double rotationApplied, double thrustApplied)
        {
            MovementSettings settings = MovementSettings ?? new MovementSettings();
            settings.Normalize();
            HP -= Math.Abs(rotationApplied) * settings.RotationHpCost;
            HP -= Math.Abs(thrustApplied) * settings.ThrustHpCost;
        }

        /// <summary>
        /// Per-tick interaction with rafts and open water. Base behavior is the swimmer
        /// (frog-like): rest and gain HP on a charged raft, otherwise lose HP in open water.
        /// Birds and sharks override this with predator behavior.
        /// </summary>
        public virtual void InteractWithRafts(RaftTickContext ctx)
        {
            TickBiteCooldown();
            IsGettingHP = false;
            bool onAnyRaft = false;

            foreach (TargetObj raft in ctx.Rafts)
            {
                double raftRadius = raft.Size / 2D;
                Vector toRaft = Point.Subtract(raft.Location, Location);
                if (toRaft.LengthSquared <= raftRadius * raftRadius)
                {
                    raft.ObjectsOnTop++;
                    if (this is Frog)
                    {
                        raft.FrogsOnTop++;
                        Frog raftFrog = (Frog)this;
                        if (!ctx.FrogsOnRafts.Contains(raftFrog))
                            ctx.FrogsOnRafts.Add(raftFrog);
                    }
                    onAnyRaft = true;
                    HP += raft.HpCharge;
                    if (raft.HpCharge > 0)
                    {
                        IsGettingHP = true;
                    }
                }
            }

            // The environment takes HP from all swimmers each tick
            HP -= BaseHpDrain;

            if (!onAnyRaft && this is Frog waterFrog)
            {
                ctx.FrogsInWater.Add(waterFrog);
                if (IsHungry)
                    ctx.HungryFrogsInWater.Add(waterFrog);
            }
            else if (onAnyRaft && this is Frog raftFrog && IsHungry)
            {
                ctx.HungryFrogsOnRafts.Add(raftFrog);
            }
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
        double[] Act(double[] arrayInputs);
        void Dispose();
    }
}
