using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AI_Evlo_WPF.UnitTests.Objects
{
    [STATestClass]
    public class SmartObjectTests
    {
        [TestMethod]
        public void Fitness_WhenCyclesAndOfspringsSet_ReturnsDifference()
        {
            // Arrange
            var smartObject = new SmartObject
            {
                Cycles = 100,
                Ofsprings = 30
            };

            // Act
            var fitness = smartObject.Fitness;

            // Assert
            Assert.AreEqual(70.0, fitness);
        }

        [TestMethod]
        public void Fitness_WhenOfspringsGreaterThanCycles_ReturnsNegativeValue()
        {
            // Arrange
            var smartObject = new SmartObject
            {
                Cycles = 50,
                Ofsprings = 80
            };

            // Act
            var fitness = smartObject.Fitness;

            // Assert
            Assert.AreEqual(-30.0, fitness);
        }

        [TestMethod]
        public void Fitness_WhenCyclesAndOfspringsZero_ReturnsZero()
        {
            // Arrange
            var smartObject = new SmartObject
            {
                Cycles = 0,
                Ofsprings = 0
            };

            // Act
            var fitness = smartObject.Fitness;

            // Assert
            Assert.AreEqual(0.0, fitness);
        }

        [TestMethod]
        public void HP_SetValidValue_ReturnsValue()
        {
            // Arrange
            var smartObject = new SmartObject();
            double expectedValue = 150.0;

            // Act
            smartObject.HP = expectedValue;

            // Assert
            Assert.AreEqual(expectedValue, smartObject.HP);
        }

        [TestMethod]
        public void HP_SetValueAboveMaxHp_ClampsToMaxHp()
        {
            // Arrange
            var smartObject = new SmartObject();
            int originalMaxHp = SmartObject.MaxHp;
            SmartObject.MaxHp = 300;

            // Act
            smartObject.HP = 500.0;

            // Assert
            Assert.AreEqual(300.0, smartObject.HP);

            // Cleanup
            SmartObject.MaxHp = originalMaxHp;
        }

        [TestMethod]
        public void HP_SetValueBelowZero_ClampsToZero()
        {
            // Arrange
            var smartObject = new SmartObject();

            // Act
            smartObject.HP = -50.0;

            // Assert
            Assert.AreEqual(0.0, smartObject.HP);
        }

        [TestMethod]
        public void HP_SetZero_ReturnsZero()
        {
            // Arrange
            var smartObject = new SmartObject();

            // Act
            smartObject.HP = 0.0;

            // Assert
            Assert.AreEqual(0.0, smartObject.HP);
        }

        [TestMethod]
        public void HP_SetMaxHpBoundary_ReturnsMaxHp()
        {
            // Arrange
            var smartObject = new SmartObject();
            int originalMaxHp = SmartObject.MaxHp;
            SmartObject.MaxHp = 300;

            // Act
            smartObject.HP = 300.0;

            // Assert
            Assert.AreEqual(300.0, smartObject.HP);

            // Cleanup
            SmartObject.MaxHp = originalMaxHp;
        }

        [TestMethod]
        public void Bird_InteractWithRafts_WhenLanded_DrainsOneFifthOfFlyingHp()
        {
            var raft = new TargetObj { Size = 100, HpCharge = 1 };
            raft.SetLocation(0, 0);
            var landed = new Bird { HP = 100 };
            landed.SetLocation(0, 0);
            var flying = new Bird { HP = 100 };
            flying.SetLocation(200, 0);
            var ctx = new RaftTickContext
            {
                Rafts = new System.Collections.Generic.List<TargetObj> { raft }
            };

            landed.InteractWithRafts(ctx);
            flying.InteractWithRafts(ctx);

            double landedDrain = 100 - landed.HP;
            double flyingDrain = 100 - flying.HP;
            Assert.AreEqual(flyingDrain / 5.0, landedDrain, 0.000001);
        }

        [TestMethod]
        public void Bird_InteractWithRafts_WhenFlyingAndHungry_RegistersAsHunter()
        {
            var raft = new TargetObj { Size = 100, HpCharge = 1 };
            raft.SetLocation(0, 0);
            var bird = new Bird { HP = 100 };
            bird.SetLocation(200, 0);
            var ctx = new RaftTickContext
            {
                Rafts = new System.Collections.Generic.List<TargetObj> { raft }
            };

            bird.InteractWithRafts(ctx);

            Assert.IsFalse(bird.IsLanded);
            Assert.AreSame(bird, ctx.HungryBirds[0]);
        }

        [TestMethod]
        public void ResolveBirdHuntsForTick_WhenBirdEatsShark_AddsSharkHpToBird()
        {
            var bird = new Bird { HP = 100 };
            bird.SetLocation(0, 0);
            var shark = new Shark { HP = 123 };
            shark.SetLocation(10, 0);

            var eaten = AI_Evlo_Test.MainWindow.ResolveBirdHuntsForTick(
                new System.Collections.Generic.List<Bird> { bird },
                new System.Collections.Generic.List<Shark> { shark });

            Assert.AreEqual(223, bird.HP);
            Assert.AreEqual(1, bird.SharksEaten);
            Assert.AreEqual(0, shark.HP);
            Assert.AreSame(shark, eaten[0]);
        }

        [TestMethod]
        public void Bird_IgnoredCategories_DoesNotIgnoreSharks()
        {
            var bird = new Bird();

            bool ignoresShark = System.Array.Exists(
                bird.IgnoredCategories,
                category => category == ObjectCategory.Shark);

            Assert.IsFalse(ignoresShark);
        }

        [TestMethod]
        public void Shark_IgnoredCategories_SeesBirdsAndWaterFrogsButIgnoresRaftFrogs()
        {
            var shark = new Shark();

            bool ignoresBird = System.Array.Exists(
                shark.IgnoredCategories,
                category => category == ObjectCategory.Bird || category == ObjectCategory.Bird_Landed);
            bool ignoresWaterFrog = System.Array.Exists(
                shark.IgnoredCategories,
                category => category == ObjectCategory.Frog);
            bool ignoresRaftFrog = System.Array.Exists(
                shark.IgnoredCategories,
                category => category == ObjectCategory.Frog_OnRaft);

            Assert.IsFalse(ignoresBird);
            Assert.IsFalse(ignoresWaterFrog);
            Assert.IsTrue(ignoresRaftFrog);
        }

        [TestMethod]
        public void RayPerception_WhenFrogOnRaftIgnored_DetectsWaterFrogBehindIt()
        {
            var perception = new RayPerception(1, 100, 0, 1.0);
            var raftFrog = new TargetObj { Size = 10, Category = ObjectCategory.Frog_OnRaft };
            raftFrog.SetLocation(20, 0);
            var waterFrog = new TargetObj { Size = 10, Category = ObjectCategory.Frog };
            waterFrog.SetLocation(40, 0);
            var objects = new System.Collections.Generic.List<ISensable> { raftFrog, waterFrog };

            perception.Update(
                new Point(0, 0),
                new System.Windows.Vector(1, 0),
                objects,
                ignoredCategories: new[] { ObjectCategory.Frog_OnRaft });

            Assert.AreEqual(ObjectCategory.Frog, perception.Hits[0].Category);
        }

        [TestMethod]
        public void CachedInputs_WhenNull_CreatesNewArrayWithCorrectLength()
        {
            // Arrange
            var smartObject = new SmartObject();
            int expectedLength = smartObject.Perception.Signals.Length + 1;

            // Act
            var cachedInputs = smartObject.CachedInputs;

            // Assert
            Assert.IsNotNull(cachedInputs);
            Assert.HasCount(expectedLength, cachedInputs);
        }

        [TestMethod]
        public void CachedInputs_WhenAccessedMultipleTimes_ReturnsSameInstance()
        {
            // Arrange
            var smartObject = new SmartObject();

            // Act
            var firstAccess = smartObject.CachedInputs;
            var secondAccess = smartObject.CachedInputs;

            // Assert
            Assert.AreSame(firstAccess, secondAccess);
        }

        [TestMethod]
        public void Act_WhenNNetworkIsNull_ReturnsEmptyArray()
        {
            // Arrange
            var smartObject = new SmartObject();
            smartObject.NNetwork = null;
            double[] inputs = new double[5];

            // Act
            var result = smartObject.Act(inputs);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [TestMethod]
        public void Act_WhenNNetworkIsNull_IncrementsOnlyCycles()
        {
            // Arrange
            var smartObject = new SmartObject();
            smartObject.NNetwork = null;
            smartObject.Cycles = 0;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.AreEqual(1, smartObject.Cycles);
        }

        [TestMethod]
        public void Act_WithValidNeuralNetwork_IncrementsCycles()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            smartObject.Cycles = 10;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.AreEqual(11, smartObject.Cycles);
        }

        [TestMethod]
        public void Act_WithValidNeuralNetwork_CallsSetInputs()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            mockNeuralNetwork.Verify(nn => nn.SetInputs(inputs), Times.Once);
        }

        [TestMethod]
        public void Act_WithValidNeuralNetwork_CallsProcess()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            mockNeuralNetwork.Verify(nn => nn.Process(), Times.Once);
        }

        [TestMethod]
        public void Act_WithValidNeuralNetwork_CallsGetOutputs()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            mockNeuralNetwork.Verify(nn => nn.GetOutputs(), Times.Once);
        }

        [TestMethod]
        public void Act_WithValidNeuralNetwork_ReturnsOutputs()
        {
            // Arrange
            var expectedOutputs = new double[] { 0.5, 0.5 };
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(expectedOutputs);

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double[] inputs = new double[5];

            // Act
            var result = smartObject.Act(inputs);

            // Assert
            Assert.AreSame(expectedOutputs, result);
        }

        [TestMethod]
        public void Act_WithOutputs_StoresRequestedMovement()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.5, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.AreEqual(1.0, smartObject.LastSpeed);
            Assert.AreEqual(1.5, smartObject.LastRotation);
        }

        [TestMethod]
        public void Act_WithOutputs_MovesObject()
        {
            // Arrange
            var mockNeuralNetwork = new Mock<INeuralNetwork>();
            mockNeuralNetwork.Setup(nn => nn.GetOutputs()).Returns(new double[] { 0.0, 0.5 });

            var smartObject = new SmartObject();
            smartObject.NNetwork = mockNeuralNetwork.Object;
            double originalY = smartObject.Location.Y;
            double[] inputs = new double[5];

            // Act
            smartObject.Act(inputs);

            // Assert
            Assert.AreNotEqual(originalY, smartObject.Location.Y);
            Assert.AreEqual(1.0, smartObject.LastSpeed);
        }

        [TestMethod]
        public void Constructor_InitializesHPToMaxHp()
        {
            // Arrange
            int originalMaxHp = SmartObject.MaxHp;
            SmartObject.MaxHp = 300;

            // Act
            var smartObject = new SmartObject();

            // Assert
            Assert.AreEqual(300.0, smartObject.HP);

            // Cleanup
            SmartObject.MaxHp = originalMaxHp;
        }

        [TestMethod]
        public void Constructor_InitializesPerceptionWithCenterRayMultiplier()
        {
            // Act
            var smartObject = new SmartObject();

            // Assert
            Assert.IsNotNull(smartObject.Perception);
        }

        [TestMethod]
        public void Constructor_InitializesPerception_WithSignalsArray()
        {
            // Act
            var smartObject = new SmartObject();

            // Assert
            Assert.IsNotNull(smartObject.Perception.Signals);
            Assert.IsNotEmpty(smartObject.Perception.Signals);
        }
    }
}
